// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Localization;
using Arca.Desktop.Composition;
using Arca.Testing;
using Arca.UI.Common;
using Arca.UI.Info;
using Arca.UI.Layout;
using Arca.UI.Notifications;
using Arca.UI.Shell;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class VersionVisibleTests
{
    const string Spec = "versio-visible/versio-de-l-aplicacio";

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();

    HeaderView Header(string title = "ARCA")
    {
        var state = new GlobalStateService(_ => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.GlobalState.GlobalState>.Success(new(null, 0))), new ResultNotifier(_notifications, _localizer, _log));
        return new HeaderView(state, _localizer, title);
    }

    static async Task<Window> ShownAsync(Control content, double width = WindowLimits.MinWidth)
    {
        var window = new Window { Width = width, Height = WindowLimits.MinHeight, Content = content };
        window.Show();
        await Task.Delay(30);
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // --- The header ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Versión visible en la cabecera (Siempre a la vista)")]
    public async Task The_header_shows_the_version_small_and_next_to_the_title_with_its_description()
    {
        var header = Header();
        header.ShowVersion("9.8.7-dev");
        await ShownAsync(header);

        Assert.True(header.Version.IsVisible);
        Assert.Equal("9.8.7-dev", header.Version.Text); // as it is, suffix included
        Assert.Equal("Versió de l'aplicació", ToolTip.GetTip(header.Version));
        Assert.True(header.Version.FontSize < header.Name.FontSize); // small
        var name = header.Name.TranslatePoint(new Point(0, 0), header)!.Value;
        var version = header.Version.TranslatePoint(new Point(0, 0), header)!.Value;
        Assert.True(version.X >= name.X + header.Name.Bounds.Width - 1); // after the title, not over it
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Versión visible en la cabecera (Con la identidad del centro)")]
    public async Task With_the_identity_of_the_centre_the_version_stays_beside_its_name()
    {
        var header = Header();
        header.ShowVersion("1.2.3");
        var identity = new Arca.UI.Identity.CentreIdentityModel();
        identity.Set(new Arca.Application.Identity.CentreIdentityView("Ins. Monturiol", null, null, "#A9C4D3"));
        header.ShowIdentity(identity);
        await ShownAsync(header);

        Assert.Equal("Ins. Monturiol", header.Name.Text);
        Assert.Equal("1.2.3", header.Version.Text);
        Assert.True(header.Version.IsVisible);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Versión visible en la cabecera (Ventana estrecha)")]
    public async Task A_very_long_name_is_cut_and_the_version_is_never_hidden_at_the_smallest_window()
    {
        var header = Header(new string('M', 200));
        header.ShowVersion("0.1.0-dev");
        await ShownAsync(header);

        Assert.True(header.Name.Bounds.Width <= 420 + 1);
        Assert.True(header.Version.Bounds.Width > 0);
        Assert.True(header.Version.Bounds.Right <= header.Bounds.Width); // inside the header
        Assert.Equal(Avalonia.Media.TextTrimming.CharacterEllipsis, header.Name.TextTrimming);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Versión visible en la cabecera (Siempre a la vista)")]
    public async Task The_version_follows_the_theme_and_the_year_and_needs_neither()
    {
        var header = Header();
        header.ShowVersion("1.0.0");

        await ShownAsync(header);

        Assert.True(header.Version.IsVisible); // with no active year and no identity
        Assert.DoesNotContain(header.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "?" && t.IsVisible);
    }

    // --- Settings ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Versión en Ajustes (Mismo número, Copiar)")]
    public async Task Settings_shows_the_same_version_as_a_text_that_can_be_selected_and_copied()
    {
        var info = new AppInfo("9.8.7-dev", "20260930193636_HomeCards");
        var header = Header();
        header.ShowVersion(info.ApplicationVersion);
        var view = new InfoView(new InfoViewModel(info, _localizer));
        await ShownAsync(view);

        var shown = view.GetVisualDescendants().OfType<SelectableTextBlock>().Single(t => t.Text == "9.8.7-dev");

        Assert.Equal(header.Version.Text, shown.Text); // the same value: they read the same information
        Assert.Equal("9.8.7-dev", shown.Text); // what is copied is only the number
    }

    // --- The text of a version ---

    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("0.1.0-dev", "0.1.0-dev")]
    [InlineData("0.1.0-dev+4f9c2ab", "0.1.0-dev")]
    [InlineData("  2.0.0+build.5 ", "2.0.0")]
    [InlineData("", "?")]
    [InlineData("   ", "?")]
    [InlineData("+abc", "?")]
    [InlineData(null, "?")]
    [Trait("spec", Spec + ": Formato de la versión (Compilación de desarrollo, Versión publicada)")]
    public void The_version_is_written_as_it_is_without_the_build_information(string? raw, string expected)
    {
        Assert.Equal(expected, AppVersionText.Clean(raw));
    }

    [Fact]
    [Trait("spec", Spec + ": Formato de la versión (Versión ilegible)")]
    public void A_version_that_cannot_be_read_is_unknown_and_logged_and_the_start_carries_on()
    {
        var version = AppStartup.ApplicationVersion(() => throw new InvalidOperationException("no attribute"), _log);

        Assert.Equal("?", version);
        Assert.Equal("ReadVersion", Assert.Single(_log.Entries).Context);
    }

    [Fact]
    [Trait("spec", Spec + ": Una sola fuente de la versión (Cambiar la versión)")]
    public void The_version_of_the_running_application_comes_from_the_assembly_built_with_the_one_definition()
    {
        var version = AppStartup.ApplicationVersion();

        Assert.NotEqual("?", version);
        Assert.DoesNotContain('+', version); // no commit hash on the screen
    }
}
