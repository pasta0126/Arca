// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Localization;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.Application.Students.ListStudentRows;
using Arca.Domain.Common;
using Arca.UI.Screens;

namespace Arca.UI.Assigning;

/// <summary>What the selectors of assignment read and do, as the composition offers it: one delegate per use case.</summary>
public sealed record AssignmentPickerServices(
    Func<CancellationToken, Task<Result<LockerRowsListing>>> ListLockers,
    Func<Guid?, CancellationToken, Task<Result<LockerSuggestion>>> Suggest,
    Func<Guid, Guid, CancellationToken, Task<Result<AssignmentTargetCheck>>> CheckTarget,
    Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> Assign,
    Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> Change,
    Func<CancellationToken, Task<Result<StudentRowsListing>>> ListStudents,
    Func<Guid, CancellationToken, Task<IReadOnlyList<string>>> DebtLines);

/// <summary>
/// The two selectors that assign a locker (pantalles-de-domini, D5), shared by the Students and Lockers sections and by every way
/// of asking: choose a locker for a student (a zone with its free lockers and the lowest number suggested), or choose a student
/// without a locker for a locker (with a search). Both end in the same warnings, the same impediments and the same result, since
/// both go through <see cref="AssignFlow"/>. An impediment is explained and nothing is assigned.
/// </summary>
public sealed class AssignmentDialogs(AssignmentPickerServices services, ScreenContext context)
{
    readonly AssignmentResultTexts _texts = new(context.Localizer);

