// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Assignments;

/// <summary>Why an assignment was closed. The assignment stays in the history, closed, with its date and this reason.</summary>
public enum AssignmentCloseReason
{
    /// <summary>The locker was released by hand.</summary>
    Released,

    /// <summary>The student was moved to another locker.</summary>
    Changed,

    /// <summary>The student was retired.</summary>
    StudentRetired,

    /// <summary>The locker was put out of service and the student was freed.</summary>
    OutOfServiceReleased,

    /// <summary>The locker was put out of service and the student was given another one.</summary>
    OutOfServiceReassigned,
}
