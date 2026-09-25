// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>One line of a locker's history: the instant, the stable type and the text in the active language.</summary>
public sealed record LockerHistoryEntry(DateTimeOffset At, string Type, string Text);
