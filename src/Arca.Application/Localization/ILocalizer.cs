// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Domain.Common;

namespace Arca.Application.Localization;

/// <summary>
/// Turns stable codes and keys into text and formats values for the active language. Application and Domain
/// never produce text; the interface asks this (arquitectura-base, D8).
/// </summary>
public interface ILocalizer
{
    CultureInfo Culture { get; }

    /// <summary>The text of a key with positional arguments. An unknown key gives the key itself and never fails.</summary>
    string Get(string key, params object[] args);

    string Message(Error error);

    string Message(Notice notice);

    string Format(Money amount);

    string Format(DateOnly date);

    /// <summary>An instant, shown in the computer's local time.</summary>
    string Format(DateTimeOffset instant);
}
