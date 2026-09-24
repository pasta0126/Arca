// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using System.Text;
using Arca.Application.Storage;

namespace Arca.Testing;

/// <summary>
/// Deterministic keys for tests only. They protect nothing real; the production key never comes from code
/// (it derives from the centre password, see acces-i-xifrat).
/// </summary>
public static class TestKeys
{
    public static DatabaseKey FromSeed(string seed) => new(SHA256.HashData(Encoding.UTF8.GetBytes(seed)));

    /// <summary>Key of the versioned sample file used to prove a file opens across systems.</summary>
    public static DatabaseKey Fixture() => FromSeed("arca-test-fixture-v1");
}
