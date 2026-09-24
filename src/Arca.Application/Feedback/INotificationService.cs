// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Feedback;

/// <summary>
/// Tells the user the result of an action without blocking them. Success disappears by itself; warnings and
/// errors stay until closed. The implementation lives in the interface (arquitectura-base, feedback-operacions).
/// </summary>
public interface INotificationService
{
    /// <param name="kind">How the notification behaves and looks.</param>
    /// <param name="text">The message, already in the user's language.</param>
    /// <param name="details">Optional technical details shown only on request.</param>
    void Publish(NotificationKind kind, string text, string? details = null);
}
