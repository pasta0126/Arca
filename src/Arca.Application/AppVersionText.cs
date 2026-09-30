// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application;

/// <summary>
/// The version of the application as it is written on the screen (versio-de-l-aplicacio, Formato de la versión): MAJOR.MINOR.PATCH with the
/// suffix of a build that is not a published version, such as -dev, kept as it is. The build information that follows a plus sign (the
/// commit the build was made from) is left out. It is read from the one definition of the version of the project; nothing writes it by hand.
/// </summary>
public static class AppVersionText
{
    /// <summary>What is shown when the version cannot be read.</summary>
    public const string Unknown = "?";

    /// <summary>The text of a version without the build information, or <see cref="Unknown"/> when there is none.</summary>
    public static string Clean(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return Unknown;
        }

        var text = version.Trim();
        var plus = text.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? text : plus == 0 ? Unknown : text[..plus];
    }
}
