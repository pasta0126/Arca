// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>One line of a student's history: the instant, the stable type and the text in the active language.</summary>
public sealed record StudentHistoryEntry(DateTimeOffset At, string Type, string Text);
