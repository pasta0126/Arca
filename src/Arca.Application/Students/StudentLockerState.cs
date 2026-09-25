// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>Whether to keep the students that hold a locker, the ones that do not, or all.</summary>
public enum StudentLockerState
{
    Any,
    WithLocker,
    WithoutLocker,
}
