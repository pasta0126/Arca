// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;

namespace Arca.Domain.Common;

/// <summary>The culture used for formatting and text comparison. v1 is Catalan only, ignoring the system culture.</summary>
public static class Cultures
{
    public static CultureInfo Catalan { get; } = CultureInfo.ReadOnly(new CultureInfo("ca-ES"));
}
