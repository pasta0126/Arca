// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Domain.Common;

namespace Arca.UI.Startup;

/// <summary>What the user sees when the application cannot start: the cause and what to do, in their language.</summary>
public sealed class StartupErrorViewModel(Error error, ILocalizer localizer)
{
    public string WindowTitle => localizer.Get("App.Label.Title");

    public string Title => localizer.Get("App.Label.StartupErrorTitle");

    public string Message => localizer.Message(error);

    public string CloseLabel => localizer.Get("App.Label.Close");
}
