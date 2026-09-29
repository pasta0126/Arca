// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.RegularExpressions;
using Arca.Domain.Common;

namespace Arca.Domain.Identity;

/// <summary>
/// The identity of the centre (ui-shell, D7): its name, its logo and its accent colour. There is one of them in the database, so it
/// travels with the backups. Every part is optional: a centre without a name shows the name of the application, without a logo shows
/// only the name, and without an accent uses the default one.
/// </summary>
public sealed partial class CentreIdentity
{
    public const int MaximumNameLength = 100;

    /// <summary>The largest logo accepted, in bytes: 1 MB.</summary>
    public const int MaximumLogoBytes = 1024 * 1024;

    /// <summary>The identity is a single row; this is its identity in the table.</summary>
    public static readonly Guid SingleId = new("0d3f7a9e-5c1b-4a52-9e0d-1c2b3a4d5e6f");

    public CentreIdentity(Guid id, string? name, byte[]? logo, string? logoContentType, string? accent)
    {
        Id = id;
        Name = name;
        Logo = logo;
        LogoContentType = logoContentType;
        Accent = accent;
    }

    public Guid Id { get; }

    public string? Name { get; private set; }

    public byte[]? Logo { get; private set; }

    /// <summary>"image/png" or "image/jpeg" when there is a logo.</summary>
    public string? LogoContentType { get; private set; }

    /// <summary>"#RRGGBB" when the centre chose an accent colour, or null for the default one.</summary>
    public string? Accent { get; private set; }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex AccentShape();

    /// <summary>Checks a name: from 1 to 100 characters once trimmed.</summary>
    public static Result<string> CheckName(string? name)
    {
        var clean = name?.Trim() ?? string.Empty;
        return clean.Length == 0 ? Result<string>.Failure(IdentityErrors.NameRequired)
            : clean.Length > MaximumNameLength ? Result<string>.Failure(IdentityErrors.NameTooLong(MaximumNameLength))
            : Result<string>.Success(clean);
    }

    /// <summary>Checks an accent colour: null for the default one, or "#RRGGBB".</summary>
    public static Result<string?> CheckAccent(string? accent) =>
        accent is null || AccentShape().IsMatch(accent) ? Result<string?>.Success(accent?.ToUpperInvariant()) : Result<string?>.Failure(IdentityErrors.AccentInvalid);

    /// <summary>
    /// Checks a logo by what it is, not by its name: the first bytes must be those of a PNG or a JPEG and it must not exceed 1 MB. It
    /// answers the content type. Whether the image can be decoded is asked of the interface, which has the decoder.
    /// </summary>
    public static Result<string> CheckLogo(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumLogoBytes)
        {
            return Result<string>.Failure(IdentityErrors.LogoTooLarge(MaximumLogoBytes / (1024 * 1024)));
        }

        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return Result<string>.Success("image/png");
        }

        return bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF
            ? Result<string>.Success("image/jpeg")
            : Result<string>.Failure(IdentityErrors.LogoFormatNotSupported);
    }

    /// <summary>Puts what was checked in place.</summary>
    public void Set(string? name, byte[]? logo, string? logoContentType, string? accent)
    {
        Name = name;
        Logo = logo;
        LogoContentType = logo is null ? null : logoContentType;
        Accent = accent;
    }
}
