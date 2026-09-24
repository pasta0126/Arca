// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Common;

/// <summary>
/// A business error. The code is stable and independent of the language; the user-facing
/// message is resolved from a resource key derived from it (docs/convenciones.md, section 3).
/// </summary>
public sealed record Error(string Code, Severity Severity = Severity.Error, IReadOnlyList<object>? Args = null)
{
    public IReadOnlyList<object> Args { get; } = Args ?? [];
}
