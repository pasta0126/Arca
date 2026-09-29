// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Screens;

/// <summary>Opens a form for the person to fill in and completes when it is closed: saved, or cancelled. Tests answer it without any window.</summary>
public interface IFormDialogs
{
    /// <returns>True if the form was saved, false if it was cancelled.</returns>
    Task<bool> ShowAsync(IFormModel form);
}
