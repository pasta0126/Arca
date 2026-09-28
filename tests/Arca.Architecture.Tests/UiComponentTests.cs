// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.RegularExpressions;
using System.Windows.Input;
using Xunit;

namespace Arca.Architecture.Tests;

/// <summary>Rules of ux-fonaments that keep the components uniform: one way to run a change, and nothing literal.</summary>
public sealed partial class UiComponentTests
{
    // --- D5: the run-once command ---

    [Fact]
    [Trait("spec", "ux-fonaments/design: D5 Comando de ejecución única")]
    public void The_only_commands_of_the_component_library_are_the_run_once_command_and_the_actions()
    {
        var commands = typeof(UI.AssemblyMarker).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ICommand).IsAssignableFrom(t))
            .Select(t => t.Name)
            .Order()
            .ToList();

        // A screen that changes data gets a RunOnceCommand, which is protected against running twice and reports its result;
        // a new kind of command has to be a conscious decision that adds itself to this list.
        Assert.Equal(["AppAction", "RunOnceCommand`1"], commands);
    }

    [Fact]
    [Trait("spec", "ux-fonaments/design: D5 Comando de ejecución única")]
    public void A_use_case_is_only_called_from_the_run_once_command_or_the_assignment_interaction()
    {
        var offending = UiSourceFiles()
            .Select(f => (Name: Path.GetFileName(f), Text: File.ReadAllText(f)))
            .Where(f => UseCaseCall().IsMatch(f.Text) && !f.Text.Contains("RunOnceCommand<", StringComparison.Ordinal))
            .Select(f => f.Name)
            .ToList();

        Assert.Empty(offending);
    }

    // --- D13: nothing literal in the components ---

    [Fact]
    [Trait("spec", "ux-fonaments/components-de-feedback: Componentes sin colores ni textos propios")]
    public void The_components_write_no_colour_no_font_and_no_text_of_their_own()
    {
        var offending = new List<string>();
        foreach (var file in UiSourceFiles().Where(f => !InTheme(f)))
        {
            foreach (var (line, number) in File.ReadLines(file).Select((l, i) => (l.Trim(), i + 1)))
            {
                if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('*') || line.StartsWith("///", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Violation(line) is { } why)
                {
                    offending.Add($"{Path.GetFileName(file)}:{number}: {why}: {line}");
                }
            }
        }

        Assert.Empty(offending);
    }

    [Theory]
    [InlineData("Foreground = Brushes.Firebrick,", "a colour")]
    [InlineData("var c = Color.Parse(\"#FF0000\");", "a colour")]
    [InlineData("var b = new SolidColorBrush(Colors.Red);", "a colour")]
    [InlineData("new TextBlock { FontSize = 20 }", "a font size")]
    [InlineData("FontFamily = new FontFamily(\"Menlo\"),", "a font family")]
    [InlineData("new TextBlock { Text = \"Cancel·la\" }", "a text")]
    [InlineData("Content = \"Desa\",", "a text")]
    public void The_scanner_finds_what_it_is_meant_to_find(string line, string why)
    {
        Assert.Equal(why, Violation(line));
    }

    [Theory]
    [InlineData("new TextBlock { Text = model.Title }")]
    [InlineData("Text = \"•\",")]
    [InlineData("Text = string.Empty,")]
    [InlineData("var text = new TextBlock().Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);")]
    public void The_scanner_lets_through_what_comes_from_the_theme_or_the_model(string line)
    {
        Assert.Null(Violation(line));
    }

    static string? Violation(string line) =>
        ColourLiteral().IsMatch(line) ? "a colour"
        : FontSizeLiteral().IsMatch(line) ? "a font size"
        : FontFamilyLiteral().IsMatch(line) ? "a font family"
        : TextLiteral().IsMatch(line) ? "a text"
        : null;

    static bool InTheme(string file) => file.Contains(Path.DirectorySeparatorChar + "Theme" + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    static IEnumerable<string> UiSourceFiles() => Directory
        .EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "Arca.UI"), "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal));

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    [GeneratedRegex(@"Color\.Parse\(|\bColors\.|\bBrushes\.|new SolidColorBrush\(|""#[0-9A-Fa-f]{6,8}""")]
    private static partial Regex ColourLiteral();

    [GeneratedRegex(@"\bFontSize\s*=\s*[0-9]")]
    private static partial Regex FontSizeLiteral();

    [GeneratedRegex(@"new FontFamily\(|\bFontFamily\s*=\s*""")]
    private static partial Regex FontFamilyLiteral();

    /// <summary>A text property set to a literal with letters in it. A lone symbol such as a bullet is a drawing, not a text.</summary>
    [GeneratedRegex(@"\b(Text|Content|Header|Title|Watermark)\s*=\s*""[^""]*\p{L}[^""]*""")]
    private static partial Regex TextLiteral();

    [GeneratedRegex(@"\.HandleAsync\(")]
    private static partial Regex UseCaseCall();
}
