// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.UI.Notifications;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Arca.Desktop.Composition;

/// <summary>Everything the running application needs: the services and the shutdown of what was opened at start.</summary>
public sealed class AppRuntime(ServiceProvider services, AppInfo info, MainWindowAccessor windows) : IAsyncDisposable
{
    public AppInfo Info { get; } = info;

    public ILocalizer Localizer => services.GetRequiredService<ILocalizer>();

    public NotificationCenter Notifications => services.GetRequiredService<NotificationCenter>();

    public IConfirmationService Confirmations => services.GetRequiredService<IConfirmationService>();

    /// <summary>Tells the confirmation dialogs which window to open over.</summary>
    public void SetMainWindow(Window window) => windows.Current = window;

    public ValueTask DisposeAsync() => services.DisposeAsync();
}

/// <summary>Holds the main window once it exists, so services built before it can still find their owner window.</summary>
public sealed class MainWindowAccessor
{
    public Window? Current { get; set; }
}
