// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Charges;

/// <summary>Error codes of the charges. Resource keys: Charges.Error.&lt;Name&gt;.</summary>
public static class ChargeErrors
{
    /// <summary>The charge is not in the status the operation requires.</summary>
    public static readonly Error InvalidStatus = new("Charges.InvalidStatus");

    /// <summary>A paid, exempt or waived charge cannot be voided directly: it has to be reverted to pending first.</summary>
    public static readonly Error MustRevertFirst = new("Charges.MustRevertFirst");

    /// <summary>The payment date is after today.</summary>
    public static readonly Error DateInvalid = new("Charges.DateInvalid");

    /// <summary>The reason is empty or only whitespace.</summary>
    public static readonly Error ReasonRequired = new("Charges.ReasonRequired");

    /// <summary>The reason is longer than allowed. Args: {0} maximum length.</summary>
    public static Error ReasonTooLong(int maximum) => new("Charges.ReasonTooLong", Args: [maximum]);

    /// <summary>Zero, negative, with more than two decimals, or over the maximum. Args: {0} the maximum amount.</summary>
    public static Error AmountInvalid(decimal maximum) => new("Charges.AmountInvalid", Args: [maximum]);

    /// <summary>A returned deposit cannot be reverted to pending: the give-back has to be reverted first.</summary>
    public static readonly Error MustRevertReturnFirst = new("Charges.MustRevertReturnFirst");

    /// <summary>The deposit of a student who is still in the school is not given back: only when they leave.</summary>
    public static readonly Error StudentStillActive = new("Charges.StudentStillActive");

    /// <summary>A give-back cannot be undone while the student is back in the school: the deposit is only due back when they leave.</summary>
    public static readonly Error ReturnStudentActive = new("Charges.ReturnStudentActive");

    /// <summary>A give-back cannot be undone because the student already has another current deposit.</summary>
    public static readonly Error CurrentDepositExists = new("Charges.CurrentDepositExists");

    /// <summary>The give-back note is longer than allowed. Args: {0} maximum length.</summary>
    public static Error NoteTooLong(int maximum) => new("Charges.NoteTooLong", Args: [maximum]);

    /// <summary>There is no charge with that identity.</summary>
    public static readonly Error NotFound = new("Charges.NotFound");
}
