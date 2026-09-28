// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.MarkChargePaid;

/// <param name="ChargeId">The pending charge.</param>
/// <param name="PaidOn">The payment date; defaults to today and cannot be in the future.</param>
public sealed record MarkChargePaidRequest(Guid ChargeId, DateOnly? PaidOn = null);
