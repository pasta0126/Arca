// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.ConceptAmounts;

/// <summary>Error codes of the amounts by concept. Resource keys: ConceptAmounts.Error.&lt;Name&gt;.</summary>
public static class ConceptAmountErrors
{
    /// <summary>Zero, negative, with more than two decimals, or over the maximum. Args: {0} the maximum amount.</summary>
    public static Error AmountInvalid(decimal maximum) => new("ConceptAmounts.AmountInvalid", Args: [maximum]);

    /// <summary>The year's end date has already passed, so its amounts cannot be changed.</summary>
    public static readonly Error YearFinished = new("ConceptAmounts.YearFinished");
}
