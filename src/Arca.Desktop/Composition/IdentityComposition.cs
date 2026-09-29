// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Identity;
using Arca.Domain.Identity;
using Arca.Infrastructure.Inventory;
using Arca.UI.Identity;

namespace Arca.Desktop.Composition;

/// <summary>Joins the identity of the centre to its use cases: the composition root is the only place of the desktop project that knows them.</summary>
static class IdentityComposition
{
    public static IdentityServices Create(EfInventory store) => new(
        new GetCentreIdentityHandler(store.Identity).HandleAsync,
        new SaveCentreIdentityHandler(store.Identity, store).HandleAsync,
        bytes => CentreIdentity.CheckLogo(bytes).Error);
}
