// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Desktop.Composition;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Info;
using Arca.UI.Layout;
using Arca.UI.Notifications;
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

        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Avalonia.Controls.Control>>
        {
            [ShellCatalog.Settings] = () => SettingsRoot(runtime),
        });
        var navigation = new NavigationViewModel(registry, runtime.Preferences, section => SectionPlaceholder.Create(section, registry, localizer));
        var shell = new ShellView(navigation, localizer, new NotificationHostView(runtime.Notifications, localizer));
        shell.HeaderSlot.Content = ThemedText.Title(localizer.Get("App.Label.Title"));
        Content = shell;
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
