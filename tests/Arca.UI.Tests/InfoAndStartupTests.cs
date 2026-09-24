// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Localization;
using Arca.Application.Storage;
using Arca.UI.Info;
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
    [Trait("spec", "arquitectura-base/emmagatzematge-local: Fichero corrupto o clave incompatible")]
    public void Startup_error_model_gives_a_catalan_cause_and_action(string code, string expected)
    {
        var error = code switch
        {
            "Storage.PathNotAccessible" => StorageErrors.PathNotAccessible("/dades/arca.db"),
            "Storage.Unreadable" => StorageErrors.Unreadable("/dades/arca.db"),
            "Storage.SchemaNewer" => StorageErrors.SchemaNewer,
            _ => StorageErrors.AlreadyRunning,
        };

        var model = new StartupErrorViewModel(error, _localizer);

        Assert.Contains(expected, model.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Storage.", model.Message, StringComparison.Ordinal);
        Assert.Equal("No s'ha pogut obrir ARCA", model.Title);
        Assert.Equal("Tanca", model.CloseLabel);
    }

    [AvaloniaFact]
    public void Startup_error_window_shows_the_message_and_closes_from_the_button()
    {
        var model = new StartupErrorViewModel(StorageErrors.PathNotAccessible("/dades/arca.db"), _localizer);
        var window = new StartupErrorWindow(model);
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(model.Message, Texts(window));
        Assert.Contains("/dades/arca.db", model.Message, StringComparison.Ordinal);
        Assert.True(window.CloseButton.IsFocused, "the only button starts focused so Enter closes it");

        window.CloseButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.True(closed);
    }
}
