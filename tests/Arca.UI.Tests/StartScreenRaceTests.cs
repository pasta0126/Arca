// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Assigning;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Search;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>Answers that arrive late, and failures outside a command, must never leave the screen showing or doing the wrong thing.</summary>
public sealed class StartScreenRaceTests
{
    const string Spec = "ui-shell/pantalla-principal: Carga y rendimiento del mapa";

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Load() => new();

        public void Save(UiPreferences preferences)
        {
        }
    }

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();

    static readonly Guid _a = Guid.NewGuid();
    static readonly Guid _b = Guid.NewGuid();

    [Fact]
    [Trait("spec", "ui-shell/navegacio-i-cerca: Búsqueda sin bloquear (Escritura rápida)")]
    public async Task A_search_that_fails_after_a_newer_one_started_does_not_close_the_panel_of_the_newer_one()
    {
        var release = new TaskCompletionSource<Result<GlobalSearchResult>>();
        var search = new GlobalSearchViewModel(
            (request, _) => request.Text == "ga" ? release.Task : Task.FromResult(Result<GlobalSearchResult>.Success(GlobalSearchResult.Empty)),
            _delay, new ResultNotifier(_notifications, _localizer, _log), new SearchNavigator(), _localizer);

        search.Text = "ga";
        await Task.Delay(20);
        _delay.Elapse(GlobalSearchViewModel.Pause + TimeSpan.FromMilliseconds(1)); // the first search is waiting for its answer
        await Task.Delay(20);
        search.Text = "gar"; // a newer one starts and is shown as "searching"
        await Task.Delay(20);
        release.SetException(new IOException("disk failed")); // the old one fails now
        await Task.Delay(40);

        Assert.Equal(SearchState.Searching, search.State); // the newer search keeps its panel
        Assert.Empty(_notifications.Published); // and nobody is told about a search that no longer matters
    }
}
