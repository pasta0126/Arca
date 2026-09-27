// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Enrollments;

/// <summary>An enrolment just created and the event that goes into the history of the student.</summary>
public sealed record EnrollmentCreated(Enrollment Enrollment, HistoryEvent Event);
