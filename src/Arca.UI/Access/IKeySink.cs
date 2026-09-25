// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Access;

/// <summary>What the recovery key screen can do with the key besides showing it: copy it or print it. The window implements it.</summary>
public interface IKeySink
{
    Task CopyAsync(string text);

    /// <summary>Opens a page with the text, ready to print. Anything written for it is removed by <see cref="Cleanup"/>.</summary>
    Task PrintAsync(string title, IReadOnlyList<string> lines);

    /// <summary>Removes what printing left on disk. Called when the screen closes.</summary>
    void Cleanup();
}
