// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.WaiveChargesInBulk;

/// <param name="ChargeIds">The selected pending charges.</param>
/// <param name="Reason">The common reason. Required.</param>
public sealed record WaiveChargesInBulkRequest(IReadOnlyList<Guid> ChargeIds, string? Reason);
