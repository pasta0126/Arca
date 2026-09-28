// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Collections.ObjectModel;
using Arca.Application.Common;
using Arca.Application.Feedback;

namespace Arca.UI.Notifications;

/// <summary>One notification shown or remembered.</summary>
public sealed record Notification(Guid Id, NotificationKind Kind, string Text, string? Details, DateTimeOffset At);

/// <summary>
/// Notifications that never block the work: success disappears by itself after 5 seconds (and waits while the mouse is
/// over it), warnings and errors stay until the user closes them, and the session keeps a history, newest first
/// (feedback-operacions). At most <paramref name="maxVisible"/> are on screen; the rest wait in a queue and appear as
/// room is made, so a burst of notifications never covers the screen or hides another one.
/// </summary>
public sealed class NotificationCenter(IClock clock, IDelay delay, int maxVisible = NotificationCenter.DefaultMaxVisible) : INotificationService
{
    public const int DefaultMaxVisible = 5;

    public static readonly TimeSpan SuccessLifetime = TimeSpan.FromSeconds(5);

    readonly List<Notification> _history = [];
    readonly Queue<Notification> _waiting = new();
    readonly Dictionary<Guid, CancellationTokenSource> _timers = [];

    /// <summary>What is on screen now, oldest first.</summary>
    public ObservableCollection<Notification> Visible { get; } = [];

    /// <summary>Every notification of this session, newest first, including the ones still waiting to appear.</summary>
    public IReadOnlyList<Notification> History => [.. Enumerable.Reverse(_history)];

    /// <summary>How many are waiting for room on screen.</summary>
    public int WaitingCount => _waiting.Count;

    public void Publish(NotificationKind kind, string text, string? details = null)
    {
        var notification = new Notification(Guid.NewGuid(), kind, text, details, clock.UtcNow);
        _history.Add(notification);
        if (Visible.Count < maxVisible)
        {
            Show(notification);
        }
        else
        {
            _waiting.Enqueue(notification);
        }
    }

    /// <summary>Closes a notification on screen, or removes it from the queue. It stays in the history.</summary>
    public void Dismiss(Guid id)
    {
        StopTimer(id);
        var found = Visible.FirstOrDefault(n => n.Id == id);
        if (found is null)
        {
            var kept = _waiting.Where(n => n.Id != id).ToList();
            _waiting.Clear();
            kept.ForEach(_waiting.Enqueue);
            return;
        }

        Visible.Remove(found);
        if (_waiting.Count > 0 && Visible.Count < maxVisible)
        {
            Show(_waiting.Dequeue());
        }
    }

    /// <summary>The mouse is over a notification: a success stops counting down so it can be read.</summary>
    public void Pause(Guid id) => StopTimer(id);

    /// <summary>The mouse left: a success starts its countdown again, in full.</summary>
    public void Resume(Guid id)
    {
        if (Visible.FirstOrDefault(n => n.Id == id) is { Kind: NotificationKind.Success })
        {
            StartTimer(id);
        }
    }

    void Show(Notification notification)
    {
        Visible.Add(notification);
        if (notification.Kind == NotificationKind.Success)
        {
            StartTimer(notification.Id);
        }
    }

    void StartTimer(Guid id)
    {
        StopTimer(id);
        var source = new CancellationTokenSource();
        _timers[id] = source;
        _ = DismissLaterAsync(id, source.Token);
    }

    void StopTimer(Guid id)
    {
        if (_timers.Remove(id, out var source))
        {
            source.Cancel();
        }
    }

    async Task DismissLaterAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await delay.DelayAsync(SuccessLifetime, ct);
            Dismiss(id);
        }
        catch (OperationCanceledException)
        {
            // Paused or closed by hand before the time was up.
        }
    }
}
