// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>How many active students of the active year there are, and how many hold a locker, whatever the filters are.</summary>
public sealed record StudentCounters(int Active, int WithLocker, int WithoutLocker);
