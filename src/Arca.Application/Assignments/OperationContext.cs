// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Assignments;

/// <summary>
/// What goes with an operation on assignments (alumnes-i-assignacions, D9): why it is done and optional extra data the
/// caller supplies, so later capabilities can ask for more information (for example, what happened to the key when a locker
/// is released or changed) without changing any signature.
/// </summary>
/// <param name="Reason">A free note or a stable reason for the operation.</param>
/// <param name="Data">Extra values, by name. Empty when there are none.</param>
public sealed record OperationContext(string? Reason = null, IReadOnlyDictionary<string, object?>? Data = null)
{
    public static OperationContext None { get; } = new();

    public object? Get(string name) => Data is not null && Data.TryGetValue(name, out var value) ? value : null;
}
