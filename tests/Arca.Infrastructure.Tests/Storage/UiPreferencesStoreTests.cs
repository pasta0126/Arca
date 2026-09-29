// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Preferences;
using Arca.Infrastructure.Storage;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

public sealed class UiPreferencesStoreTests
{
    const string Spec = "ux-fonaments/adaptabilitat-i-disposicio: Preferencias locales de interfaz";

    [Fact]
    [Trait("spec", Spec + " (guardar y recuperar)")]
    public void Preferences_survive_closing_and_reopening_and_keep_the_database_path()
    {
        using var dir = new TempDirectory();
        var file = dir.File("settings.json");
        new LocalSettingsStore(file).Save(new LocalSettings("/dades/arca.db"));
        var prefs = new UiPreferences(
            new WindowBounds(120, 80, 1300, 800), new Dictionary<string, bool> { ["security"] = false, ["filters"] = true }, CompactLists: true);

        new LocalUiPreferencesStore(new LocalSettingsStore(file)).Save(prefs);
        var reopened = new LocalSettingsStore(file).Load();

        Assert.Equal("/dades/arca.db", reopened.DatabasePath);
        Assert.Equal(new WindowBounds(120, 80, 1300, 800), reopened.Ui!.Window);
        Assert.False(reopened.Ui.Sections!["security"]);
        Assert.True(reopened.Ui.Sections["filters"]);
        Assert.True(reopened.Ui.CompactLists);
    }

    [Fact]
    [Trait("spec", Spec + " (ajustes ilegibles)")]
    public void A_damaged_interface_section_costs_nothing_else()
    {
        using var dir = new TempDirectory();
        var file = dir.File("settings.json");
        File.WriteAllText(file, "{ \"DatabasePath\": \"/dades/arca.db\", \"Ui\": { \"Window\": \"not a window\", \"Sections\": 7 } }");

        var loaded = new LocalSettingsStore(file).Load();

        Assert.Equal("/dades/arca.db", loaded.DatabasePath);
        Assert.Null(loaded.Ui);
        Assert.Equal(new UiPreferences(), new LocalUiPreferencesStore(new LocalSettingsStore(file)).Load());
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ nope")]
    [InlineData("null")]
    [InlineData("[]")]
    [Trait("spec", Spec + " (ajustes ilegibles)")]
    public void A_missing_or_damaged_file_gives_the_default_preferences(string content)
    {
        using var dir = new TempDirectory();
        var file = dir.File("settings.json");
        File.WriteAllText(file, content);

        Assert.Equal(new UiPreferences(), new LocalUiPreferencesStore(new LocalSettingsStore(file)).Load());
        Assert.Equal(new UiPreferences(), new LocalUiPreferencesStore(new LocalSettingsStore(dir.File("absent.json"))).Load());
    }

    [Fact]
    [Trait("spec", Spec + " (sin datos personales)")]
    public void The_preferences_hold_only_sizes_positions_and_flags()
    {
        var properties = typeof(UiPreferences).GetProperties().Concat(typeof(WindowBounds).GetProperties()).Select(p => p.Name);

        Assert.Equal(
            ["CompactLists", "Height", "IsMaximized", "Sections", "SidebarCollapsed", "Width", "Window", "X", "Y"],
            properties.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    [Trait("spec", Spec + " (guardar y recuperar)")]
    public void Saving_preferences_leaves_every_other_section_exactly_as_it_was()
    {
        using var dir = new TempDirectory();
        var file = dir.File("settings.json");
        File.WriteAllText(file, "{ \"DatabasePath\": 42, \"FutureSetting\": { \"a\": [1, 2] } }"); // a value of the wrong type and one from a newer version

        new LocalUiPreferencesStore(new LocalSettingsStore(file)).Save(new UiPreferences(CompactLists: true));

        var text = File.ReadAllText(file);
        Assert.Contains("\"DatabasePath\": 42", text, StringComparison.Ordinal);
        Assert.Contains("FutureSetting", text, StringComparison.Ordinal);
        Assert.True(new LocalUiPreferencesStore(new LocalSettingsStore(file)).Load().CompactLists);
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("[1,2,3]")]
    [Trait("spec", Spec + " (ajustes ilegibles)")]
    public void A_file_that_cannot_be_read_is_never_rewritten_from_the_defaults(string content)
    {
        using var dir = new TempDirectory();
        var file = dir.File("settings.json");
        File.WriteAllText(file, content);

        new LocalUiPreferencesStore(new LocalSettingsStore(file)).Save(new UiPreferences(CompactLists: true));

        Assert.Equal(content, File.ReadAllText(file)); // the path of the database, if it was in there, is not lost
    }

    [Fact]
    [Trait("spec", Spec + " (guardar y recuperar)")]
    public void Saving_preferences_creates_the_file_when_there_is_none()
    {
        using var dir = new TempDirectory();
        var file = Path.Combine(dir.Path, "sub", "settings.json");

        new LocalUiPreferencesStore(new LocalSettingsStore(file)).Save(new UiPreferences(CompactLists: true));

        Assert.True(new LocalUiPreferencesStore(new LocalSettingsStore(file)).Load().CompactLists);
        Assert.Equal(["settings.json"], Directory.GetFiles(Path.GetDirectoryName(file)!).Select(Path.GetFileName));
    }
}
