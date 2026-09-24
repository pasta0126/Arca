// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Desktop.Composition;
using Arca.UI.Startup;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.Themes.Fluent;

namespace Arca.Desktop;

public sealed class App : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The application ends when its window closes, and there is no window until startup has decided which one.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = StartAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    static async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var result = await Task.Run(() => AppStartup.StartAsync());
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Window window;
            int exitCode;
            if (result.IsSuccess)
            {
                var runtime = result.Value!;
                window = new MainWindow(runtime.Info, runtime.Localizer);
                window.Closed += async (_, _) => await runtime.DisposeAsync();
                exitCode = 0;
            }
            else
            {
                window = new StartupErrorWindow(new StartupErrorViewModel(result.Error!, new ResxLocalizer()));
                exitCode = 1;
            }

            window.Closed += (_, _) => desktop.Shutdown(exitCode);
            desktop.MainWindow = window;
            window.Show();
        });
    }
}
