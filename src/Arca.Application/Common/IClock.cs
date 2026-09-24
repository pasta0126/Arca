// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Common;

/// <summary>
/// The only source of time for business code, so closings and due dates can be tested.
/// Calendar dates are DateOnly; instants are UTC.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Today's calendar date in the centre's time zone.</summary>
    DateOnly Today { get; }
}
