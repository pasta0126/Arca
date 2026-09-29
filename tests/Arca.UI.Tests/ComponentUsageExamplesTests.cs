// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Actions;
using Arca.UI.Commands;
using Arca.UI.Layout;
using Arca.UI.Lists;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>
/// The smallest screen that uses every component, written the way <c>ui-shell</c> uses them; docs/componentes-ui.md quotes it.
/// If this stops working, the documentation is out of date.
/// </summary>
public sealed class ComponentUsageExamplesTests
{
    sealed record Zone(Guid Id, string Name);

    sealed class MemoryPreferences : IUiPreferencesStore
    {
        public UiPreferences Saved { get; private set; } = new();

        public UiPreferences Load() => Saved;

        public void Save(UiPreferences preferences) => Saved = preferences;
    }

    [AvaloniaFact]
    [Trait("spec", "ux-fonaments: ejemplo mínimo de uso de cada componente")]
    public async Task A_screen_built_from_the_components_works_as_a_whole()
    {
        // 1. Once per application, in the composition root.
        var localizer = new ResxLocalizer();
        var log = new RecordingErrorLog();
        var delay = new ManualDelay();
        var center = new NotificationCenter(new FakeClock(DateTimeOffset.UtcNow), delay);
        var registry = new ActionRegistry(localizer, UiPlatform.Windows);
        var store = new MemoryPreferences();
        var preferences = new UiPreferencesSession(store);

        // 2. A screen: a list of zones, a command that changes data, an action, a folded section.
        var zones = new ListViewModel<Zone, Guid>([new ListColumn<Zone>("name", "Zona", z => z.Name)], z => z.Id, localizer);
        var state = new ListStateViewModel(localizer);
        state.BeginLoading();
        zones.SetItems([new Zone(Guid.NewGuid(), "Planta 1"), new Zone(Guid.NewGuid(), "Planta 2")]);
        state.ShowContent();

        var create = new RunOnceCommand<int>(
            (_, _) => Task.FromResult(Result<int>.Success(40)), n => $"{n} taquilles creades", "CreateLockerRange", center, localizer, log, delay);
        var searchBox = new TextBox();
        var runs = 0;
        var search = registry[StandardActions.Search].Attach(() => { runs++; searchBox.Focus(); });
        var section = new CollapsibleSectionViewModel("filters", "Filtres", () => "Cap filtre actiu", preferences);

        var window = new Window
        {
            Width = 1100,
            Height = 700,
            Content = new Grid
            {
                Children =
                {
                    new AdaptivePanels(
                        new VirtualizedListView<Zone, Guid>(zones),
                        new CollapsibleSectionView(section, searchBox)),
                    new ListStateView(state),
                    new NotificationHostView(center, localizer),
                    ActionControls.Button(registry[StandardActions.Search]),
                },
            },
        };
        ShortcutDispatcher.Attach(window, registry);
        WindowStateKeeper.Attach(window, preferences);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // 3. Using it: a command reports its result, a shortcut runs the action, a section remembers its state.
        await create.RunAsync();
        window.KeyPress(Key.F, RawInputModifiers.Control, PhysicalKey.F, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, runs);
        Assert.Same(searchBox, window.FocusManager!.GetFocusedElement());
        section.Toggle();

        Assert.Equal("40 taquilles creades", Assert.Single(center.Visible).Text);

        // A screen that goes away lets go of its actions; the views bound to them settle before the test ends.
        search.Dispose();
        Dispatcher.UIThread.RunJobs();
        Assert.False(new UiPreferencesSession(store).IsSectionExpanded("filters", true));
        Assert.Equal("0 de 2", zones.SelectionText);
    }
}
