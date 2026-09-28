// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.VoidCharge;

/// <param name="ChargeId">The pending charge.</param>
/// <param name="Reason">Why it is voided. Required.</param>
public sealed record VoidChargeRequest(Guid ChargeId, string? Reason);
