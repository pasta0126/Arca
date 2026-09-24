// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Security;

/// <summary>
/// Cost of deriving a key from the password with Argon2id. They are stored in the key file so they can be
/// strengthened later without breaking files made before. The starting point is 64 MiB and 3 passes, calibrated
/// to stay under two seconds on a low-end computer (acces-i-xifrat, D2).
/// </summary>
/// <param name="MemoryKiB">Memory in KiB.</param>
/// <param name="Passes">Number of passes over the memory.</param>
/// <param name="Parallelism">Lanes; 1 keeps the time predictable on old machines.</param>
public sealed record Argon2Parameters(int MemoryKiB = 64 * 1024, int Passes = 3, int Parallelism = 1)
{
    public static Argon2Parameters Default { get; } = new();
}
