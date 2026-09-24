#:property PublishAot=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property AnalysisLevel=none
// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo
//
// Zero-cost control (docs/stack.md): lists the licence of every NuGet package of the solution, direct and
// transitive, and fails if any is outside the allowed list or has no reviewed exception, or if
// THIRD-PARTY-NOTICES.md is out of date.
//
//   dotnet run build/CheckLicenses.cs                   check
//   dotnet run build/CheckLicenses.cs -- --write-notices  rewrite THIRD-PARTY-NOTICES.md, then check
//
// A single file-based program so the same code runs on macOS, Linux and Windows.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

var root = FindRoot();
Environment.CurrentDirectory = root;
var writeNotices = args.Contains("--write-notices");

// Licences compatible with the GPL-3.0 that the project accepts without review (MIT, Apache 2.0, BSD, ISC).
string[] allowed = ["MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "ISC"];
var reviewed = LoadReviewed(Path.Combine(root, "build", "licenses-reviewed.json"));

var (shipped, all) = ListPackages();
var packagesFolder = PackagesFolder();

var rows = new List<Row>();
var problems = new List<string>();
foreach (var (id, version) in all.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase))
{
    var nuspec = Path.Combine(packagesFolder, id.ToLowerInvariant(), version, id.ToLowerInvariant() + ".nuspec");
    if (!File.Exists(nuspec))
    {
        problems.Add($"{id} {version}: not found in the NuGet cache (run dotnet restore first)");
        continue;
    }

    var info = Read(nuspec);
    var license = info.License;
    var status = "ok";
    if (!Accepts(license, allowed))
    {
        if (reviewed.TryGetValue(id, out var review) && Accepts(review.License, allowed.Concat(review.AlsoAccepted).ToArray()))
        {
            status = "reviewed";
            license = review.License;
        }
        else
        {
            status = "REJECTED";
            problems.Add($"{id} {version}: licence '{info.License}' is not in the allowed list and has no reviewed exception in build/licenses-reviewed.json");
        }
    }

    rows.Add(new Row(id, version, license, info.Authors, info.Copyright, info.ProjectUrl, shipped.Contains(id), status, reviewed.GetValueOrDefault(id)?.Reason));
}

var notices = BuildNotices(rows);
var noticesPath = Path.Combine(root, "THIRD-PARTY-NOTICES.md");
if (writeNotices)
{
    File.WriteAllText(noticesPath, notices, new UTF8Encoding(false));
    Console.WriteLine("Wrote THIRD-PARTY-NOTICES.md");
}
else if (!File.Exists(noticesPath) || Normalise(File.ReadAllText(noticesPath)) != Normalise(notices))
{
    problems.Add("THIRD-PARTY-NOTICES.md is missing or out of date (run: dotnet run build/CheckLicenses.cs -- --write-notices)");
}

Console.WriteLine($"{rows.Count} packages checked ({rows.Count(r => r.Shipped)} in the application, {rows.Count(r => !r.Shipped)} for development and tests)");
foreach (var group in rows.GroupBy(r => r.License).OrderBy(g => g.Key))
{
    Console.WriteLine($"  {group.Key,-40} {group.Count()}");
}

if (problems.Count > 0)
{
    Console.Error.WriteLine("\nLICENCE CHECK FAILED:");
    foreach (var problem in problems)
    {
        Console.Error.WriteLine("  - " + problem);
    }

    return 1;
}

Console.WriteLine("OK: every dependency has an accepted licence");
return 0;

// ---------------------------------------------------------------------------------------------------------------

