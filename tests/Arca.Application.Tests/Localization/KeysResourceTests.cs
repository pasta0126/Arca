// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.RegularExpressions;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Xunit;

namespace Arca.Application.Tests.Localization;

/// <summary>Every text of the password and key screens exists in Catalan (acces-i-xifrat, 7.5).</summary>
public sealed partial class KeysResourceTests
{
    [GeneratedRegex(@"""(Keys\.[A-Z][A-Za-z]*(?:\.[A-Z][A-Za-z]*)?)""")]
    private static partial Regex KeysLiteral();

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    [Fact]
    [Trait("spec", "acces-i-xifrat/contrasenya-del-centre: Feedback y guía (Mensajes)")]
    public void Every_keys_code_and_label_written_in_the_sources_has_a_catalan_text()
    {
        var localizer = new ResxLocalizer();
        var literals = Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .SelectMany(f => KeysLiteral().Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var missing = literals.Where(literal =>
        {
            var keys = literal.Count(c => c == '.') == 2
                ? [literal]
                : new[] { ResourceKeys.For(new Error(literal)), ResourceKeys.For(new Notice(literal)) };
            return keys.All(k => localizer.Get(k) == k || string.IsNullOrWhiteSpace(localizer.Get(k)));
        }).ToList();

        Assert.True(literals.Count > 30, "the scan found too few keys to be meaningful");
        Assert.Empty(missing);
    }

    [Fact]
    [Trait("spec", "acces-i-xifrat/contrasenya-del-centre: Feedback y guía (Mensajes)")]
    public void The_texts_never_show_a_technical_detail()
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(RepositoryRoot(), "src", "Arca.Application", "Resources", "Keys.resx"));
        var texts = doc.Descendants("data").Select(d => d.Element("value")?.Value ?? string.Empty).ToList();

        Assert.NotEmpty(texts);
        Assert.All(texts, t => Assert.DoesNotMatch("(?i)exception|stack|argon|xchacha|sqlite|nsec|libsodium", t));
    }
}
