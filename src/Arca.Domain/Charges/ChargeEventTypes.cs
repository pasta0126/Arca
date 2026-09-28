// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Charges;

/// <summary>The types of history event of a charge (pagaments, D2). Stable codes, never translated text.</summary>
public static class ChargeEventTypes
{
    public const string Created = "Charge.Created";
    public const string Paid = "Charge.Paid";
    public const string Exempted = "Charge.Exempted";
    public const string Waived = "Charge.Waived";
    public const string Voided = "Charge.Voided";
    public const string Reverted = "Charge.Reverted";
    public const string AmountAdjusted = "Charge.AmountAdjusted";
    public const string ReturnDue = "Charge.ReturnDue";
    public const string ReturnCancelled = "Charge.ReturnCancelled";
    public const string Returned = "Charge.Returned";
    public const string ReturnReverted = "Charge.ReturnReverted";

    public static IReadOnlyList<string> All { get; } = [Created, Paid, Exempted, Waived, Voided, Reverted, AmountAdjusted, ReturnDue, ReturnCancelled, Returned, ReturnReverted];
}