static string Normalise(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd();

static string FindRoot()
{
    var dir = new DirectoryInfo(Environment.CurrentDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Arca.slnx")))
    {
        dir = dir.Parent;
    }

    return dir?.FullName ?? throw new InvalidOperationException("Run from inside the repository (Arca.slnx not found)");
}

static string PackagesFolder()
{
    var fromEnvironment = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
    return !string.IsNullOrWhiteSpace(fromEnvironment)
        ? fromEnvironment
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
}

static Dictionary<string, Reviewed> LoadReviewed(string path)
{
    if (!File.Exists(path))
    {
        return [];
    }

    return JsonSerializer.Deserialize<Dictionary<string, Reviewed>>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? [];
}

// Packages of the application (Arca.Desktop and what it needs) versus everything else in the solution.
static (HashSet<string> Shipped, List<(string Id, string Version)> All) ListPackages()
{
    var json = Run("dotnet", "list Arca.slnx package --include-transitive --format json");
    using var doc = JsonDocument.Parse(json);
    var all = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var shipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var project in doc.RootElement.GetProperty("projects").EnumerateArray())
    {
        var isDesktop = project.GetProperty("path").GetString()!.EndsWith("Arca.Desktop.csproj", StringComparison.Ordinal);
        if (!project.TryGetProperty("frameworks", out var frameworks))
        {
            continue;
        }

        foreach (var framework in frameworks.EnumerateArray())
        {
            foreach (var kind in new[] { "topLevelPackages", "transitivePackages" })
            {
                if (!framework.TryGetProperty(kind, out var list))
                {
                    continue;
                }

                foreach (var package in list.EnumerateArray())
                {
                    var id = package.GetProperty("id").GetString()!;
                    all[id] = package.GetProperty("resolvedVersion").GetString()!;
                    if (isDesktop)
                    {
                        shipped.Add(id);
                    }
                }
            }
        }
    }

    return (shipped, [.. all.Select(p => (p.Key, p.Value))]);
}

static string Run(string file, string arguments)
{
    var start = new ProcessStartInfo(file, arguments) { RedirectStandardOutput = true, RedirectStandardError = true };
    using var process = Process.Start(start)!;
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return process.ExitCode == 0 ? output : throw new InvalidOperationException($"{file} {arguments} failed: {process.StandardError.ReadToEnd()}");
}

static Nuspec Read(string path)
{
    var meta = XDocument.Load(path).Descendants().First(e => e.Name.LocalName == "metadata");
    string? Value(string name) => meta.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim();

    var licenseElement = meta.Elements().FirstOrDefault(e => e.Name.LocalName == "license");
    var license = licenseElement is not null && (string?)licenseElement.Attribute("type") == "expression"
        ? licenseElement.Value.Trim()
        : FromLegacyUrl(Value("licenseUrl"), licenseElement is not null);
    return new Nuspec(license, Value("authors") ?? string.Empty, Value("copyright") ?? string.Empty, Value("projectUrl") ?? string.Empty);
}

// Older packages only carry a licence URL; recognise the standard ones, everything else stays unknown and is rejected.
static string FromLegacyUrl(string? url, bool hasLicenseFile)
{
    if (hasLicenseFile)
    {
        return "(licence file, needs review)";
    }

    var u = (url ?? string.Empty).ToLowerInvariant();
    if (u.Contains("licenses.nuget.org/mit", StringComparison.Ordinal) || u.Contains("opensource.org/licenses/mit", StringComparison.Ordinal))
    {
        return "MIT";
    }

    if (u.Contains("apache.org/licenses/license-2.0", StringComparison.Ordinal) || u.Contains("licenses.nuget.org/apache-2.0", StringComparison.Ordinal))
    {
        return "Apache-2.0";
    }

    return "(unknown: " + (url ?? "no licence information") + ")";
}

// An SPDX expression such as "MIT", "MIT OR Apache-2.0" or "(MIT AND BSD-3-Clause)": accepted when at least one
// OR-alternative has every AND-part in the accepted list.
static bool Accepts(string expression, string[] accepted)
{
    var cleaned = expression.Replace("(", " ", StringComparison.Ordinal).Replace(")", " ", StringComparison.Ordinal);
    return cleaned.Split(" OR ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(alternative => alternative.Split(" AND ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(part => accepted.Contains(part, StringComparer.OrdinalIgnoreCase)));
}

static string BuildNotices(List<Row> rows)
{
    var text = new StringBuilder();
    text.AppendLine("# Avisos de software de terceros");
    text.AppendLine();
    text.AppendLine("ARCA usa los paquetes de código abierto que se listan aquí, cada uno con su licencia. Este fichero lo genera");
    text.AppendLine("`build/CheckLicenses.cs` y los scripts de verificación fallan si está desactualizado. Para regenerarlo:");
    text.AppendLine("`dotnet run build/CheckLicenses.cs -- --write-notices`.");
    text.AppendLine();
    text.AppendLine("Solo se aceptan licencias compatibles con la GPL-3.0 (MIT, Apache 2.0, BSD, ISC). Las excepciones revisadas a mano");
    text.AppendLine("están en `build/licenses-reviewed.json`, con su motivo.");

    void Section(string title, IEnumerable<Row> section)
    {
        var list = section.ToList();
        text.AppendLine();
        text.AppendLine($"## {title} ({list.Count})");
        text.AppendLine();
        text.AppendLine("| Paquete | Versión | Licencia | Titular |");
        text.AppendLine("|---------|---------|----------|---------|");
        foreach (var row in list)
        {
            var owner = string.IsNullOrWhiteSpace(row.Copyright) ? row.Authors : row.Copyright;
            text.AppendLine($"| {Cell(row.Id)} | {row.Version} | {Cell(row.License)} | {Cell(owner)} |");
        }
    }

    Section("Incluidos en la aplicación", rows.Where(r => r.Shipped));
    Section("Solo para desarrollo y pruebas (no se distribuyen)", rows.Where(r => !r.Shipped));

    var exceptions = rows.Where(r => r.Status == "reviewed").ToList();
    if (exceptions.Count > 0)
    {
        text.AppendLine();
        text.AppendLine("## Excepciones revisadas");
        text.AppendLine();
        foreach (var row in exceptions)
        {
            text.AppendLine($"- **{row.Id}** ({row.License}): {row.Reason}");
        }
    }

    return text.ToString();
}

static string Cell(string value) => value.Replace("|", "/", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();

record Nuspec(string License, string Authors, string Copyright, string ProjectUrl);

record Reviewed(string License, string Reason, string[]? AlsoAcceptedList = null)
{
    public string[] AlsoAccepted => AlsoAcceptedList ?? [];
}

record Row(string Id, string Version, string License, string Authors, string Copyright, string ProjectUrl, bool Shipped, string Status, string? Reason);
