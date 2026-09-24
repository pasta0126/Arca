// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;

namespace Arca.Testing;

/// <summary>A delay that only finishes when the test moves virtual time forward, so timings are exact and instant.</summary>
public sealed class ManualDelay : IDelay
{
    readonly List<(TimeSpan Due, TaskCompletionSource Source)> _pending = [];
    readonly object _gate = new();

    public TimeSpan Now { get; private set; }

    public int PendingCount
    {
        get
        {
            lock (_gate)
            {
                return _pending.Count;
            }
        }
    }

    public Task DelayAsync(TimeSpan time, CancellationToken ct)
    {
        // Continuations run inline when time moves, so a test sees the effect as soon as Elapse returns.
        var source = new TaskCompletionSource();
        lock (_gate)
        {
            _pending.Add((Now + time, source));
        }

        ct.Register(() => source.TrySetCanceled(ct));
        return source.Task;
    }

    /// <summary>Moves time forward and completes the delays that are now due.</summary>
    public void Elapse(TimeSpan by)
    {
        List<TaskCompletionSource> due;
        lock (_gate)
        {
            Now += by;
            due = [.. _pending.Where(p => p.Due <= Now).Select(p => p.Source)];
            _pending.RemoveAll(p => p.Due <= Now);
        }

        foreach (var source in due)
        {
            source.TrySetResult();
        }
    }
}
