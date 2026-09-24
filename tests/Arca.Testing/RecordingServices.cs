// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;

namespace Arca.Testing;

/// <summary>Collects notifications so a test can check what the user would have seen.</summary>
public sealed class RecordingNotifications : INotificationService
{
    public List<(NotificationKind Kind, string Text, string? Details)> Published { get; } = [];

    public void Publish(NotificationKind kind, string text, string? details = null) => Published.Add((kind, text, details));
}

/// <summary>An error log that remembers what it was asked to log and hands out predictable references.</summary>
public sealed class RecordingErrorLog : IErrorLog
{
    public List<(Exception Error, string Context)> Entries { get; } = [];

    public string LogUnexpected(Exception error, string context)
    {
        Entries.Add((error, context));
        return "REF" + Entries.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
