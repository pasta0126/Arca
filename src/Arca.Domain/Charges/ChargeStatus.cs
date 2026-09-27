// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Charges;

/// <summary>The state of a charge (pagaments, D1). A new charge is always pending.</summary>
public enum ChargeStatus
{
    /// <summary>Not paid yet. The only status that counts as debt.</summary>
    Pending,

    /// <summary>Paid on a date.</summary>
    Paid,

    /// <summary>Exempt with a reason, such as a grant.</summary>
    Exempt,

    /// <summary>Waived with a reason.</summary>
    Waived,

    /// <summary>Voided with a reason. Final: it cannot be reverted.</summary>
    Voided,
}
