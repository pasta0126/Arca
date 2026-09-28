// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.ConceptAmounts;

/// <summary>The types of history event of an amount by concept (pagaments). Stable codes, never translated text.</summary>
public static class ConceptAmountEventTypes
{
    public const string Created = "ConceptAmount.Created";
    public const string Changed = "ConceptAmount.Changed";

    public static IReadOnlyList<string> All { get; } = [Created, Changed];
}
