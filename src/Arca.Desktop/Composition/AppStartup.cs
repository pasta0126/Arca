// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Reflection;
using Arca.Application;
using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Arca.Desktop.Composition;

/// <summary>
/// The composition root: the only place in Arca.Desktop that knows Infrastructure. It opens the storage and
/// registers the services; the rest of the application sees only Application types.
/// </summary>
public static class AppStartup
{
    /// <summary>Development only, until the first-run screen exists: create the database when it is missing.</summary>
    public const string CreateVariable = "ARCA_DEV_CREATE";

    public static async Task<Result<AppRuntime>> StartAsync(CancellationToken ct = default)
    {
        var createIfMissing = Environment.GetEnvironmentVariable(CreateVariable) == "1";
        var storage = new StorageStartup(PlatformContext.Current(), new EnvironmentKeyProvider(), createIfMissing);

        var opened = await storage.OpenAsync(ct);
        if (!opened.IsSuccess)
        {
            return Result<AppRuntime>.Failure(opened.Error!);
        }

        var session = opened.Value!;
        var info = session.Info(ApplicationVersion());
        var services = new ServiceCollection()
            .AddSingleton<ILocalizer>(new ResxLocalizer())
            .AddSingleton<IClock, SystemClock>()
            .AddSingleton(session)
            .AddSingleton(info)
            .BuildServiceProvider();
        return Result<AppRuntime>.Success(new AppRuntime(services, info));
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
