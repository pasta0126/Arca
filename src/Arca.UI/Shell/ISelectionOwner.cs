// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Shell;

/// <summary>What a screen can let go of when Esc reaches it: the element chosen in its list or map.</summary>
public interface ISelectionOwner
{
    /// <summary>True while an element is chosen.</summary>
    bool HasSelection { get; }

    /// <summary>Chooses none, which leaves the detail on its "choose one" state.</summary>
    void ClearSelection();
}
