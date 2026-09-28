// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Charges;

/// <summary>
/// The cycle of a deposit, separate from its payment status (pagaments, D3): whether it was paid and whether it was given
/// back are different questions. Only a paid deposit ever leaves <see cref="None"/>.
/// </summary>
public enum DepositReturnStatus
{
    /// <summary>Nothing to give back: not a deposit, not paid, or the student is still in the school.</summary>
    None,

    /// <summary>Paid and the student has left: it has to be given back.</summary>
    ToReturn,

    /// <summary>Given back, with a date and an optional note.</summary>
    Returned,
}
