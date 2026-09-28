// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.MarkDepositReturned;

/// <param name="ChargeId">The deposit that is due back.</param>
/// <param name="ReturnedOn">When it was given back; today if omitted, never in the future.</param>
/// <param name="Note">Optional, up to 500 characters.</param>
public sealed record MarkDepositReturnedRequest(Guid ChargeId, DateOnly? ReturnedOn, string? Note);
