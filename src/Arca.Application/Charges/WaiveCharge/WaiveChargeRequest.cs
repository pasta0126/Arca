// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.WaiveCharge;

/// <param name="ChargeId">The pending charge.</param>
/// <param name="Reason">Why it is waived. Required.</param>
public sealed record WaiveChargeRequest(Guid ChargeId, string? Reason);
