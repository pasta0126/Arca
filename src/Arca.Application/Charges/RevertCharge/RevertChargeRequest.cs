// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.RevertCharge;

/// <param name="ChargeId">The paid, exempt or waived charge.</param>
/// <param name="Reason">Why it is reverted. Required.</param>
public sealed record RevertChargeRequest(Guid ChargeId, string? Reason);
