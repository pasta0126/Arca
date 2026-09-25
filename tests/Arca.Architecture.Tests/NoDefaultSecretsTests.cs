// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.RegularExpressions;
using Xunit;

namespace Arca.Architecture.Tests;

/// <summary>
/// acces-i-xifrat, D8: there is no key, password or secret in the production code, so any build can open a database
/// with the right password and nobody can open one without it. These checks read the sources of <c>src</c>.
/// </summary>
public sealed partial class NoDefaultSecretsTests
{
    const string Spec = "acces-i-xifrat/xifrat-de-la-base: Ningún secreto en el código";

    [GeneratedRegex("\"[0-9a-fA-F]{32,}\"")]
    private static partial Regex LongHexLiteral();

    [GeneratedRegex(@"new\s+DatabaseKey\(\s*(""|Convert\.FromHexString\(\s*""|Encoding\.\w+\.GetBytes\(\s*""|new\s+byte\[\]\s*\{|SHA\d+\.HashData)")]
    private static partial Regex KeyBuiltFromLiteral();

    [GeneratedRegex(@"GetEnvironmentVariable\(\s*""[^""]*(KEY|PASS|SECRET|PWD)[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex SecretFromEnvironment();

    [GeneratedRegex(@"ARCA_DEV_DB_KEY|ARCA_DEV_CREATE")]
    private static partial Regex RemovedDevBackdoor();

    [GeneratedRegex(@"(password|contrasenya|passphrase|secret)\w*(?<!Kdf)\s*=\s*""[^""]{4,}""", RegexOptions.IgnoreCase)]
    private static partial Regex SecretAssignedFromLiteral();

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    static IEnumerable<(string File, string Text)> Sources() => Directory
        .EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !f.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        .Select(f => (f, File.ReadAllText(f)));

    static List<string> Offenders(Regex pattern) =>
        [.. Sources().Where(s => pattern.IsMatch(s.Text)).Select(s => Path.GetRelativePath(RepositoryRoot(), s.File))];

    [Fact]
    [Trait("spec", Spec + " (Compilación propia)")]
    public void No_source_of_production_holds_a_long_hexadecimal_key()
    {
        Assert.Empty(Offenders(LongHexLiteral()));
    }

    [Fact]
    [Trait("spec", Spec + " (Compilación propia)")]
    public void No_database_key_is_built_from_a_literal()
    {
        Assert.Empty(Offenders(KeyBuiltFromLiteral()));
    }

    [Fact]
    [Trait("spec", Spec + " (Compilación propia)")]
    public void No_secret_is_read_from_the_environment_and_the_development_backdoor_is_gone()
    {
        Assert.Empty(Offenders(SecretFromEnvironment()));
        Assert.Empty(Offenders(RemovedDevBackdoor()));
    }

    [Fact]
    [Trait("spec", Spec + " (Contraseñas de prueba)")]
    public void No_password_is_assigned_from_a_literal_in_production_code()
    {
        Assert.Empty(Offenders(SecretAssignedFromLiteral()));
    }

    [Fact]
    [Trait("spec", Spec + " (Contraseñas de prueba)")]
    public void The_scanner_finds_what_it_looks_for()
    {
        Assert.Matches(LongHexLiteral(), "var k = \"" + new string('a', 64) + "\";");
        Assert.Matches(KeyBuiltFromLiteral(), "new DatabaseKey(Convert.FromHexString(\"00\"))");
        Assert.Matches(KeyBuiltFromLiteral(), "new DatabaseKey(SHA256.HashData(x))");
        Assert.Matches(SecretFromEnvironment(), "Environment.GetEnvironmentVariable(\"ARCA_DB_KEY\")");
        Assert.Matches(SecretAssignedFromLiteral(), "var password = \"letmein123\";");
        Assert.DoesNotMatch(KeyBuiltFromLiteral(), "new DatabaseKey(dek)");
    }

    [Fact]
    [Trait("spec", Spec + " (Contraseñas de prueba)")]
    public void Test_keys_live_only_in_the_test_project_and_no_production_project_references_it()
    {
        var root = RepositoryRoot();
        var production = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories);

        Assert.True(File.Exists(Path.Combine(root, "tests", "Arca.Testing", "TestKeys.cs")));
        Assert.All(production, project => Assert.DoesNotContain("Arca.Testing", File.ReadAllText(project), StringComparison.Ordinal));
    }
}
