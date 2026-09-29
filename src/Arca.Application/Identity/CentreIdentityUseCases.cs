// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Domain.Common;
using Arca.Domain.Identity;

namespace Arca.Application.Identity;

/// <summary>The identity of the centre as the screens use it: what is defined, and the logo with its type.</summary>
public sealed record CentreIdentityView(string? Name, byte[]? Logo, string? LogoContentType, string? Accent)
{
    public static CentreIdentityView Empty { get; } = new(null, null, null, null);
}

/// <summary>What to do with the logo when the identity is saved.</summary>
public enum LogoChange
{
    /// <summary>Leave the logo as it is.</summary>
    Keep,

    /// <summary>Put the logo given.</summary>
    Replace,

    /// <summary>Remove the logo.</summary>
    Remove,
}

/// <param name="Name">The name of the centre; empty is refused.</param>
/// <param name="Change">What to do with the logo.</param>
/// <param name="Logo">The new logo, when it is replaced.</param>
/// <param name="Accent">The accent colour as #RRGGBB, or null for the default one.</param>
public sealed record SaveCentreIdentityRequest(string? Name, LogoChange Change, byte[]? Logo, string? Accent);

/// <summary>Reads the identity of the centre.</summary>
public sealed class GetCentreIdentityHandler(ICentreIdentityRepository identity)
{
    public async Task<Result<CentreIdentityView>> HandleAsync(CancellationToken ct)
    {
        var stored = await identity.GetAsync(ct);
        return Result<CentreIdentityView>.Success(stored is null ? CentreIdentityView.Empty : Of(stored));
    }

    internal static CentreIdentityView Of(CentreIdentity stored) => new(stored.Name, stored.Logo, stored.LogoContentType, stored.Accent);
}

/// <summary>
/// Saves the identity of the centre in one operation. Everything is checked before anything is saved: a refused logo leaves the
/// current one and the rest as they were.
/// </summary>
public sealed class SaveCentreIdentityHandler(ICentreIdentityRepository identity, IUnitOfWork unit)
{
    public Task<Result<CentreIdentityView>> HandleAsync(SaveCentreIdentityRequest request, CancellationToken ct) =>
        unit.RunAsync(async token =>
        {
            var name = CentreIdentity.CheckName(request.Name);
            if (!name.IsSuccess)
            {
                return Result<CentreIdentityView>.Failure(name.Error!);
            }

            var accent = CentreIdentity.CheckAccent(request.Accent);
            if (!accent.IsSuccess)
            {
                return Result<CentreIdentityView>.Failure(accent.Error!);
            }

            var stored = await identity.GetAsync(token);
            var created = stored is null;
            stored ??= new CentreIdentity(CentreIdentity.SingleId, null, null, null, null);
            byte[]? logo = stored.Logo;
            var contentType = stored.LogoContentType;
            if (request.Change == LogoChange.Replace)
            {
                var checkedLogo = CentreIdentity.CheckLogo(request.Logo ?? []);
                if (!checkedLogo.IsSuccess)
                {
                    return Result<CentreIdentityView>.Failure(checkedLogo.Error!);
                }

                (logo, contentType) = (request.Logo, checkedLogo.Value);
            }
            else if (request.Change == LogoChange.Remove)
            {
                (logo, contentType) = (null, null);
            }

            stored.Set(name.Value, logo, contentType, accent.Value);
            if (created)
            {
                await identity.AddAsync(stored, token);
            }
            else
            {
                await identity.UpdateAsync(stored, token);
            }

            return Result<CentreIdentityView>.Success(GetCentreIdentityHandler.Of(stored));
        }, ct);
}
