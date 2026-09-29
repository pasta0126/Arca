// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Desktop.Composition;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Course;
using Arca.UI.Screens;
using Arca.UI.Identity;
using Arca.UI.Info;
using Arca.UI.Assigning;
using Arca.UI.Charges;
using Arca.UI.Lockers;
using Arca.UI.Students;
using Arca.UI.Layout;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Search;
using Arca.UI.Access;
using Arca.UI.Shell;
using Avalonia.Controls;

namespace Arca.Desktop;

/// <summary>
/// The main window: the frame of the application (sidebar, header and the open section) with the notifications over it.
/// Sections that do not have a screen yet show what they will hold; Settings already shows the version and the security of
/// the data.
/// </summary>
public sealed class MainWindow : Window
{
    public MainWindow(AppRuntime runtime)
    {
        var localizer = runtime.Localizer;
        Title = localizer.Get("App.Label.Title");
        WindowStateKeeper.Attach(this, runtime.Preferences);
        ShortcutDispatcher.Attach(this, runtime.Actions);

        var state = runtime.GlobalState;
        var navigator = new SearchNavigator();
        var notifier = new ResultNotifier(runtime.Notifications, localizer, runtime.ErrorLog);
        var themeSettings = new ThemeSettingsViewModel(runtime.Preferences, runtime.Identity, (choice, accent) => runtime.Theme?.Apply(choice, accent));
        themeSettings.ApplySaved(); // the theme chosen on this computer, with the accent of the centre
        var screenContext = new ScreenContext(
            localizer, runtime.Notifications, runtime.ErrorLog, runtime.Delay, runtime.Confirmations, runtime.Forms, () => state.RefreshAsync(), runtime.Choices);
        var homeModel = new LockerHomeModel(
            runtime.HomeServices, runtime.Preferences, notifier, runtime.Confirmations, localizer, runtime.Notifications, runtime.ErrorLog, runtime.Delay, state);
        var home = new LockerMapHomeScreen(homeModel, navigator, localizer);
        NavigationViewModel? navigation = null;
        var assignments = new AssignmentDialogs(runtime.Pickers, screenContext);
        var registry = SectionRegistry.Compose(
            new Dictionary<string, Func<Avalonia.Controls.Control>>
            {
                [ShellCatalog.Settings] = () => SettingsRoot(runtime, themeSettings, screenContext),
                [ShellCatalog.Lockers] = () => LockersSection.Create(runtime.LockerServices, screenContext, runtime.Actions[StandardActions.New], assignments),
                [ShellCatalog.Students] = () => StudentsView.Create(
                    new StudentsViewModel(runtime.StudentServices, screenContext, assignments, runtime.Actions[StandardActions.New],
                        () => Task.FromResult(navigation!.Navigate(ShellCatalog.Course)),
                        new StudentChargesViewModel(runtime.ChargeServices, screenContext, () => Task.FromResult(navigation!.Navigate(ShellCatalog.Course)))), localizer),
                [ShellCatalog.Payments] = () => PaymentsSection.Create(runtime.ChargeServices, screenContext, () => Task.FromResult(navigation!.Navigate(ShellCatalog.Course))),
                [ShellCatalog.Course] = () => CourseView.Create(
                    new CourseViewModel(runtime.CourseServices, screenContext, runtime.Actions[StandardActions.New]), localizer),
            },
            new Dictionary<string, Func<int>> { [ShellCatalog.Payments] = () => state.Current?.PendingCharges ?? 0 },
            home);
        navigation = new NavigationViewModel(registry, runtime.Preferences, section => SectionPlaceholder.Create(section, registry, localizer));
        navigator.Bind(navigation);
        var shell = new ShellView(navigation, localizer, new NotificationHostView(runtime.Notifications, localizer));
        var header = new StackPanel();
        var headerView = new HeaderView(state, localizer, localizer.Get("App.Label.Title"));
        headerView.ShowIdentity(runtime.Identity);
        var search = new GlobalSearchViewModel(
            runtime.Search.HandleAsync, runtime.Delay, notifier,
            navigator, localizer);
        var searchBox = new SearchBoxView(search, localizer);
        headerView.SearchSlot.Content = searchBox;
        runtime.Actions[StandardActions.Search].Attach(searchBox.FocusInput); // Control or Command + F, from any screen
        header.Children.Add(headerView);
        header.Children.Add(new NoticeBarView(new GlobalNoticesViewModel(state, navigation, localizer), localizer));
        shell.HeaderSlot.Content = header;
        Content = shell;

        // The frame reads the state when the application opens, and again after every write; it never asks on a timer.
        state.Changed += (_, _) => navigation.RefreshAttention();
        Opened += (_, _) => _ = state.RefreshAsync();
    }

    static ScreenView SettingsRoot(AppRuntime runtime, ThemeSettingsViewModel themeSettings, ScreenContext screenContext)
    {
        var localizer = runtime.Localizer;
        var security = new CollapsibleSectionViewModel("security", runtime.Security.Title, () => runtime.Security.Note, runtime.Preferences);
        var identity = new IdentityViewModel(
            runtime.IdentityServices, runtime.Identity, runtime.LogoPicker, localizer, runtime.Notifications, runtime.ErrorLog, runtime.Delay);
        var identitySection = new CollapsibleSectionViewModel("identity", localizer.Get("Identity.Section.Identity"), () => string.Empty, runtime.Preferences);
        var themeSection = new CollapsibleSectionViewModel("theme", localizer.Get("Identity.Section.Theme"), () => string.Empty, runtime.Preferences);
        var content = new StackPanel
        {
            Children =
            {
                new InfoView(new InfoViewModel(runtime.Info, localizer)),
                new CollapsibleSectionView(identitySection, IdentitySettingsView.Identity(identity, localizer)),
                new CollapsibleSectionView(themeSection, IdentitySettingsView.Theme(themeSettings, localizer)),
                new CollapsibleSectionView(security, new SecurityView(runtime.Security)),
            },
        };
        return new ScreenView(localizer.Get("Shell.Section.Settings"), [], content);
    }
}
