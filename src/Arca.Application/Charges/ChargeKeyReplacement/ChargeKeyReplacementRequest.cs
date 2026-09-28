// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Charges.ChargeKeyReplacement;

/// <param name="StudentId">The student who needs a new key.</param>
/// <param name="YearId">The year to generate the charge in, normally the active one.</param>
public sealed record ChargeKeyReplacementRequest(Guid StudentId, Guid YearId);
