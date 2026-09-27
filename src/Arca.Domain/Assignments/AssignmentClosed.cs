// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Assignments;

/// <summary>The two history events (student and locker) of closing an assignment.</summary>
public sealed record AssignmentClosed(HistoryEvent StudentEvent, HistoryEvent LockerEvent);
