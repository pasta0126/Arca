// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Students;

/// <summary>
/// The email of a student, which identifies them while they belong to the centre (alumnes-i-assignacions, D2). It is stored
/// trimmed and in lower case, so two spellings that differ only in case are the same address. The check is deliberately
/// simple: one "@", something on both sides and a dot in the domain, with no spaces.
/// </summary>
public static class EmailAddress
{
    public const int MaximumLength = 254;

    public static Result<string> Normalize(string? typed)
    {
        var email = typed?.Trim().ToLowerInvariant() ?? string.Empty;
        return IsValid(email) ? Result<string>.Success(email) : Result<string>.Failure(StudentErrors.EmailInvalid);
    }

    static bool IsValid(string email)
    {
        if (email.Length is 0 or > MaximumLength || email.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
        {
            return false;
        }

        var at = email.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != email.LastIndexOf('@'))
        {
            return false;
        }

        var domain = email[(at + 1)..];
        return domain.Length >= 3 && domain.Contains('.', StringComparison.Ordinal)
            && !domain.StartsWith('.') && !domain.EndsWith('.') && !domain.Contains("..", StringComparison.Ordinal);
    }
}
