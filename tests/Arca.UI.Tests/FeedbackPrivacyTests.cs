// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Commands;
using Arca.UI.Notifications;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>What the person sees in a notification may include what they were working on; what is logged never does.</summary>
public sealed class FeedbackPrivacyTests
{
    const string Spec = "ux-fonaments/components-de-feedback: Notificaciones de resultado (Error inesperado)";

    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ResxLocalizer _localizer = new();
    readonly ManualDelay _delay = new();

    [Fact]
    [Trait("spec", Spec + " (sin datos de alumnos en el registro)")]
    public async Task An_unexpected_error_carrying_student_data_leaves_it_out_of_the_notification_and_of_the_log_context()
    {
        var command = new RunOnceCommand<int>(
            (_, _) => throw new InvalidOperationException("No es pot assignar la taquilla a Núria Garcia Puig (nuria@example.com)"),
            n => n.ToString(System.Globalization.CultureInfo.InvariantCulture), "AssignLocker", _notifications, _localizer, _log, _delay);

        await command.RunAsync();

        var only = Assert.Single(_notifications.Published);
        foreach (var forbidden in new[] { "Núria", "Garcia", "Puig", "nuria@example.com", "taquilla a" })
        {
            Assert.DoesNotContain(forbidden, only.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(forbidden, only.Details ?? string.Empty, StringComparison.Ordinal);
        }

        var entry = Assert.Single(_log.Entries);
        Assert.Equal("AssignLocker", entry.Context); // chosen in code, never built from what the person typed
    }

    [Fact]
    [Trait("spec", Spec + " (sin datos de alumnos en el registro)")]
    public void A_business_error_is_shown_to_the_person_but_never_written_to_the_technical_log()
    {
        var error = new Error("Students.EmailInUse", Args: ["nuria@example.com"]);

        new ResultNotifier(_notifications, _localizer, _log).Error(error);

        Assert.Empty(_log.Entries);
        Assert.Equal(NotificationKind.Error, Assert.Single(_notifications.Published).Kind); // the person is told, the log is not
    }

    [Fact]
    [Trait("spec", Spec + " (sin datos de alumnos en el registro)")]
    public void A_missing_text_logs_only_its_key_never_the_arguments_of_the_message()
    {
        var error = new Error("Nowhere.Missing", Args: ["Núria Garcia Puig"]);

        new ResultNotifier(_notifications, _localizer, _log).Error(error);

        var entry = Assert.Single(_log.Entries);
        Assert.Equal("MissingResource:Nowhere.Error.Missing", entry.Context);
        Assert.DoesNotContain("Núria", entry.Context, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + " (sin datos de alumnos en el registro)")]
    public void The_history_of_the_session_lives_in_memory_and_is_not_written_anywhere()
    {
        var center = new NotificationCenter(new FakeClock(DateTimeOffset.UtcNow), _delay);
        center.Publish(NotificationKind.Success, "Marta Puig té ara la taquilla 7.");

        Assert.Single(center.History);
        Assert.Empty(_log.Entries); // publishing a notification never touches the technical log
        Assert.Empty(new NotificationCenter(new FakeClock(DateTimeOffset.UtcNow), _delay).History); // a new session starts empty
    }
}
