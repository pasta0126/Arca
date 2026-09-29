// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;

namespace Arca.UI.Commands;

/// <summary>
/// The one place that knows whether the application is working (pantalles-de-domini, Feedback y verificación): every run-once command
/// that lasts long enough to show its indicator (more than 300 ms) reports here, and the frame shows a single indicator while any
/// runs, so an action started from a button of a detail, which has no place of its own for an indicator, is never silent.
/// It lives on the interface thread, like the commands that report to it.
/// </summary>
public sealed class WorkHub : ObservableObject
{
    int _running;

    /// <summary>The hub the application uses.</summary>
    public static WorkHub Shared { get; } = new();

    /// <summary>How many long actions are running now.</summary>
    public int Running => _running;

    /// <summary>True while some long action is running.</summary>
    public bool IsBusy => _running > 0;

    public void Enter()
    {
        _running++;
        Raise(nameof(IsBusy));
    }

    public void Leave()
    {
        _running = Math.Max(0, _running - 1);
        Raise(nameof(IsBusy));
    }
}
