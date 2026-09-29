// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Identity;

/// <summary>Lets the person choose an image file for the logo. Answers null when they cancel. Tests answer it without any window.</summary>
public interface ILogoPicker
{
    Task<byte[]?> PickAsync();
}
