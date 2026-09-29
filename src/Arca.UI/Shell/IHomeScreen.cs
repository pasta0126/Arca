// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;

namespace Arca.UI.Shell;

/// <summary>
/// The screen of the Home section (ui-shell, D2): a piece that can be replaced. The map of lockers is the proposal until it is
/// validated with the caretakers; registering another implementation changes what Home shows without touching the navigation,
/// the search or any other section.
/// </summary>
public interface IHomeScreen
{
    /// <summary>Builds the screen. It is called the first time Home is opened and kept afterwards.</summary>
    Control Create();
}
