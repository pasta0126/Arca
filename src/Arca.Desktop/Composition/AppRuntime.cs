// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Arca.Desktop.Composition;

/// <summary>Everything the running application needs: the services and the shutdown of what was opened at start.</summary>
public sealed class AppRuntime(ServiceProvider services, AppInfo info) : IAsyncDisposable
{
    public AppInfo Info { get; } = info;

    public ILocalizer Localizer => services.GetRequiredService<ILocalizer>();

    public ValueTask DisposeAsync() => services.DisposeAsync();
}
