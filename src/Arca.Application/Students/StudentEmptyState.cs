// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>Why the list of students has nothing to show, so the screen can guide instead of leaving it blank.</summary>
public enum StudentEmptyState
{
    /// <summary>There is something to show.</summary>
    None,

    /// <summary>No student is enrolled in the active year yet.</summary>
    NoStudents,

    /// <summary>Every active student already holds a locker.</summary>
    NoStudentsWithoutLocker,

    /// <summary>There are students, but none matches the filters.</summary>
    NoResults,
}
