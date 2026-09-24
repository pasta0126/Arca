// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Common;

/// <summary>Waiting, behind an interface so timings such as the 300 ms busy indicator can be tested without real time.</summary>
public interface IDelay
{
    Task DelayAsync(TimeSpan time, CancellationToken ct);
}
