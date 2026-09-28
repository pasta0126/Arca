// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;

namespace Arca.UI.Notifications;

/// <summary>
/// The one place that turns a structured result, an error code or an unexpected exception into a notification
/// (ux-fonaments, D3). Application never builds text: it is composed here from resource keys. If the text of a message is
/// missing, the key itself is shown and the missing key is written once to the technical log, so it gets fixed.
/// </summary>
public sealed class ResultNotifier(INotificationService notifications, ILocalizer localizer, IErrorLog log)
{
    readonly HashSet<string> _reportedMissing = [];

    /// <summary>A result: what was done on success, followed by any warnings; the explained error on failure.</summary>
    public void Notify<T>(Result<T> result, Func<T, string> successText)
    {
        if (!result.IsSuccess)
        {
            Error(result.Error!);
            return;
        }

        notifications.Publish(NotificationKind.Success, successText(result.Value!));
        foreach (var notice in result.Notices)
        {
            notifications.Publish(NotificationKind.Warning, TextOf(ResourceKeys.For(notice), localizer.Message(notice)));
        }
    }

    /// <summary>A business error, with the cause and what to do, in plain language.</summary>
    public void Error(Error error) =>
        notifications.Publish(NotificationKind.Error, TextOf(ResourceKeys.For(error), localizer.Message(error)));

    /// <summary>The person cancelled a running operation.</summary>
    public void Cancelled() => notifications.Publish(NotificationKind.Warning, localizer.Get("Common.Result.Cancelled"));

    /// <summary>
    /// Something nobody planned for: a generic message with the reference of the technical log, and technical details on
    /// demand that never include student data (only the reference and the type of the exception).
    /// </summary>
    public void Unexpected(Exception exception, string context)
    {
        var reference = log.LogUnexpected(exception, context);
        notifications.Publish(
            NotificationKind.Error,
            localizer.Message(CommonErrors.Unexpected(reference)),
            localizer.Get("Common.Label.TechnicalDetails", reference, exception.GetType().FullName ?? exception.GetType().Name));
    }

    string TextOf(string key, string text)
    {
        if (text == key && _reportedMissing.Add(key))
        {
            log.LogUnexpected(new MissingResourceException(), "MissingResource:" + key);
        }

        return text;
    }
}

/// <summary>Logged when a message has no text in the active language. The key goes in the context, not here.</summary>
public sealed class MissingResourceException : Exception;
