// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Access;

/// <summary>How a form ended: the person submitted it, chose its secondary action, or cancelled.</summary>
public enum FormOutcome
{
    Submitted,
    Secondary,
    Cancelled,
}
