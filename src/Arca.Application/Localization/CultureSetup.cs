// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Domain.Common;

namespace Arca.Application.Localization;

/// <summary>Fixes the culture for formatting and text to Catalan (Spain), ignoring the operating system's (v1).</summary>
public static class CultureSetup
{
    public static void Apply(CultureInfo? culture = null)
    {
        var target = culture ?? Cultures.Catalan;
        CultureInfo.DefaultThreadCurrentCulture = target;
        CultureInfo.DefaultThreadCurrentUICulture = target;
        CultureInfo.CurrentCulture = target;
        CultureInfo.CurrentUICulture = target;
    }
}
