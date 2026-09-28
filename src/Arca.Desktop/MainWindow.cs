// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Desktop.Composition;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Info;
using Arca.UI.Layout;
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
        var registry = SectionRegistry.Compose(
            new Dictionary<string, Func<Avalonia.Controls.Control>> { [ShellCatalog.Settings] = () => SettingsRoot(runtime) },
            new Dictionary<string, Func<int>> { [ShellCatalog.Payments] = () => state.Current?.PendingCharges ?? 0 });
        var navigation = new NavigationViewModel(registry, runtime.Preferences, section => SectionPlaceholder.Create(section, registry, localizer));
        var shell = new ShellView(navigation, localizer, new NotificationHostView(runtime.Notifications, localizer));
        var header = new StackPanel();
        var headerView = new HeaderView(state, localizer, localizer.Get("App.Label.Title"));
        var search = new GlobalSearchViewModel(
            runtime.Search.HandleAsync, runtime.Delay, new ResultNotifier(runtime.Notifications, localizer, runtime.ErrorLog),
            new SearchNavigator(navigation), localizer);
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

    static ScreenView SettingsRoot(AppRuntime runtime)
    {
        var localizer = runtime.Localizer;
        var security = new CollapsibleSectionViewModel("security", runtime.Security.Title, () => runtime.Security.Note, runtime.Preferences);
        var content = new StackPanel
        {
            Children =
            {
                new InfoView(new InfoViewModel(runtime.Info, localizer)),
                new CollapsibleSectionView(security, new SecurityView(runtime.Security)),
            },
        };
        return new ScreenView(localizer.Get("Shell.Section.Settings"), [], content);
    }
}
