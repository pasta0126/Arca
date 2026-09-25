// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Zones;

/// <summary>
/// A zone or corridor where lockers are placed (taquilles-i-zones). The rules that need to know about lockers are given
/// the counts they need, so the entity does not depend on them and can be tested without a database. Business rules
/// return a failure instead of throwing.
/// </summary>
public sealed class Zone
{
    Zone(Guid id, ZoneName name)
    {
        Id = id;
        Name = name.Value;
        NameKey = name.Key;
        IsActive = true;
    }

    /// <summary>Rebuilds a stored zone. Used by persistence, which has already validated it.</summary>
    public Zone(Guid id, string name, string nameKey, bool isActive)
    {
        Id = id;
        Name = name;
        NameKey = nameKey;
        IsActive = isActive;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string NameKey { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Creates an active zone. The name must not be the name of any other zone, active or not.</summary>
    public static Result<Zone> Create(Guid id, string? name, IEnumerable<Zone> existing)
    {
        var parsed = ZoneName.Parse(name);
        if (!parsed.IsSuccess)
        {
            return Result<Zone>.Failure(parsed.Error!);
        }

        return IsTaken(parsed.Value!.Key, existing, except: null)
            ? Result<Zone>.Failure(ZoneErrors.NameDuplicate(parsed.Value!.Value))
            : Result<Zone>.Success(new Zone(id, parsed.Value!));
    }

    /// <summary>Renames the zone with the same rules; a zone is never a duplicate of itself, so only case or accents may change.</summary>
    public Result<Zone> Rename(string? name, IEnumerable<Zone> all)
    {
        var parsed = ZoneName.Parse(name);
        if (!parsed.IsSuccess)
        {
            return Result<Zone>.Failure(parsed.Error!);
        }

        if (IsTaken(parsed.Value!.Key, all, except: this))
        {
            return Result<Zone>.Failure(ZoneErrors.NameDuplicate(parsed.Value!.Value));
        }

        Name = parsed.Value!.Value;
        NameKey = parsed.Value.Key;
        return Result<Zone>.Success(this);
    }

    /// <summary>Deactivates a zone that has no lockers that are not retired. Deactivating an inactive zone changes nothing.</summary>
    /// <param name="activeLockers">Lockers of the zone that are not retired.</param>
    public Result<Zone> Deactivate(int activeLockers)
    {
        if (activeLockers > 0)
        {
            return Result<Zone>.Failure(ZoneErrors.HasActiveLockers(activeLockers));
        }

        IsActive = false;
        return Result<Zone>.Success(this);
    }

    /// <summary>Reactivates the zone as long as its name is still not taken by another zone.</summary>
    public Result<Zone> Reactivate(IEnumerable<Zone> all)
    {
        if (IsTaken(NameKey, all, except: this))
        {
            return Result<Zone>.Failure(ZoneErrors.NameDuplicate(Name));
        }

        IsActive = true;
        return Result<Zone>.Success(this);
    }

    /// <summary>Whether the zone may be deleted: only one that has never had a locker, retired or not.</summary>
    /// <param name="everHadLockers">True if any locker, even a retired one, belongs or belonged to the zone.</param>
    public Result<Zone> CheckCanDelete(bool everHadLockers) =>
        everHadLockers ? Result<Zone>.Failure(ZoneErrors.HasHistory) : Result<Zone>.Success(this);

    static bool IsTaken(string key, IEnumerable<Zone> zones, Zone? except) =>
        zones.Any(z => z.Id != except?.Id && string.Equals(z.NameKey, key, StringComparison.Ordinal));
}
