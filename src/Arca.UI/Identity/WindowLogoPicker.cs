// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Arca.UI.Identity;

/// <summary>Opens the file chooser of the system over the main window, for PNG and JPEG files.</summary>
public sealed class WindowLogoPicker(Func<Window?> owner, string title) : ILogoPicker
{
    public async Task<byte[]?> PickAsync()
    {
        if (owner()?.StorageProvider is not { } storage)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("PNG, JPEG") { Patterns = ["*.png", "*.jpg", "*.jpeg"] }],
        });
        if (files.Count == 0)
        {
            return null;
        }

        await using var stream = await files[0].OpenReadAsync();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
