// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Encodings.Web;
using System.Text.Json;
using Arca.Domain.Common;

namespace Arca.Domain.Students;

/// <summary>
/// A student (alumnes-i-assignacions): one record with its own identity that lasts across school years, with a name,
/// surnames and an email that identifies them while they belong to the centre. It is either active or retired, and a
/// retired student can be reactivated with the same record. The level and group belong to the enrolment of each year,
/// not to the student. Every change returns the history event it originates. Business rules return a failure instead
/// of throwing.
/// </summary>
public sealed class Student
{
    public const int MaximumNameLength = 100;
    public const int MaximumReasonLength = 500;

    static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep accents readable in the stored history
    };

    /// <summary>Rebuilds a stored student. Used by persistence, which has already validated it.</summary>
    public Student(
        Guid id, string firstName, string lastName, string email, string nameKey, DateTimeOffset? retiredAtUtc, string? retirementReason)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        NameKey = nameKey;
        RetiredAtUtc = retiredAtUtc;
        RetirementReason = retirementReason;
    }

    public Guid Id { get; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    /// <summary>Trimmed and in lower case. Unique among all students, retired ones included.</summary>
    public string Email { get; private set; }

    /// <summary>First name and surnames without case, accents or repeated spaces, for searching. Not unique: namesakes are legitimate.</summary>
    public string NameKey { get; private set; }

    public DateTimeOffset? RetiredAtUtc { get; private set; }

    public string? RetirementReason { get; private set; }

    public bool IsRetired => RetiredAtUtc is not null;

    /// <summary>Creates an active student. The email must not belong to any other student, active or retired.</summary>
    public static Result<StudentCreated> Create(
        Guid id, string? firstName, string? lastName, string? email, IEnumerable<Student> existing, DateTimeOffset now)
    {
        var details = Validate(firstName, lastName, email);
        if (!details.IsSuccess)
        {
            return Result<StudentCreated>.Failure(details.Error!);
        }

        var (first, last, mail) = details.Value;
        if (existing.Any(s => s.Email == mail))
        {
            return Result<StudentCreated>.Failure(StudentErrors.EmailInUse);
        }

        var student = new Student(id, first, last, mail, KeyOf(first, last), null, null);
        var created = new HistoryEvent(
            id, StudentEventTypes.Created, now, null, Json(new { firstName = first, lastName = last, email = mail }));
        return Result<StudentCreated>.Success(new StudentCreated(student, created));
    }

    /// <summary>
    /// Corrects the name, the surnames or the email. The event keeps the old and the new value of what changed. The
    /// email must not belong to another student.
    /// </summary>
    public Result<HistoryEvent> UpdateDetails(
        string? firstName, string? lastName, string? email, IEnumerable<Student> all, DateTimeOffset now)
    {
        var details = Validate(firstName, lastName, email);
        if (!details.IsSuccess)
        {
            return Result<HistoryEvent>.Failure(details.Error!);
        }

        var (first, last, mail) = details.Value;
        if (first == FirstName && last == LastName && mail == Email)
        {
            return Result<HistoryEvent>.Failure(StudentErrors.Unchanged);
        }

        if (mail != Email && all.Any(s => s.Id != Id && s.Email == mail))
        {
            return Result<HistoryEvent>.Failure(StudentErrors.EmailInUse);
        }

        var before = new Dictionary<string, string>();
        var after = new Dictionary<string, string>();
        if (first != FirstName)
        {
            (before["firstName"], after["firstName"]) = (FirstName, first);
        }

        if (last != LastName)
        {
            (before["lastName"], after["lastName"]) = (LastName, last);
        }

        if (mail != Email)
        {
            (before["email"], after["email"]) = (Email, mail);
        }

        (FirstName, LastName, Email, NameKey) = (first, last, mail, KeyOf(first, last));
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, StudentEventTypes.DataChanged, now, Json(before), Json(after)));
    }

    /// <summary>Retires the student with a reason, keeping the record and its history. Freeing the locker is done by the use case.</summary>
    public Result<HistoryEvent> Retire(string? reason, DateTimeOffset now)
    {
        if (IsRetired)
        {
            return Result<HistoryEvent>.Failure(StudentErrors.AlreadyRetired);
        }

        var clean = reason?.Trim() ?? string.Empty;
        if (clean.Length == 0)
        {
            return Result<HistoryEvent>.Failure(StudentErrors.ReasonRequired);
        }

        if (clean.Length > MaximumReasonLength)
        {
            return Result<HistoryEvent>.Failure(StudentErrors.ReasonTooLong(MaximumReasonLength));
        }

        RetiredAtUtc = now;
        RetirementReason = clean;
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, StudentEventTypes.Retired, now, null, Json(new { reason = clean }), clean));
    }

    /// <summary>Reactivates a retired student with the same record. The enrolment of the active year is made by the use case.</summary>
    public Result<HistoryEvent> Reactivate(DateTimeOffset now)
    {
        if (!IsRetired)
        {
            return Result<HistoryEvent>.Failure(StudentErrors.AlreadyActive);
        }

        var before = new { retiredAtUtc = RetiredAtUtc, reason = RetirementReason };
        RetiredAtUtc = null;
        RetirementReason = null;
        return Result<HistoryEvent>.Success(new HistoryEvent(Id, StudentEventTypes.Reactivated, now, Json(before), null));
    }

    static Result<(string First, string Last, string Email)> Validate(string? firstName, string? lastName, string? email)
    {
        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;
        if (first.Length == 0)
        {
            return Result<(string, string, string)>.Failure(StudentErrors.FirstNameRequired);
        }

        if (last.Length == 0)
        {
            return Result<(string, string, string)>.Failure(StudentErrors.LastNameRequired);
        }

        if (first.Length > MaximumNameLength || last.Length > MaximumNameLength)
        {
            return Result<(string, string, string)>.Failure(StudentErrors.NameTooLong(MaximumNameLength));
        }

        var mail = EmailAddress.Normalize(email);
        return mail.IsSuccess
            ? Result<(string, string, string)>.Success((first, last, mail.Value!))
            : Result<(string, string, string)>.Failure(mail.Error!);
    }

    static string KeyOf(string first, string last) => TextComparer.Key(first + " " + last);

    static string Json(object value) => JsonSerializer.Serialize(value, _json);
}
