// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;

namespace Arca.Infrastructure.Common;

public sealed class SystemDelay : IDelay
{
    public Task DelayAsync(TimeSpan time, CancellationToken ct) => Task.Delay(time, ct);
}
