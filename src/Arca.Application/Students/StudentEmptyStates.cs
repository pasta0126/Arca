// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;

namespace Arca.Application.Students;

/// <summary>Turns an empty state of the list of students into its guidance: what happened and how to go on.</summary>
public static class StudentEmptyStates
{
    public static StudentEmptyStateGuide? Describe(StudentEmptyState state, ILocalizer localizer) => state switch
    {
        StudentEmptyState.NoStudents => new StudentEmptyStateGuide(localizer.Get("Students.Empty.NoStudents"), [StudentSuggestedAction.AddStudent]),
        StudentEmptyState.NoStudentsWithoutLocker => new StudentEmptyStateGuide(localizer.Get("Students.Empty.NoStudentsWithoutLocker"), []),
        StudentEmptyState.NoResults => new StudentEmptyStateGuide(localizer.Get("Students.Empty.NoResults"), [StudentSuggestedAction.ClearFilters]),
        _ => null,
    };

    /// <summary>The label of a suggested action, for its button.</summary>
    public static string Label(StudentSuggestedAction action, ILocalizer localizer) => action switch
    {
        StudentSuggestedAction.AddStudent => localizer.Get("Students.Label.AddStudent"),
        StudentSuggestedAction.ClearFilters => localizer.Get("Students.Label.ClearFilters"),
        _ => string.Empty,
    };
}
