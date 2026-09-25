// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// The check that the recovery key was really written down (acces-i-xifrat, D4): the person types two of its five
/// groups, chosen at random, before being allowed to continue. Groups are read as tolerantly as the whole key.
/// </summary>
public sealed class RecoveryKeyChallenge
{
    public const int GroupsAsked = 2;

    readonly IReadOnlyList<string> _groups;

    /// <param name="recoveryKey">The key in canonical form.</param>
    /// <param name="indices">Zero-based groups to ask; random when omitted (tests pass them).</param>
    public RecoveryKeyChallenge(string recoveryKey, IReadOnlyList<int>? indices = null)
    {
        _groups = RecoveryKey.Groups(recoveryKey);
        Indices = indices ?? PickRandom(_groups.Count);
    }

    /// <summary>Zero-based positions of the groups asked, in ascending order. The screen says "group 2 and group 4".</summary>
    public IReadOnlyList<int> Indices { get; }

    /// <summary>Checks the groups typed, in the order of <see cref="Indices"/>.</summary>
    public Error? Verify(IReadOnlyList<string?>? typed)
    {
        if (typed is null || typed.Count != Indices.Count || typed.All(string.IsNullOrWhiteSpace))
        {
            return KeyErrors.ConfirmationRequired;
        }

        for (var i = 0; i < Indices.Count; i++)
        {
            if (!string.Equals(RecoveryKey.NormalizeGroup(typed[i]), _groups[Indices[i]], StringComparison.Ordinal))
            {
                return KeyErrors.ConfirmationIncorrect;
            }
        }

        return null;
    }

    static int[] PickRandom(int count)
    {
        var first = RandomNumberGenerator.GetInt32(count);
        int second;
        do
        {
            second = RandomNumberGenerator.GetInt32(count);
        }
        while (second == first);

        return first < second ? [first, second] : [second, first];
    }
}
