// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// Where the key file of a database is read and written (acces-i-xifrat, D3). Writing is atomic and keeps the
/// previous version until a later unlock proves the current one works. Infrastructure implements it.
/// </summary>
public interface IKeyFileStore
{
    /// <summary>A missing, damaged or newer-version file is reported without touching anything.</summary>
    Result<KeyFile> Read(string databasePath);

    /// <summary>Replaces the key file atomically. Throws <see cref="IOException"/> if it cannot; the old file is then intact.</summary>
    void Write(string databasePath, KeyFile file);

    /// <summary>Removes the previous version once the current one has been used successfully.</summary>
    void DiscardPrevious(string databasePath);
}
