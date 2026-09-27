// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Assignments;

/// <summary>An assignment just opened, its two history events (student and locker) and whether it consumes a reservation.</summary>
public sealed record AssignmentOpened(Assignment Assignment, HistoryEvent StudentEvent, HistoryEvent LockerEvent, bool ConsumesReservation);
