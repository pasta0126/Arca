// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// Keeps two instances from opening the same database. The lock belongs to the database file, not to the
/// process, so an installed copy and a portable copy pointing at the same file also exclude each other
/// (arquitectura-base, D6). Any failure to take the lock, including on a network or synced folder, means "already in use".
/// </summary>
public sealed class InstanceLock : IDisposable
{
    const string Extension = ".lock";

    readonly FileStream _stream;

    InstanceLock(FileStream stream) => _stream = stream;

    public static Result<InstanceLock> TryAcquire(string databasePath)
    {
        var lockFile = Path.GetFullPath(databasePath) + Extension;
        try
        {
            var stream = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return Result<InstanceLock>.Success(new InstanceLock(stream));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Result<InstanceLock>.Failure(StorageErrors.AlreadyRunning);
        }
    }

    public void Dispose() => _stream.Dispose();
}
