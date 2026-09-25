// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Localization;
using Arca.Application.Storage;
using Arca.UI.Info;
using Arca.Application.Startup;
using Arca.UI.Startup;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests;

public sealed class InfoAndStartupTests
{
    static readonly ILocalizer _localizer = new ResxLocalizer();

    static IEnumerable<string> Texts(Control root) =>
        root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty);

    [Fact]
    [Trait("spec", "arquitectura-base/distribucio-multiplataforma: Versión visible")]
    public void Info_model_exposes_both_versions_with_catalan_labels()
    {
        var model = new InfoViewModel(new AppInfo("0.1.0-dev", "20260924_InitialCreate"), _localizer);

        Assert.Equal("0.1.0-dev", model.ApplicationVersion);
        Assert.Equal("20260924_InitialCreate", model.SchemaVersion);
        Assert.Equal("Versió de l'aplicació", model.ApplicationVersionLabel);
        Assert.Equal("Versió de l'esquema de les dades", model.SchemaVersionLabel);
    }

    [AvaloniaFact]
    [Trait("spec", "arquitectura-base/distribucio-multiplataforma: Versión visible")]
    public void Info_view_shows_the_application_and_schema_versions()
    {
        var view = new InfoView(new InfoViewModel(new AppInfo("0.1.0-dev", "20260924_InitialCreate"), _localizer));
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var texts = Texts(view).ToList();

        Assert.Contains("0.1.0-dev", texts);
        Assert.Contains("20260924_InitialCreate", texts);
        Assert.Contains("Versió de l'aplicació", texts);
        Assert.Contains("Versió de l'esquema de les dades", texts);
    }

    [Theory]
    [InlineData("Storage.PathNotAccessible", "no existeix o no s'hi pot escriure")]
    [InlineData("Storage.Unreadable", "està malmès")]
    [InlineData("Storage.SchemaNewer", "versió més nova")]
    [InlineData("Storage.AlreadyRunning", "altra instància")]
    [Trait("spec", "arquitectura-base/feedback-operacions: Pantalla de arranque con etapas reales (fallo de arranque)")]
    public void Splash_error_gives_a_catalan_cause_action_and_the_code_as_reference(string code, string expected)
    {
        var error = code switch
        {
            "Storage.PathNotAccessible" => StorageErrors.PathNotAccessible("/dades/arca.db"),
            "Storage.Unreadable" => StorageErrors.Unreadable("/dades/arca.db"),
            "Storage.SchemaNewer" => StorageErrors.SchemaNewer,
            _ => StorageErrors.AlreadyRunning,
        };
        var model = new SplashViewModel(_localizer);

        model.ShowError(error);

        Assert.True(model.IsError);
        Assert.Contains(expected, model.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Storage.", model.Message, StringComparison.Ordinal);
        Assert.Equal("No s'ha pogut obrir ARCA", model.ErrorTitle);
        Assert.Equal("Referència: " + code, model.Reference);
    }

    [Fact]
    public void Splash_starts_in_loading_mode_and_shows_each_stage_text()
    {
        var model = new SplashViewModel(_localizer);

        Assert.False(model.IsError);
        Assert.Equal("Iniciant ARCA…", model.StageText);

        model.Show(new StartupProgress("Startup.Stage.Database", 4, 4));
        Assert.Equal("Obrint la base de dades…", model.StageText);

        model.Show(new StartupProgress("Startup.Stage.Migrating", 4, 4));
        Assert.Equal("Actualitzant la base de dades…", model.StageText);
    }

    [Fact]
    public void An_unexpected_failure_shows_the_log_reference()
    {
        var model = new SplashViewModel(_localizer);

        model.ShowError(Arca.Application.Common.CommonErrors.Unexpected("A7F3C9"), "A7F3C9");

        Assert.Contains("A7F3C9", model.Message, StringComparison.Ordinal);
        Assert.Equal("Referència: A7F3C9", model.Reference);
    }

    [AvaloniaFact]
    [Trait("spec", "arquitectura-base/feedback-operacions: Pantalla de arranque con etapas reales")]
    public void Splash_window_shows_the_stage_then_switches_to_the_error_without_closing()
    {
        var model = new SplashViewModel(_localizer);
        var window = new SplashWindow(model);
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        Dispatcher.UIThread.RunJobs();

        model.Show(new StartupProgress("Startup.Stage.Key", 3, 4));
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("Esperant la contrasenya per desbloquejar les dades…", Texts(window));
        Assert.False(window.CloseButton.IsEffectivelyVisible);

        model.ShowError(StorageErrors.PathNotAccessible("/dades/arca.db"));
        Dispatcher.UIThread.RunJobs();

        Assert.False(closed, "a failure must not close the window by itself");
        Assert.Contains("No s'ha pogut obrir ARCA", Texts(window));
        Assert.Contains("Referència: Storage.PathNotAccessible", Texts(window));
        Assert.True(window.CloseButton.IsEffectivelyVisible);
        Assert.True(window.CloseButton.IsFocused, "the close button is focused so Enter closes it");

        window.CloseButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.True(closed);
    }
}
