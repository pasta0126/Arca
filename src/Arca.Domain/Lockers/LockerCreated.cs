// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Lockers;

/// <summary>A locker just created and the history event of its creation, which is saved in the same operation.</summary>
public sealed record LockerCreated(Locker Locker, HistoryEvent Event);
