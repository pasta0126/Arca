// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Lockers;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.GetLockerScreen;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Zones.ListZoneRows;
using Arca.Domain.Common;

namespace Arca.UI.Lockers;

/// <summary>
/// What the Lockers section reads and does, as the composition offers it: one delegate per use case, so the screen never builds one.
/// Each operation that changes something answers with the sentence that says what was done.
/// </summary>
public sealed record LockerServices(
    Func<CancellationToken, Task<Result<LockerRowsListing>>> ListLockers,
    Func<Guid, CancellationToken, Task<Result<LockerScreenDetail>>> Detail,
    Func<Guid, CancellationToken, Task<Result<IReadOnlyList<string>>>> History,
    Func<CancellationToken, Task<Result<IReadOnlyList<ZoneRow>>>> ListZones,
    Func<AddLockerRequest, CancellationToken, Task<Result<string>>> Add,
    Func<CreateLockerRangeRequest, IProgress<OperationProgress>, CancellationToken, Task<Result<CreateLockerRangePlan>>> AnalyzeRange,
    Func<CreateLockerRangePlan, IProgress<OperationProgress>, CancellationToken, Task<Result<CreateLockerRangeResult>>> ApplyRange,
    Func<CreateLockerRangeResult, string> RangeSentence,
    Func<Guid, int, CancellationToken, Task<Result<string>>> ChangeNumber,
    Func<Guid, Guid, CancellationToken, Task<Result<string>>> ChangeZone,
    Func<Guid, string?, CancellationToken, Task<Result<string>>> Reserve,
    Func<Guid, CancellationToken, Task<Result<string>>> RemoveReservation,
    Func<Guid, OutOfServiceKindView, OutOfServiceDecisionView?, Guid?, bool, CancellationToken, Task<Result<OutOfServiceView>>> OutOfService,
    Func<Guid, CancellationToken, Task<Result<string>>> Restore,
    Func<Guid, CancellationToken, Task<Result<string>>> Retire,
    Func<string, CancellationToken, Task<Result<string>>> CreateZone,
    Func<Guid, string, CancellationToken, Task<Result<string>>> RenameZone,
    Func<Guid, CancellationToken, Task<Result<string>>> DeactivateZone,
    Func<Guid, CancellationToken, Task<Result<string>>> ReactivateZone,
    Func<Guid, CancellationToken, Task<Result<string>>> DeleteZone,
    Func<Guid, CancellationToken, Task<Result<string>>> Release);
