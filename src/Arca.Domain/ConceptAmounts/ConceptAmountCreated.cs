// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.ConceptAmounts;

/// <summary>An amount just defined for a concept and year, and its history event.</summary>
public sealed record ConceptAmountCreated(ConceptAmount Amount, HistoryEvent Event);
