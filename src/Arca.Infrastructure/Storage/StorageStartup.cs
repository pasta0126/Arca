// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Startup;
using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// The storage part of starting the application, as named stages (arquitectura-base, D13): where the data is,
/// whether that location works, that no other instance has it, the key, and opening the database (migrating if
/// needed). Any failure releases what was taken and returns an error with a stable code.
/// </summary>
/// <param name="platform">Facts about the machine that decide the default location.</param>
/// <param name="keys">Where the database key comes from.</param>
/// <param name="createIfMissing">
/// Development only, until the first-run screen of configuracio-inicial exists: creates the database
/// when the file is missing instead of reporting it.
/// </param>
public sealed class StorageStartup(PlatformContext platform, IDatabaseKeyProvider keys, bool createIfMissing = false)
{
    const int StageCount = 4;

    public async Task<Result<StorageSession>> OpenAsync(IProgress<StartupProgress>? progress = null, CancellationToken ct = default)
    {
        string path = string.Empty;
        InstanceLock? instance = null;
        DatabaseKey? key = null;
        StorageSession? session = null;

        var steps = new SyncProgress<string>(textKey => progress?.Report(new StartupProgress(textKey, StageCount, StageCount)));
        var sequence = new StartupSequence(
        [
            new StartupStage("Startup.Stage.Location", _ =>
            {
                var locations = DataLocations.Resolve(platform);
                var settings = new LocalSettingsStore(locations.SettingsFile).Load();
                path = locations.DatabasePathFor(settings);
                if (string.IsNullOrWhiteSpace(settings.DatabasePath))
                {
                    locations.EnsureDataFolder(); // only the folder ARCA owns; a path the user chose is never created
                }

                var valid = DatabasePathValidator.Validate(path);
                path = valid.Value ?? path;
                return Task.FromResult(valid.Error);
            }),
            new StartupStage("Startup.Stage.Instance", _ =>
            {
                var taken = InstanceLock.TryAcquire(path);
                instance = taken.Value;
                return Task.FromResult(taken.Error);
            }),
            new StartupStage("Startup.Stage.Key", async token =>
            {
                var result = await keys.GetKeyAsync(token);
                key = result.Value;
                return result.Error;
            }),
            new StartupStage("Startup.Stage.Database", async token =>
            {
                var exists = File.Exists(path);
                var opened = exists || !createIfMissing
                    ? await ArcaDatabase.OpenAsync(path, key!, steps, token)
                    : await ArcaDatabase.CreateAsync(path, key!, token);
                if (!opened.IsSuccess)
                {
                    return opened.Error;
                }

                await using var context = opened.Value!;
                var version = await StorageSession.ReadSchemaVersionAsync(context, token);
                session = new StorageSession(path, instance!, key!, version, !exists);
                return null;
            }),
        ]);

        var error = await sequence.RunAsync(progress, ct);
        if (error is null)
        {
            return Result<StorageSession>.Success(session!);
        }

        key?.Dispose();
        instance?.Dispose();
        return Result<StorageSession>.Failure(error);
    }

    /// <summary>Reports on the calling thread; Progress&lt;T&gt; would post to another one and lose the order in tests.</summary>
    sealed class SyncProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
