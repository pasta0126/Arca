// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Localization;

namespace Arca.UI.Info;

/// <summary>The information screen: application version and the schema version of the open database.</summary>
public sealed class InfoViewModel(AppInfo info, ILocalizer localizer)
{
    public string Title => localizer.Get("App.Label.InfoTitle");

    public string ApplicationVersionLabel => localizer.Get("App.Label.ApplicationVersion");

    public string ApplicationVersion => info.ApplicationVersion;

    public string SchemaVersionLabel => localizer.Get("App.Label.SchemaVersion");

    public string SchemaVersion => info.SchemaVersion;
}
