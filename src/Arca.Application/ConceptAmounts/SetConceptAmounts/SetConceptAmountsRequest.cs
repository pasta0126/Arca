// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.ConceptAmounts.SetConceptAmounts;

/// <param name="YearId">The year the amounts are defined for.</param>
/// <param name="Fee">The annual fee.</param>
/// <param name="Deposit">The deposit.</param>
/// <param name="KeyReplacementFee">The key replacement fee.</param>
public sealed record SetConceptAmountsRequest(Guid YearId, decimal Fee, decimal Deposit, decimal KeyReplacementFee);
