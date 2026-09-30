// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Catalog.ListCatalog;
using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.ChangeStudentEnrollment;
using Arca.Application.Students.EditStudent;
using Arca.Application.Students.GetStudentScreen;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Students.ReactivateStudent;
using Arca.Domain.Common;

namespace Arca.UI.Students;

/// <summary>What the Students section reads and does, as the composition offers it: one delegate per use case, so the screen never builds one.</summary>
public sealed record StudentServices(
    Func<CancellationToken, Task<Result<StudentRowsListing>>> ListStudents,
    Func<CancellationToken, Task<Result<CatalogListing>>> ListCatalog,
    Func<Guid, CancellationToken, Task<Result<StudentScreenDetail>>> Detail,
    Func<Guid, CancellationToken, Task<Result<IReadOnlyList<string>>>> History,
    Func<AddStudentRequest, CancellationToken, Task<Result<StudentChangeResult>>> Add,
    Func<EditStudentRequest, CancellationToken, Task<Result<StudentDetail>>> Edit,
    Func<ChangeStudentEnrollmentRequest, CancellationToken, Task<Result<StudentChangeResult>>> ChangeEnrollment,
    Func<Guid, string?, CancellationToken, Task<Result<StudentDetail>>> Retire,
    Func<ReactivateStudentRequest, CancellationToken, Task<Result<StudentChangeResult>>> Reactivate,
    Func<Guid, CancellationToken, Task<Result<string>>> Release);
