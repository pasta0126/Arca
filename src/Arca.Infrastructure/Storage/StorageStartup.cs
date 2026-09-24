// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// The storage part of starting the application, in order: where the data is, whether the location works,
/// that no other instance has it, the key, and opening the database (migrating if needed).
/// Any failure releases what was taken and returns an error with a stable code.
/// </summary>
/// <param name="platform">Facts about the machine that decide the default location.</param>
/// <param name="keys">Where the database key comes from.</param>
/// <param name="createIfMissing">
/// Development only, until the first-run screen of configuracio-inicial exists: creates the database
/// when the file is missing instead of reporting it.
/// </param>
public sealed class StorageStartup(PlatformContext platform, IDatabaseKeyProvider keys, bool createIfMissing = false)
{
    public async Task<Result<StorageSession>> OpenAsync(CancellationToken ct = default)
    {
        var locations = DataLocations.Resolve(platform);
        var settings = new LocalSettingsStore(locations.SettingsFile).Load();
        var path = locations.DatabasePathFor(settings);

        if (string.IsNullOrWhiteSpace(settings.DatabasePath))
        {
            locations.EnsureDataFolder(); // only the folder ARCA owns; a path the user chose is never created
        }

        var valid = DatabasePathValidator.Validate(path);
        if (!valid.IsSuccess)
        {
            return Result<StorageSession>.Failure(valid.Error!);
        }

        path = valid.Value!;
        var instance = InstanceLock.TryAcquire(path);
        if (!instance.IsSuccess)
        {
            return Result<StorageSession>.Failure(instance.Error!);
        }

        var keyResult = await keys.GetKeyAsync(ct);
        if (!keyResult.IsSuccess)
        {
            instance.Value!.Dispose();
            return Result<StorageSession>.Failure(keyResult.Error!);
        }

        var key = keyResult.Value!;
        var exists = File.Exists(path);
        var opened = exists || !createIfMissing
            ? await ArcaDatabase.OpenAsync(path, key, ct)
            : await ArcaDatabase.CreateAsync(path, key, ct);
        if (!opened.IsSuccess)
        {
            key.Dispose();
            instance.Value!.Dispose();
            return Result<StorageSession>.Failure(opened.Error!);
        }

        await using var context = opened.Value!;
        var version = await StorageSession.ReadSchemaVersionAsync(context, ct);
        return Result<StorageSession>.Success(new StorageSession(path, instance.Value!, key, version, !exists));
    }
}
