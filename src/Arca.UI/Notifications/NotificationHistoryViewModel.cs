// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;

namespace Arca.UI.Notifications;

/// <summary>One line of the history: what was said, of which kind, and when, in the computer's local time.</summary>
public sealed record NotificationHistoryEntry(NotificationKind Kind, string Text, string When);

/// <summary>
/// The notifications of this session, newest first, including the successes that already disappeared. It lives in memory
/// with the session: a new session starts empty and keeps nothing of the previous one (components-de-feedback).
/// </summary>
public sealed class NotificationHistoryViewModel(NotificationCenter center, ILocalizer localizer)
{
    public string Title => localizer.Get("Common.Label.NotificationHistory");

    /// <summary>The current history. Ask again after new notifications: it is a snapshot.</summary>
    public IReadOnlyList<NotificationHistoryEntry> Entries =>
        [.. center.History.Select(n => new NotificationHistoryEntry(n.Kind, n.Text, localizer.Format(n.At)))];
}
