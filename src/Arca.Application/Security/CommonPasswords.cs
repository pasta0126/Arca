// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Reflection;

namespace Arca.Application.Security;

/// <summary>
/// The list of common passwords embedded in the application (acces-i-xifrat, D5): words and keyboard runs that people
/// pick again and again, written for this project (Catalan, Spanish and English), one per line, without accents and in
/// lower case. Each entry is also matched when digits or signs are added at the end, as in "contrasenya1234".
/// </summary>
static class CommonPasswords
{
    const string ResourceName = "Arca.Application.Security.CommonPasswords.txt";

    static readonly Lazy<HashSet<string>> _entries = new(Load);

    public static bool Contains(string folded) => _entries.Value.Contains(folded);

    static HashSet<string> Load()
    {
        using var stream = typeof(CommonPasswords).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The list of common passwords is not embedded.");
        using var reader = new StreamReader(stream);
        var entries = new HashSet<string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            if (line.Length > 0 && line[0] != '#')
            {
                entries.Add(line);
            }
        }

        return entries;
    }
}
