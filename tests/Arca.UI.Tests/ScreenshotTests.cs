// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Startup;
using Arca.Application.Storage;
using Arca.UI.Confirmation;
using Arca.UI.Info;
using Arca.UI.Startup;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>
/// Not a check: renders the screens to PNG files so the look can be reviewed without opening the application.
/// Runs only with ARCA_SCREENSHOT=&lt;folder&gt; (for example: ARCA_SCREENSHOT=/tmp/arca dotnet test).
/// </summary>
public sealed class ScreenshotTests
{
    static readonly ILocalizer _localizer = new ResxLocalizer();

    static void Take(Window window, string name)
    {
        var folder = Environment.GetEnvironmentVariable("ARCA_SCREENSHOT");
        Assert.SkipWhen(folder is null, "Set ARCA_SCREENSHOT=<folder> to render screenshots");
        Directory.CreateDirectory(folder!);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        window.CaptureRenderedFrame()!.Save(Path.Combine(folder!, name + ".png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
    }

    [AvaloniaFact]
    public void Main_window_with_information()
    {
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new InfoView(new InfoViewModel(new AppInfo("0.1.0-dev", "20260924104538_InitialCreate"), _localizer)));
        content.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(24, 0),
            Children =
            {
                new Button { Content = "Acció principal", IsDefault = true },
                new Button { Content = "Secundària" },
                new Button { Content = "Desactivada", IsEnabled = false },
                new TextBox { Text = "Cerca un alumne", Width = 200 },
                new CheckBox { Content = "Inclou baixes", IsChecked = true },
                new ComboBox { ItemsSource = new[] { "1r ESO", "2n ESO" }, SelectedIndex = 0, Width = 120 },
            },
        });
        Take(new Window { Width = 900, Height = 300, Content = content, Title = "ARCA" }, "main");
    }

    [AvaloniaFact]
    public void Splash_loading_and_error()
    {
        var loading = new SplashViewModel(_localizer);
        loading.Show(new StartupProgress("Startup.Stage.Migrating", 4, 4));
        Take(new SplashWindow(loading), "splash-loading");

        var failed = new SplashViewModel(_localizer);
        failed.ShowError(StorageErrors.PathNotAccessible("/dades/arca.db"));
        Take(new SplashWindow(failed), "splash-error");
    }

    [AvaloniaFact]
    public void Confirmation_destructive()
    {
        var request = new ConfirmationRequest(
            "Dona de baixa la taquilla", "La taquilla 15 es donarà de baixa. Aquesta acció no es pot desfer.", "Dona de baixa", true, ["1 taquilla"]);
        Take(new ConfirmationWindow(new ConfirmationViewModel(request, _localizer)), "confirmation");
    }
}
