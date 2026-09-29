// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.ConceptAmounts;

/// <summary>Error codes of the amounts by concept. Resource keys: ConceptAmounts.Error.&lt;Name&gt;.</summary>
public static class ConceptAmountErrors
{
    /// <summary>Zero, negative, with more than two decimals, or over the maximum. Args: {0} the maximum amount, {1} the name of the concept the amount is for, so a form can mark that field.</summary>
    public static Error AmountInvalid(decimal maximum, ChargeConcept concept) => new("ConceptAmounts.AmountInvalid", Args: [maximum, concept.ToString()]);

    /// <summary>The year's end date has already passed, so its amounts cannot be changed.</summary>
    public static readonly Error YearFinished = new("ConceptAmounts.YearFinished");

    /// <summary>The amount of a concept needed right now has not been defined for the year yet.</summary>
    public static readonly Error NotDefined = new("ConceptAmounts.NotDefined");
}
