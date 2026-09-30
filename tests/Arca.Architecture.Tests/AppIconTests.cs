// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Arca.Architecture.Tests;

/// <summary>The icon of the application (icona-d-aplicacio): one master, derivatives that correspond to it, every window carrying it and every package including it.</summary>
public sealed class AppIconTests
{
    const string Spec = "icona-d-aplicacio/icona-d-aplicacio";
    static readonly int[] _sizes = [16, 32, 48, 64, 128, 256, 512, 1024];

    static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    static string Icon(string name) => Path.Combine(Root(), "assets", "icon", name);

    [Fact]
    [Trait("spec", Spec + ": Icono sustituible (Derivados al día)")]
    public void The_derivatives_correspond_to_the_master_or_the_check_says_to_regenerate_them()
    {
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Icon("arca.svg")))).ToLowerInvariant();
        var stored = File.ReadAllText(Icon("arca.svg.sha256")).Trim();

        Assert.True(actual == stored, "assets/icon/arca.svg changed and the derivatives were not regenerated: run build/icons.sh.");
    }

    [Fact]
    [Trait("spec", Spec + ": Icono sustituible (Compilar sin herramientas de imágenes)")]
    public void Every_derivative_is_in_the_repository_so_building_needs_no_image_tool()
    {
        foreach (var size in _sizes)
        {
            var png = File.ReadAllBytes(Icon($"arca-{size}.png"));
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);
            Assert.Equal(size, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4))); // the width in the PNG header
        }

        var ico = File.ReadAllBytes(Icon("arca.ico"));
        Assert.Equal((0, 1, 6), (BinaryPrimitives.ReadUInt16LittleEndian(ico), BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2)), BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4))));
        var icns = File.ReadAllBytes(Icon("arca.icns"));
        Assert.Equal("icns", System.Text.Encoding.ASCII.GetString(icns, 0, 4));
        Assert.Equal(icns.Length, (int)BinaryPrimitives.ReadUInt32BigEndian(icns.AsSpan(4)));
    }

    [Fact]
    [Trait("spec", Spec + ": Icono provisional sencillo (Sin texto)")]
    public void The_master_is_a_plain_drawing_with_no_text_and_no_borrowed_image()
    {
        var svg = XDocument.Load(Icon("arca.svg"));
        var names = svg.Descendants().Select(e => e.Name.LocalName).ToList();

        Assert.DoesNotContain("text", names);
        Assert.DoesNotContain("image", names); // no bitmap of someone else inside
        Assert.DoesNotContain("font-face", names);
        Assert.DoesNotContain(svg.Descendants().SelectMany(e => e.Attributes()), a => a.Name.LocalName == "href");
        Assert.All(names.Where(n => n != "svg"), n => Assert.Contains(n, new[] { "rect", "circle", "path", "g", "defs" }));
    }

    [Fact]
    [Trait("spec", Spec + ": Icono visible en la aplicación (Diálogos)")]
    public void Every_window_of_the_application_gives_itself_the_icon()
    {
        var windows = Directory.EnumerateFiles(Path.Combine(Root(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"class \w+ : Window\b"))
            .ToList();

        Assert.True(windows.Count >= 6, "the windows of the application were not found");
        Assert.All(windows, f => Assert.Contains("AppIcon.ApplyTo(this)", File.ReadAllText(f), StringComparison.Ordinal));
        Assert.DoesNotContain(Directory.EnumerateFiles(Path.Combine(Root(), "src"), "*.cs", SearchOption.AllDirectories), f =>
            !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && Regex.IsMatch(File.ReadAllText(f), @"new Window\s*[({]")); // nobody opens a bare window
    }

    [Fact]
    [Trait("spec", Spec + ": Icono visible en la aplicación (Modo portable); Independiente del logotipo del centro")]
    public void The_icon_is_inside_the_assembly_and_is_never_read_from_the_database()
    {
        var project = File.ReadAllText(Path.Combine(Root(), "src", "Arca.UI", "Arca.UI.csproj"));
        Assert.Contains("<AvaloniaResource Include=\"..\\..\\assets\\icon\\arca-256.png\"", project, StringComparison.Ordinal);

        var code = File.ReadAllText(Path.Combine(Root(), "src", "Arca.UI", "Common", "AppIcon.cs"));
        Assert.Contains("avares://", code, StringComparison.Ordinal);
        Assert.DoesNotContain("CentreIdentity", code, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Icono en los paquetes (Windows, macOS, Linux)")]
    public void Each_package_carries_the_icon()
    {
        var desktop = XDocument.Load(Path.Combine(Root(), "src", "Arca.Desktop", "Arca.Desktop.csproj"));
        var applicationIcon = desktop.Descendants("ApplicationIcon").Single().Value.Replace('\\', Path.DirectorySeparatorChar);
        Assert.True(File.Exists(Path.GetFullPath(Path.Combine(Root(), "src", "Arca.Desktop", applicationIcon))), "the icon of the Windows executable does not exist");

        var script = File.ReadAllText(Path.Combine(Root(), "build", "package.sh"));
        Assert.Contains("assets/icon/arca.icns", script, StringComparison.Ordinal); // macOS
        Assert.Contains("<key>CFBundleIconFile</key><string>arca</string>", script, StringComparison.Ordinal);
        Assert.Contains("assets/icon/arca-256.png", script, StringComparison.Ordinal); // Linux and Windows image
        Assert.Contains("arca.desktop", script, StringComparison.Ordinal);
        Assert.Contains("Icon=", script, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Coste cero y licencia (Procedencia)")]
    public void The_provenance_and_the_free_tools_are_written_down_next_to_the_icon()
    {
        var readme = File.ReadAllText(Icon("README.md"));

        Assert.Contains("dibujo propio", readme, StringComparison.Ordinal);
        Assert.Contains("GPL-3.0", readme, StringComparison.Ordinal);
        Assert.Contains("build/icons.sh", readme, StringComparison.Ordinal);
        Assert.Contains("rsvg-convert", readme, StringComparison.Ordinal);
    }
}
