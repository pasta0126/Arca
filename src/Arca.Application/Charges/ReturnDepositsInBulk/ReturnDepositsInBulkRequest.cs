// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.ReturnDepositsInBulk;

/// <param name="ChargeIds">The selected deposits that are due back.</param>
/// <param name="ReturnedOn">The common date; today if omitted, never in the future.</param>
/// <param name="Note">The common optional note, up to 500 characters.</param>
public sealed record ReturnDepositsInBulkRequest(IReadOnlyList<Guid> ChargeIds, DateOnly? ReturnedOn, string? Note);
