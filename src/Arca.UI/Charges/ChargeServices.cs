// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.GetStudentChargesScreen;
using Arca.Application.Charges.ListDebtors;
using Arca.Application.SchoolYears;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Zones.ListZoneRows;
using Arca.Domain.Common;

namespace Arca.UI.Charges;

/// <summary>
/// What the Payments section reads and does, as the composition offers it: one delegate per use case, so the screen never builds one.
/// Each operation that changes a charge answers with the sentence that says what was done.
/// </summary>
public sealed record ChargeServices(
    Func<Guid?, string?, Guid?, CancellationToken, Task<Result<DebtorsListing>>> ListDebtors,
    Func<CancellationToken, Task<Result<IReadOnlyList<AcademicYearSummary>>>> ListYears,
    Func<CancellationToken, Task<Result<IReadOnlyList<ZoneRow>>>> ListZones,
    Func<CancellationToken, Task<Result<StudentRowsListing>>> ListStudents,
    Func<Guid, CancellationToken, Task<Result<StudentChargesScreen>>> StudentCharges,
    Func<Guid, CancellationToken, Task<Result<IReadOnlyList<string>>>> ChargeHistory,
    Func<Guid, DateOnly?, CancellationToken, Task<Result<string>>> Pay,
    Func<Guid, string?, CancellationToken, Task<Result<string>>> Exempt,
    Func<Guid, string?, CancellationToken, Task<Result<string>>> Waive,
    Func<Guid, string?, CancellationToken, Task<Result<string>>> Void,
    Func<Guid, string?, CancellationToken, Task<Result<string>>> Revert,
    Func<Guid, decimal, string?, CancellationToken, Task<Result<string>>> Adjust,
    Func<Guid, Guid, CancellationToken, Task<Result<string>>> ChargeKeyReplacement,
    Func<DateOnly> Today);
