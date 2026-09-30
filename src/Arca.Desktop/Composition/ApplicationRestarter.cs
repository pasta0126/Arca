// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Diagnostics;
using Arca.Application.Backup;
using Arca.UI.Shell;

namespace Arca.Desktop.Composition;

/// <summary>
/// Launches ARCA again after a restoration (copies-de-seguretat D13). The window is closed first, which is what releases the database and its
/// lock; only then does the new process start, so it finds the data free and asks for the password of the restored data.
/// </summary>
public sealed class ApplicationRestarter(MainWindowAccessor windows) : IApplicationRestarter
{
    public bool RestartRequested { get; private set; }

    public bool CanRestart => Command() is not null;

    public void Restart()
    {
        RestartRequested = true;
        windows.Current?.Close();
    }

    public void Close() => windows.Current?.Close();

    /// <summary>Starts the new process. Called once the database has been released; false when it could not start.</summary>
    public static bool Launch()
    {
        try
        {
            var command = Command();
            return command is not null && Process.Start(Start(command.Value)) is not null;
        }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return false;
        }
    }

    static ProcessStartInfo Start((string File, IReadOnlyList<string> Arguments) command)
    {
        var info = new ProcessStartInfo(command.File) { UseShellExecute = false };
        foreach (var argument in command.Arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return info;
    }

    /// <summary>What starts this same application: its own executable, or the host with the same program when it was started as «dotnet program.dll».</summary>
    static (string File, IReadOnlyList<string> Arguments)? Command()
    {
        var process = Environment.ProcessPath;
        if (string.IsNullOrEmpty(process) || !File.Exists(process))
        {
            return null;
        }

        var arguments = Environment.GetCommandLineArgs();
        var hosted = Path.GetFileNameWithoutExtension(process).Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        return (process, hosted ? arguments : arguments.Skip(1).ToList());
    }
}
