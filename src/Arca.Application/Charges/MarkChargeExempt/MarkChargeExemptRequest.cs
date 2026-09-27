// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.MarkChargeExempt;

/// <param name="ChargeId">The pending charge.</param>
/// <param name="Reason">Why it is exempt, such as a grant. Required.</param>
public sealed record MarkChargeExemptRequest(Guid ChargeId, string? Reason);
