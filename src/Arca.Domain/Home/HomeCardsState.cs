// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Home;

/// <summary>
/// What the centre remembers about its cards (targetes-d-inici, Tarjetas de serie): whether the cards every centre starts with were already
/// created, so one that the person deleted does not come back by itself. A single row.
/// </summary>
public sealed class HomeCardsState(Guid id, DateTimeOffset defaultsCreatedAtUtc)
{
    /// <summary>The state is a single row; this is its identity in the table.</summary>
    public static readonly Guid SingleId = new("4b2d9c70-8e15-4f3a-b6d1-7a0c5e2f9d38");

    public Guid Id { get; } = id;

    /// <summary>When the cards of a new centre were created.</summary>
    public DateTimeOffset DefaultsCreatedAtUtc { get; } = defaultsCreatedAtUtc;
}
