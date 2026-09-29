// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.ConceptAmounts;
using Arca.Application.ConceptAmounts.GetConceptAmountsHistory;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.SchoolYears;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.SchoolYears.GetYearScreen;
using Arca.Domain.Common;

namespace Arca.UI.Course;

/// <summary>What the Course section reads and does, as the composition offers it: one delegate per use case, so the screen never builds one.</summary>
public sealed record CourseServices(
    Func<CancellationToken, Task<Result<IReadOnlyList<AcademicYearSummary>>>> List,
    Func<Guid, CancellationToken, Task<Result<YearScreenDetail>>> Detail,
    Func<CreateAcademicYearRequest, CancellationToken, Task<Result<AcademicYearSummary>>> Create,
    Func<Guid, CancellationToken, Task<Result<AcademicYearSummary>>> Activate,
    Func<Guid, CancellationToken, Task<Result<Guid>>> Delete,
    Func<Guid, CancellationToken, Task<Result<ConceptAmountsView>>> GetAmounts,
    Func<SetConceptAmountsRequest, CancellationToken, Task<Result<ConceptAmountsView>>> SetAmounts,
    Func<Guid, CancellationToken, Task<Result<IReadOnlyList<AmountHistoryLine>>>> AmountsHistory);
