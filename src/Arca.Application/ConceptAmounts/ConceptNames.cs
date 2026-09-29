// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;

namespace Arca.Application.ConceptAmounts;

/// <summary>The name of a concept from its stable name ("Fee", "Deposit", "KeyReplacementFee"), for screens that cannot use the domain type.</summary>
public static class ConceptNames
{
    /// <summary>The three concepts by their stable names, in the order the forms show them.</summary>
    public static IReadOnlyList<string> All { get; } = ["Fee", "Deposit", "KeyReplacementFee"];

    public static string Of(ILocalizer localizer, string concept) => concept switch
    {
        "Fee" => localizer.Get("Charges.Concept.Fee"),
        "Deposit" => localizer.Get("Charges.Concept.Deposit"),
        "KeyReplacementFee" => localizer.Get("Charges.Concept.KeyReplacementFee"),
        _ => concept,
    };
}
