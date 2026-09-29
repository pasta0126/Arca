// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Common;

/// <summary>
/// Errors of what a person typed in a form before it is a value at all: a text that is not a date or not a number. The rules
/// about the value (range, overlaps) stay in Domain. Args: {0} the name of the field the error is about, so the form marks it.
/// Resource keys: Common.Error.&lt;Name&gt;.
/// </summary>
public static class FormErrors
{
    public static Error DateInvalid(string field) => new("Common.DateInvalid", Args: [field]);

    public static Error NumberInvalid(string field) => new("Common.NumberInvalid", Args: [field]);
}
