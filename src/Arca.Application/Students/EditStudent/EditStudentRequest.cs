// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students.EditStudent;

public sealed record EditStudentRequest(Guid StudentId, string? FirstName, string? LastName, string? Email);
