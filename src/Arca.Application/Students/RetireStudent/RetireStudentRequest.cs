// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students.RetireStudent;

/// <param name="Reason">Why the student leaves. It is required.</param>
public sealed record RetireStudentRequest(Guid StudentId, string? Reason);
