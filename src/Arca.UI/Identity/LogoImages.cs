// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Media.Imaging;

namespace Arca.UI.Identity;

/// <summary>Reads the logo of the centre as an image: the same decoder that shows it says whether the file can be read at all.</summary>
public static class LogoImages
{
    /// <summary>The image, or null when the bytes are not an image that can be decoded.</summary>
    public static Bitmap? TryDecode(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or NotSupportedException)
        {
            return null;
        }
    }
}
