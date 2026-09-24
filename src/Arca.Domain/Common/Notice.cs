// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Common;

/// <summary>A warning attached to a successful result. Stable code plus positional arguments, never translated text.</summary>
public sealed record Notice(string Code, IReadOnlyList<object>? Args = null)
{
    public IReadOnlyList<object> Args { get; } = Args ?? [];
}
