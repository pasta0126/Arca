// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>The record of one student, opened to correct their data. It is the only place, with the import review, that shows the email.</summary>
public sealed record StudentDetail(
    Guid Id, string FirstName, string LastName, string Email, bool IsRetired, string? RetirementReason, DateTimeOffset? RetiredAtUtc,
    string? YearName, string? LevelName, string? GroupName, int? LockerNumber);
