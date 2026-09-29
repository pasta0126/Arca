// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Identity;

/// <summary>Error codes of the identity of the centre. Resource keys: Identity.Error.&lt;Name&gt;.</summary>
public static class IdentityErrors
{
    /// <summary>The name is empty.</summary>
    public static readonly Error NameRequired = new("Identity.NameRequired");

    /// <summary>The name has more than the maximum. Args: {0} the maximum.</summary>
    public static Error NameTooLong(int maximum) => new("Identity.NameTooLong", Args: [maximum]);

    /// <summary>The file is not a PNG or a JPEG.</summary>
    public static readonly Error LogoFormatNotSupported = new("Identity.LogoFormatNotSupported");

    /// <summary>The logo is over the maximum size. Args: {0} the maximum, in MB.</summary>
    public static Error LogoTooLarge(int maximumMegabytes) => new("Identity.LogoTooLarge", Args: [maximumMegabytes]);

    /// <summary>The file looks like an image but cannot be read.</summary>
    public static readonly Error LogoUnreadable = new("Identity.LogoUnreadable");

    /// <summary>The colour is not written as #RRGGBB.</summary>
    public static readonly Error AccentInvalid = new("Identity.AccentInvalid");
}
