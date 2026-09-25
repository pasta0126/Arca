// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Application.Security;
using Arca.Application.Startup;
using Arca.Desktop.Composition;
using Arca.UI.Startup;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace Arca.Desktop;

public sealed class App : Avalonia.Application
{
    public override void Initialize()
    {
        // One theme in v1: light, pastel and neutral, whatever the operating system's own setting is.
        RequestedThemeVariant = ArcaTheme.Variant;
        Styles.Add(ArcaTheme.CreateFluent());
        Resources.MergedDictionaries.Add(ArcaTheme.CreateResources());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The application ends when the window that follows the splash closes, not when the splash does.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = StartAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    static async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var localizer = new ResxLocalizer();
        var splashModel = new SplashViewModel(localizer);
        var splash = new SplashWindow(splashModel);
        desktop.MainWindow = splash;
        splash.Show(); // visible before any costly stage starts
        splash.Closed += (_, _) =>
        {
            if (splashModel.IsError)
            {
                desktop.Shutdown(1);
            }
        };

        var log = AppStartup.CreateErrorLog();
        var progress = new Progress<StartupProgress>(splashModel.Show);
        try
        {
            var result = await Task.Run(() => AppStartup.StartAsync(log, () => splash, progress));
            if (!result.IsSuccess)
            {
                if (result.Error!.Code == KeyErrors.UnlockCancelled.Code)
                {
                    desktop.Shutdown(0); // the person cancelled the password: close without opening the data
                    return;
                }

                splashModel.ShowError(result.Error!);
                return;
            }

            var runtime = result.Value!;
            var main = new MainWindow(runtime.Info, runtime.Localizer);
            runtime.SetMainWindow(main);
            main.Closed += async (_, _) =>
            {
                await runtime.DisposeAsync();
                desktop.Shutdown(0);
            };
            desktop.MainWindow = main;
            main.Show();
            splash.Close();
        }
        catch (Exception e)
        {
            var reference = log.LogUnexpected(e, "Startup");
            splashModel.ShowError(CommonErrors.Unexpected(reference), reference);
        }
    }
}
