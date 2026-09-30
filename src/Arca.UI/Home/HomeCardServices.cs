// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Home;
using Arca.Domain.Common;

namespace Arca.UI.Home;

/// <summary>What the start screen reads and does about its cards, as the composition offers it: one delegate per use case, so the screen never builds one.</summary>
public sealed record HomeCardServices(
    Func<CancellationToken, Task<Result<int>>> EnsureDefaults,
    Func<CancellationToken, Task<Result<int>>> RestoreDefaults,
    Func<CancellationToken, Task<Result<HomeCardsView>>> Load,
    Func<Guid, CancellationToken, Task<Result<ResolvedHomeCard>>> Resolve,
    Func<CreateHomeCardRequest, CancellationToken, Task<Result<HomeCardSaved>>> Create,
    Func<EditHomeCardRequest, CancellationToken, Task<Result<HomeCardSaved>>> Edit,
    Func<MoveHomeCardRequest, CancellationToken, Task<Result<bool>>> Move,
    Func<DeleteHomeCardRequest, CancellationToken, Task<Result<string>>> Delete,
    Func<CancellationToken, Task<Result<CardOptions>>> Options);
