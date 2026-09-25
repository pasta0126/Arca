// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.RegularExpressions;
using Arca.Application.Localization;
using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Application.Tests.Localization;

/// <summary>Every text of zones, lockers and their history exists in Catalan (taquilles-i-zones, 7.1).</summary>
public sealed partial class InventoryResourceTests
{
    [GeneratedRegex(@"""((?:Zones|Lockers|History)\.(?:Label|Result|Empty|Warning|Error|Kind|Locker|Unknown)[A-Za-z.]*)""")]
    private static partial Regex Literal();

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
    [Trait("spec", "taquilles-i-zones/tasks: 7.1 Todas las claves de recurso nuevas existen en catalán")]
    public void Every_zone_locker_and_history_key_written_in_the_sources_has_a_catalan_text()
    {
        var localizer = new ResxLocalizer();
        var keys = Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .SelectMany(f => Literal().Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .Where(k => !k.EndsWith('.')) // "History." is a prefix that the code completes, and is checked below
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var missing = keys.Where(k => localizer.Get(k) == k || string.IsNullOrWhiteSpace(localizer.Get(k))).ToList();

        Assert.True(keys.Count >= 25, "the scan found too few keys to be meaningful");
        Assert.Empty(missing);
    }

    [Fact]
    [Trait("spec", "taquilles-i-zones/tasks: 7.1 Todas las claves de recurso nuevas existen en catalán")]
    public void Every_history_event_type_and_its_variants_and_kinds_have_a_catalan_text()
    {
        var localizer = new ResxLocalizer();
        var keys = LockerEventTypes.All.Select(t => "History." + t)
            .Concat(["History.Locker.ReservedWithNote", "History.Locker.OutOfServiceKeeping", "History.Unknown"])
            .Concat(Enum.GetNames<OutOfServiceKind>().Select(k => "History.Kind." + k))
            .ToList();

        Assert.All(keys, key => Assert.NotEqual(key, localizer.Get(key)));
    }
}
