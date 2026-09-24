// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Common;

/// <summary>Errors shared by every capability. Resource keys: Common.Error.&lt;Name&gt;.</summary>
public static class CommonErrors
{
    /// <summary>A failure nobody planned for; the technical log has the details. Args: {0} short reference.</summary>
    public static Error Unexpected(string reference) => new("Common.Unexpected", Args: [reference]);
}
