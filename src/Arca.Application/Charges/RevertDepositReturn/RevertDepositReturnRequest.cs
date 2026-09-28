// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.RevertDepositReturn;

/// <param name="ChargeId">The deposit marked as given back.</param>
/// <param name="Reason">Why the give-back is undone. Required.</param>
public sealed record RevertDepositReturnRequest(Guid ChargeId, string? Reason);
