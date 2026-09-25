// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Reflection;
using Arca.Application;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Security;
using Arca.Application.Startup;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Arca.UI.Access;
using Arca.UI.Confirmation;
using Arca.UI.Notifications;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Arca.Desktop.Composition;

/// <summary>
/// The composition root: the only place in Arca.Desktop that knows Infrastructure. It opens the storage and
/// registers the services; the rest of the application sees only Application and UI types.
/// </summary>
public static class AppStartup
{
    /// <summary>The technical log, available from the first instant so even a failure while starting gets a reference.</summary>
    public static IErrorLog CreateErrorLog() =>
        new FileErrorLog(DataLocations.Resolve(PlatformContext.Current()).LogFolder);

    /// <param name="log">The technical log.</param>
    /// <param name="startupWindow">The window the password screens open over, if it is showing.</param>
    public static async Task<Result<AppRuntime>> StartAsync(
        IErrorLog log, Func<Window?> startupWindow, IProgress<StartupProgress>? progress = null, CancellationToken ct = default)
    {
        var localizer = new ResxLocalizer();
        var access = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore());
        var flows = new AccessFlows(
            access, new WindowFormPresenter(startupWindow), localizer,
            (path, newAccess, groups, token) => DatabaseCreator.CreateAsync(path, newAccess, groups, token));
        var storage = new StorageStartup(PlatformContext.Current(), new PasswordKeyProvider(access, flows), firstRun: flows);

        var opened = await storage.OpenAsync(progress, ct);
        if (!opened.IsSuccess)
        {
            return Result<AppRuntime>.Failure(opened.Error!);
        }

        progress?.Report(new StartupProgress("Startup.Stage.Ready", 1, 1));
        var session = opened.Value!;
        var info = session.Info(ApplicationVersion());
        var windows = new MainWindowAccessor();
        var clock = new SystemClock();
        var delay = new SystemDelay();
        var notifications = new NotificationCenter(clock, delay);
        var services = new ServiceCollection()
            .AddSingleton<ILocalizer>(localizer)
            .AddSingleton(access)
            .AddSingleton(flows)
            .AddSingleton<IClock>(clock)
            .AddSingleton<IDelay>(delay)
            .AddSingleton(log)
            .AddSingleton(session)
            .AddSingleton(info)
            .AddSingleton(windows)
            .AddSingleton(notifications)
            .AddSingleton<INotificationService>(notifications)
            .AddSingleton<IConfirmationService>(new DialogConfirmationService(() => windows.Current, localizer))
            .BuildServiceProvider();
        return Result<AppRuntime>.Success(new AppRuntime(services, info, windows));
    }

    static string ApplicationVersion()
    {
        var text = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(AppStartup).Assembly.GetName().Version?.ToString()
            ?? "?";
        var plus = text.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? text : text[..plus];
    }
}
