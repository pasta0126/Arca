// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using System.Security.Cryptography;
using Arca.Application.Common;
using Serilog;
using Serilog.Core;

namespace Arca.Infrastructure.Common;

/// <summary>
/// Technical log in rotating files (arquitectura-base, D16). Each entry gets a short reference the user can quote.
/// Privacy: it records the type of the error, where it happened and the stack, and never the message, because an
/// exception message can carry the data being processed (a name, a query value).
/// </summary>
public sealed class FileErrorLog : IErrorLog, IDisposable
{
    public const int DefaultFileSizeLimitBytes = 1_000_000;
    public const int DefaultFilesToKeep = 5;

    readonly Logger _logger;

    /// <param name="folder">Where the log files go (created if missing).</param>
    /// <param name="fileSizeLimitBytes">A file rolls over when it reaches this size.</param>
    /// <param name="filesToKeep">Older files beyond this many are deleted, so the log never grows without limit.</param>
    public FileErrorLog(string folder, int fileSizeLimitBytes = DefaultFileSizeLimitBytes, int filesToKeep = DefaultFilesToKeep)
    {
        _logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(folder, "arca-.log"),
                rollingInterval: RollingInterval.Infinite,
                fileSizeLimitBytes: fileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: filesToKeep,
                shared: false,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger();
    }

    public string LogUnexpected(Exception error, string context)
    {
        var reference = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)); // 6 characters, easy to read aloud
        _logger.Error("ref={Reference} context={Context}\n{Description}", reference, context, Describe(error));
        return reference;
    }

    public void Dispose() => _logger.Dispose();

    /// <summary>Types and stack frames only, following inner exceptions. Messages and Data are never included.</summary>
    static string Describe(Exception error)
    {
        var parts = new List<string>();
        for (var current = error; current is not null; current = current.InnerException!)
        {
            parts.Add(current.GetType().FullName + "\n" + current.StackTrace);
            if (current.InnerException is null)
            {
                break;
            }
        }

        return string.Join("\n--- caused by ---\n", parts);
    }
}
