// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Security;

/// <summary>Asks the person for the centre password while the application starts (acces-i-xifrat, D6). The screen implements it.</summary>
public interface IUnlockPrompt
{
    /// <param name="previousFailure">Null on the first ask; true when the last password was wrong, so the screen says so.</param>
    /// <returns>The password typed, or null when the person cancels, which closes the application.</returns>
    Task<string?> AskPasswordAsync(bool previousFailure, CancellationToken ct);
}
