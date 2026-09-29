// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;

namespace Arca.UI.Shell;

/// <summary>A notice about the state of the application, with the action that deals with it.</summary>
/// <param name="Id">A stable name.</param>
/// <param name="Text">What it says, already in the user's language.</param>
/// <param name="ActionLabel">The button that does something about it.</param>
/// <param name="Act">What the button does.</param>
/// <param name="CanDismiss">False for a notice that blocks actions: it stays until the situation is solved.</param>
public sealed record GlobalNotice(string Id, string Text, string ActionLabel, Action Act, bool CanDismiss);

/// <summary>
/// The notices about the state of the application, shown without blocking the work (ui-shell, Avisos globales): each one with
/// its direct action. One that only informs can be closed and does not come back until the next start; one that blocks
/// actions cannot be closed, because the person must solve it. Only what exists in this milestone is here: there is no
/// notice about the guided setup, a closing year or a new version yet.
/// </summary>
public sealed class GlobalNoticesViewModel : ObservableObject
{
    readonly GlobalStateService _state;
    readonly NavigationViewModel _navigation;
    readonly ILocalizer _localizer;
    readonly HashSet<string> _dismissed = [];

    public GlobalNoticesViewModel(GlobalStateService state, NavigationViewModel navigation, ILocalizer localizer)
    {
        _state = state;
        _navigation = navigation;
        _localizer = localizer;
        state.Changed += (_, _) => Raise(nameof(Notices));
    }

    /// <summary>The notices to show now, most important first.</summary>
    public IReadOnlyList<GlobalNotice> Notices => Build();

    /// <summary>Closes a notice that can be closed, until the next start.</summary>
    public void Dismiss(string id)
    {
        if (Build().FirstOrDefault(n => n.Id == id) is { CanDismiss: true })
        {
            _dismissed.Add(id);
            Raise(nameof(Notices));
        }
    }

    List<GlobalNotice> Build()
    {
        var notices = new List<GlobalNotice>();
        if (_state.Current is { HasActiveYear: false })
        {
            notices.Add(new GlobalNotice(
                "NoActiveYear", _localizer.Get("Shell.Notice.NoActiveYear"), _localizer.Get("Shell.Notice.GoToCourse"),
                () => _navigation.Navigate(ShellCatalog.Course), CanDismiss: false)); // without a year nothing can be assigned
        }

        return [.. notices.Where(n => !_dismissed.Contains(n.Id))];
    }
}
