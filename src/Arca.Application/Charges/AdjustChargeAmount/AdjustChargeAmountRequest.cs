// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.AdjustChargeAmount;

/// <param name="ChargeId">The pending charge.</param>
/// <param name="Amount">The new amount.</param>
/// <param name="Reason">Why the amount changes. Required.</param>
public sealed record AdjustChargeAmountRequest(Guid ChargeId, decimal Amount, string? Reason);
