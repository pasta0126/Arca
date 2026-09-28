// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.ConceptAmounts;

/// <summary>
/// The closed catalogue of charge concepts of v1 (pagaments): the annual fee, the deposit and the key replacement fee.
/// No other concept can be created.
/// </summary>
public enum ChargeConcept
{
    Fee,
    Deposit,
    KeyReplacementFee,
}
