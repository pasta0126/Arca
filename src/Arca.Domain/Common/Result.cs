// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Common;

/// <summary>
/// Structured outcome of a use case: data with its counts, warnings, or an error with a stable code.
/// Business rules never throw; they return a failure (arquitectura-base, D12).
/// </summary>
public sealed record Result<T>
{
    Result(T? value, IReadOnlyList<Notice> notices, Error? error)
    {
        Value = value;
        Notices = notices;
        Error = error;
    }

    public T? Value { get; }

    public IReadOnlyList<Notice> Notices { get; }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value, params Notice[] notices) => new(value, notices, null);

    public static Result<T> Failure(Error error) => new(default, [], error);
}
