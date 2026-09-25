// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Students;

/// <summary>A student just created and the event of its creation, which is saved in the same operation.</summary>
public sealed record StudentCreated(Student Student, HistoryEvent Event);
