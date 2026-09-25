// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// The screens that ask for the centre password while the application starts (acces-i-xifrat, D6). It keeps asking
/// until the password opens the key file, offers the recovery key when the password is forgotten, and reports a
/// cancellation as <see cref="KeyErrors.UnlockCancelled"/>, which closes the application. The interface implements it.
/// </summary>
public interface IUnlockFlow
{
    Task<Result<DatabaseKey>> UnlockAsync(string databasePath, CancellationToken ct);
}
