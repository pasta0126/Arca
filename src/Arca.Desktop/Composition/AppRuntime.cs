// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Search;
using Arca.Application.Localization;
using Arca.UI.Access;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Charges;
using Arca.UI.Identity;
using Arca.UI.Course;
using Arca.UI.Students;
using Arca.UI.Lockers;
using Arca.UI.Map;
using Arca.UI.Screens;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Shell;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Arca.Desktop.Composition;

/// <summary>Everything the running application needs: the services and the shutdown of what was opened at start.</summary>
public sealed class AppRuntime(ServiceProvider services, AppInfo info, MainWindowAccessor windows) : IAsyncDisposable
{
    public AppInfo Info { get; } = info;

    public ILocalizer Localizer => services.GetRequiredService<ILocalizer>();

    public SecurityViewModel Security => services.GetRequiredService<SecurityViewModel>();

    public UiPreferencesSession Preferences => services.GetRequiredService<UiPreferencesSession>();

    public GlobalStateService GlobalState => services.GetRequiredService<GlobalStateService>();

    public LockerHomeServices HomeServices => services.GetRequiredService<LockerHomeServices>();

    public CourseServices CourseServices => services.GetRequiredService<CourseServices>();

    public LockerServices LockerServices => services.GetRequiredService<LockerServices>();

    public StudentServices StudentServices => services.GetRequiredService<StudentServices>();

    public IdentityServices IdentityServices => services.GetRequiredService<IdentityServices>();

    public CentreIdentityModel Identity => services.GetRequiredService<CentreIdentityModel>();

    public ILogoPicker LogoPicker => services.GetRequiredService<ILogoPicker>();

    /// <summary>Puts the theme on the running application. Set by the application once it has one.</summary>
    public Arca.UI.Theme.ThemeManager? Theme { get; private set; }

    public void SetTheme(Arca.UI.Theme.ThemeManager theme) => Theme = theme;

    public ChargeServices ChargeServices => services.GetRequiredService<ChargeServices>();

    public AssignmentPickerServices Pickers => services.GetRequiredService<AssignmentPickerServices>();

    public IChoiceDialogs Choices => services.GetRequiredService<IChoiceDialogs>();

    public IFormDialogs Forms => services.GetRequiredService<IFormDialogs>();

    public GlobalSearchHandler Search => services.GetRequiredService<GlobalSearchHandler>();

    public IDelay Delay => services.GetRequiredService<IDelay>();

    public IErrorLog ErrorLog => services.GetRequiredService<IErrorLog>();

    public ActionRegistry Actions => services.GetRequiredService<ActionRegistry>();

    public NotificationCenter Notifications => services.GetRequiredService<NotificationCenter>();

    public IConfirmationService Confirmations => services.GetRequiredService<IConfirmationService>();

    /// <summary>Tells the confirmation dialogs which window to open over.</summary>
    public void SetMainWindow(Window window) => windows.Current = window;

    public ValueTask DisposeAsync() => services.DisposeAsync();
}

/// <summary>Holds the main window once it exists, so services built before it can still find their owner window.</summary>
public sealed class MainWindowAccessor
{
    public Window? Current { get; set; }
}
