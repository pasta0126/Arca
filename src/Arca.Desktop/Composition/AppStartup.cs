// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Reflection;
using Arca.Application;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Startup;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Storage;
using Arca.UI.Confirmation;
using Arca.UI.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Arca.Desktop.Composition;

/// <summary>
/// The composition root: the only place in Arca.Desktop that knows Infrastructure. It opens the storage and
/// registers the services; the rest of the application sees only Application and UI types.
/// </summary>
public static class AppStartup
{
    /// <summary>Development only, until the first-run screen exists: create the database when it is missing.</summary>
    public const string CreateVariable = "ARCA_DEV_CREATE";

    /// <summary>The technical log, available from the first instant so even a failure while starting gets a reference.</summary>
    public static IErrorLog CreateErrorLog() =>
        new FileErrorLog(DataLocations.Resolve(PlatformContext.Current()).LogFolder);

    public static async Task<Result<AppRuntime>> StartAsync(
        IErrorLog log, IProgress<StartupProgress>? progress = null, CancellationToken ct = default)
    {
        var createIfMissing = Environment.GetEnvironmentVariable(CreateVariable) == "1";
        var storage = new StorageStartup(PlatformContext.Current(), new EnvironmentKeyProvider(), createIfMissing);

        var opened = await storage.OpenAsync(progress, ct);
        if (!opened.IsSuccess)
        {
            return Result<AppRuntime>.Failure(opened.Error!);
        }

        progress?.Report(new StartupProgress("Startup.Stage.Ready", 1, 1));
        var session = opened.Value!;
        var info = session.Info(ApplicationVersion());
        var windows = new MainWindowAccessor();
        var localizer = new ResxLocalizer();
        var clock = new SystemClock();
        var delay = new SystemDelay();
        var notifications = new NotificationCenter(clock, delay);
        var services = new ServiceCollection()
            .AddSingleton<ILocalizer>(localizer)
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
