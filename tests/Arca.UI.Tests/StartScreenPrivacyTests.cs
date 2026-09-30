// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Search;
using Arca.UI.Shell;
using Avalonia.Controls;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>
/// What the caretaker sees on screen may name a student; what is written to the technical log never does (ui-shell, D3 and
/// the privacy of the search, the map and the notifications). Every read of the start screen and of the search is made to fail
/// with a message full of a student's data, and the notifications and the log are checked.
/// </summary>
public sealed class StartScreenPrivacyTests
{
    const string Spec = "ui-shell: privacidad de la búsqueda, el mapa y las notificaciones";

    static readonly string[] _personal = ["Núria", "Garcia", "Puig", "nuria@test.cat", "3r ESO"];

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();

    static IOException Leaky() => new IOException("No s'ha pogut llegir la fitxa de Núria Garcia Puig (nuria@test.cat) de 3r ESO");

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Load() => new();

        public void Save(UiPreferences preferences)
        {
        }
    }

    ResultNotifier Notifier() => new(_notifications, _localizer, _log);

    void AssertNothingPersonalWasLeaked(params string[] contexts)
    {
        Assert.Equal(contexts, _log.Entries.Select(e => e.Context));
        foreach (var published in _notifications.Published)
        {
            foreach (var word in _personal)
            {
                Assert.DoesNotContain(word, published.Text, StringComparison.Ordinal);
                Assert.DoesNotContain(word, published.Details ?? string.Empty, StringComparison.Ordinal);
            }
        }

        foreach (var context in _log.Entries.Select(e => e.Context))
        {
            foreach (var word in _personal)
            {
                Assert.DoesNotContain(word, context, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    [Trait("spec", Spec)]
    public async Task A_failing_search_leaves_no_student_data_in_the_notification_or_the_log()
    {
        var search = new GlobalSearchViewModel((_, _) => throw Leaky(), _delay, Notifier(), new SearchNavigator(), _localizer);

        search.Text = "garcia";
        await Task.Delay(20);
        _delay.Elapse(GlobalSearchViewModel.Pause + TimeSpan.FromMilliseconds(1));
        await Task.Delay(30);

        Assert.Single(_notifications.Published);
        AssertNothingPersonalWasLeaked("GlobalSearch");
    }

    [Fact]
    [Trait("spec", Spec)]
    public async Task A_failing_list_of_students_leaves_no_student_data_in_the_notification_or_the_log()
    {
        var students = new StudentsWithoutLockerViewModel(_ => throw Leaky(), Notifier(), _localizer);

        await students.LoadAsync();

        Assert.Single(_notifications.Published);
        AssertNothingPersonalWasLeaked("LoadStudentsWithoutLocker");
    }

    [Fact]
    [Trait("spec", Spec)]
    public void The_results_of_the_search_the_map_and_the_detail_hold_nothing_that_identifies_a_student_beyond_the_name()
    {
        var forbidden = new[] { "Email", "Dni", "Identifier", "Nif" };
        var types = new[]
        {
            typeof(StudentHit), typeof(LockerHit), typeof(GroupHit), typeof(GlobalSearchResult), typeof(MapLocker), typeof(ZoneMap),
            typeof(LockerMapData), typeof(LockerDetail), typeof(Arca.Application.Lockers.ListLockerRows.LockerListRow), typeof(StudentRow), typeof(AssignmentTargetCheck),
        };

        foreach (var type in types)
        {
            Assert.DoesNotContain(type.GetProperties(), p => forbidden.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    [Trait("spec", Spec)]
    public void The_notice_of_the_frame_and_the_header_say_nothing_about_any_student()
    {
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>());
        var navigation = new NavigationViewModel(registry, new UiPreferencesSession(new MemoryStore()), s => SectionPlaceholder.Create(s, registry, _localizer));
        var state = new GlobalStateService(_ => Task.FromResult(Result<Arca.Application.GlobalState.GlobalState>.Success(new(null, 5))), Notifier());
        _ = new GlobalNoticesViewModel(state, navigation, _localizer);

        Assert.Empty(_notifications.Published);
        Assert.Empty(_log.Entries);
    }
}
