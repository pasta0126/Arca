// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;

namespace Arca.DbEditor;

// A generic editor for SQLite files (plain, SQLCipher 4 or the database of ARCA). Not part of the product.
//
//   dotnet run --project tools/Arca.DbEditor [-- <file>]
internal static class Program
{
    [STAThread]
    public static int Main(string[] args) =>
        AppBuilder.Configure<EditorApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
