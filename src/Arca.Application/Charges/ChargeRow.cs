// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.ConceptAmounts;
using Arca.Domain.Charges;

namespace Arca.Application.Charges;

/// <summary>A charge as results and histories show it: who, which concept and year, its amount, status and reason.</summary>
public sealed record ChargeRow(
    Guid Id, Guid StudentId, string StudentName, ChargeConcept Concept, Guid YearId, string YearName,
    decimal Amount, ChargeStatus Status, DateOnly? PaidOn, string? Reason);
