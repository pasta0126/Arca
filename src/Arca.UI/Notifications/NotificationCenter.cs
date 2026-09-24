// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Collections.ObjectModel;
using Arca.Application.Common;
using Arca.Application.Feedback;

namespace Arca.UI.Notifications;

/// <summary>One notification shown or remembered.</summary>
public sealed record Notification(Guid Id, NotificationKind Kind, string Text, string? Details, DateTimeOffset At);

/// <summary>
/// Notifications that never block the work: success disappears by itself after 5 seconds, warnings and errors
/// stay until the user closes them, and the session keeps a history, newest first (feedback-operacions).
/// </summary>
public sealed class NotificationCenter(IClock clock, IDelay delay) : INotificationService
{
    public static readonly TimeSpan SuccessLifetime = TimeSpan.FromSeconds(5);

    readonly List<Notification> _history = [];

    /// <summary>What is on screen now.</summary>
    public ObservableCollection<Notification> Visible { get; } = [];

    /// <summary>Every notification of this session, newest first.</summary>
    public IReadOnlyList<Notification> History => [.. Enumerable.Reverse(_history)];

    public void Publish(NotificationKind kind, string text, string? details = null)
    {
        var notification = new Notification(Guid.NewGuid(), kind, text, details, clock.UtcNow);
        _history.Add(notification);
        Visible.Add(notification);
        if (kind == NotificationKind.Success)
        {
            _ = DismissLaterAsync(notification.Id);
        }
    }

    /// <summary>Closes a notification on screen. It stays in the history.</summary>
    public void Dismiss(Guid id)
    {
        var found = Visible.FirstOrDefault(n => n.Id == id);
        if (found is not null)
        {
            Visible.Remove(found);
        }
    }

    async Task DismissLaterAsync(Guid id)
    {
        await delay.DelayAsync(SuccessLifetime, CancellationToken.None);
        Dismiss(id);
    }
}