    /// <summary>Opens the selector of a locker for a student who has none, or to change the one they have.</summary>
    public async Task ChooseLockerForAsync(Guid studentId, string studentName, bool isChange)
    {
        var text = context.Localizer;
        var listing = await services.ListLockers(default);
        if (!listing.IsSuccess)
        {
            context.Notifications.Publish(Arca.Application.Feedback.NotificationKind.Error, text.Message(listing.Error!));
            return;
        }

        var free = listing.Value!.Rows.Where(r => r.Status == LockerStatusView.Free).ToList();
        if (free.Count == 0 || context.Choices is null)
        {
            if (context.Choices is not null)
            {
                await context.Choices.ChooseAsync(new ChoiceRequest(
                    text.Get("Assignments.Label.PickerTitle", studentName), text.Get("Assignments.Empty.NoFreeLockers"), [], text.Get("Common.Label.Close")));
            }

            return;
        }

        var zones = free.GroupBy(r => r.ZoneId).OrderBy(g => g.First().ZoneName, TextComparer.Comparer)
            .Select(g => new FormOption(g.Key.ToString(), text.Get("Assignments.Label.ZoneOption", g.First().ZoneName, g.Count()))).ToList();
        var suggestion = await services.Suggest(null, default);
        var startZone = suggestion.IsSuccess ? suggestion.Value!.Locker.ZoneId : free[0].ZoneId;
        var zone = new FormFieldModel("Zone", text.Get("Lockers.Label.Zone"), zones) { Text = startZone.ToString() };
        var locker = new FormFieldModel("Locker", text.Get("Lockers.Label.Number"));
        FormViewModel<AssignLockerResult>? form = null;
        var request = 0;

        List<LockerListRow> InZone() => [.. free.Where(r => r.ZoneId.ToString() == zone.Text).OrderBy(r => r.Number)];

        async Task ShowCheckAsync()
        {
            var mine = ++request;
            if (isChange || !Guid.TryParse(locker.Text, out var lockerId))
            {
                return; // a change is checked when it is tried: the student already has a locker, which is what the check refuses
            }

            var check = await services.CheckTarget(studentId, lockerId, default);
            if (mine != request || form is null)
            {
                return;
            }

            form.Note = !check.IsSuccess ? text.Message(check.Error!)
                : check.Value!.Blocker is { } blocker ? text.Message(blocker)
                : check.Value.Warnings.Count > 0 ? string.Join(" ", check.Value.Warnings.Select(text.Message))
                : null;
        }

        void OnZone()
        {
            var options = InZone();
            locker.Options = [.. options.Select(r => new FormOption(r.Id.ToString(), text.Get("Assignments.Label.LockerOption", r.Number)))];
            var suggested = suggestion.IsSuccess && suggestion.Value!.Locker.ZoneId.ToString() == zone.Text
                ? options.FirstOrDefault(r => r.Id == suggestion.Value.Locker.Id) : null;
            locker.Text = (suggested ?? options.FirstOrDefault())?.Id.ToString() ?? string.Empty;
        }

        form = new FormViewModel<AssignLockerResult>(
            [zone, locker],
            async ct =>
            {
                if (!Guid.TryParse(locker.Text, out var lockerId))
                {
                    return Result<AssignLockerResult>.Failure(new Error("Assignments.TargetRequired"));
                }

                var intent = new AssignmentIntent(studentId, lockerId);
                var done = await AssignFlow.RunAsync(isChange ? services.Change : services.Assign, intent, context.Confirmations, text, ct, debtDetails: services.DebtLines);
                return done ?? Result<AssignLockerResult>.Failure(FormViewModel<AssignLockerResult>.Cancelled);
            },
            error => error.Code.StartsWith("Assignments.", StringComparison.Ordinal) || error.Code.StartsWith("Lockers.", StringComparison.Ordinal) ? "Locker" : null,
            done => isChange ? _texts.Changed(done.Assignment!) : _texts.Assigned(done.Assignment!), "AssignLocker", context.Notifications, text,
            context.Log, context.Delay, text.Get("Assignments.Label.PickerTitle", studentName), text.Get("Assignments.Label.PickerConfirm"),
            null, context.AfterWrite);
        zone.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FormFieldModel.Text))
            {
                OnZone();
            }
        };
        locker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FormFieldModel.Text))
            {
                _ = ShowCheckAsync();
            }
        };
        OnZone();
        await context.Forms.ShowAsync(form);
    }

    /// <summary>Opens the selector of a student without a locker for a free locker, with a search that narrows the students.</summary>
    public async Task ChooseStudentForAsync(Guid lockerId, int number, string zoneName)
    {
        var text = context.Localizer;
        var listing = await services.ListStudents(default);
        if (!listing.IsSuccess)
        {
            context.Notifications.Publish(Arca.Application.Feedback.NotificationKind.Error, text.Message(listing.Error!));
            return;
        }

        var students = listing.Value!.Rows.Where(r => !r.IsRetired && r.LockerNumber is null).ToList();
        var search = new FormFieldModel("Search", text.Get("Common.Action.Search"));
        var student = new FormFieldModel("Student", text.Get("Lockers.Label.Student"));
        FormViewModel<AssignLockerResult>? form = null;
        var request = 0;

        void Narrow()
        {
            var words = TextComparer.Key(search.Text).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matching = students
                .Where(s => words.All(w => TextComparer.Key(s.FirstName + " " + s.LastName).Contains(w, StringComparison.Ordinal)))
                .Select(s => new FormOption(s.Id.ToString(), text.Get("Assignments.Label.StudentOption", s.LastName, s.FirstName, s.LevelName ?? string.Empty, s.GroupName ?? string.Empty)))
                .ToList();
            student.Options = matching;
            student.Text = matching.Count > 0 ? matching[0].Id : string.Empty;
            if (form is not null)
            {
                form.Note = matching.Count == 0 ? text.Get("Assignments.Empty.NoStudentsFound") : null;
            }
        }

        async Task ShowCheckAsync()
        {
            var mine = ++request;
            if (!Guid.TryParse(student.Text, out var studentId))
            {
                return;
            }

            var check = await services.CheckTarget(studentId, lockerId, default);
            if (mine != request || form is null || check.IsSuccess && check.Value!.Blocker is null && check.Value.Warnings.Count == 0)
            {
                if (mine == request && form is not null)
                {
                    form.Note = null;
                }

                return;
            }

            form.Note = !check.IsSuccess ? text.Message(check.Error!)
                : check.Value!.Blocker is { } blocker ? text.Message(blocker)
                : string.Join(" ", check.Value.Warnings.Select(text.Message));
        }

        form = new FormViewModel<AssignLockerResult>(
            [search, student],
            async ct =>
            {
                if (!Guid.TryParse(student.Text, out var studentId))
                {
                    return Result<AssignLockerResult>.Failure(new Error("Students.NotFound"));
                }

                var done = await AssignFlow.RunAsync(services.Assign, new AssignmentIntent(studentId, lockerId), context.Confirmations, text, ct, debtDetails: services.DebtLines);
                return done ?? Result<AssignLockerResult>.Failure(FormViewModel<AssignLockerResult>.Cancelled);
            },
            error => error.Code.StartsWith("Assignments.", StringComparison.Ordinal) || error.Code.StartsWith("Students.", StringComparison.Ordinal) ? "Student" : null,
            done => _texts.Assigned(done.Assignment!), "AssignLocker", context.Notifications, text, context.Log, context.Delay,
            text.Get("Assignments.Label.StudentPickerTitle", number, zoneName), text.Get("Assignments.Label.PickerConfirm"), null, context.AfterWrite);
        search.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FormFieldModel.Text))
            {
                Narrow();
            }
        };
        student.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FormFieldModel.Text))
            {
                _ = ShowCheckAsync();
            }
        };
        Narrow();
        await context.Forms.ShowAsync(form);
    }
}
