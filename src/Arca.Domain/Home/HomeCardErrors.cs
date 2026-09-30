// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Home;

/// <summary>What is refused about a card of the start screen (targetes-d-inici).</summary>
public static class HomeCardErrors
{
    /// <summary>The title is empty.</summary>
    public static readonly Error TitleRequired = new("HomeCards.TitleRequired");

    /// <summary>The title has more than the maximum. Args: {0} the maximum.</summary>
    public static Error TitleTooLong(int maximum) => new("HomeCards.TitleTooLong", Args: [maximum]);

    /// <summary>The card filters by something the screen it opens does not have. Args: {0} the name of the criterion.</summary>
    public static Error CriterionUnknown(string name) => new("HomeCards.CriterionUnknown", Args: [name]);

    /// <summary>The value of a criterion is not one the screen understands. Args: {0} the name of the criterion.</summary>
    public static Error CriterionInvalid(string name) => new("HomeCards.CriterionInvalid", Args: [name]);

    /// <summary>There are already as many cards as there can be. Args: {0} the maximum.</summary>
    public static Error TooManyCards(int maximum) => new("HomeCards.TooManyCards", Args: [maximum]);

    /// <summary>The card does not exist (it was deleted meanwhile).</summary>
    public static readonly Error NotFound = new("HomeCards.NotFound");
}
