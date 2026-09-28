// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Arca.UI.Preferences;

namespace Arca.UI.Layout;

/// <summary>
/// A section of a form or a screen that can be folded to give room: folded it shows only its title and a short summary of
/// what it holds. The person's choice is remembered on this computer, and a section that holds a validation error opens by
/// itself so the error is never hidden (adaptabilitat-i-disposicio, Secciones colapsables).
/// </summary>
/// <param name="name">A stable name, the key under which the state is remembered.</param>
/// <param name="title">The heading, already in the user's language.</param>
/// <param name="summary">Says what the section holds, shown when it is folded. Read each time, so it follows the data.</param>
/// <param name="preferences">Where the state is remembered.</param>
/// <param name="expandedByDefault">The state before the person ever changes it.</param>
public sealed class CollapsibleSectionViewModel(
    string name, string title, Func<string> summary, UiPreferencesSession preferences, bool expandedByDefault = true) : ObservableObject
{
    bool _isExpanded = preferences.IsSectionExpanded(name, expandedByDefault);
    bool _hasError;

    public string Title => title;

    /// <summary>What the folded section shows under its title; empty while it is expanded.</summary>
    public string Summary => IsExpanded ? string.Empty : summary();

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (Set(ref _isExpanded, value))
            {
                Raise(nameof(Summary));
                preferences.SetSectionExpanded(name, value);
            }
        }
    }

    /// <summary>Whether the section holds a validation error.</summary>
    public bool HasError => _hasError;

    /// <summary>
    /// Tells the section whether it holds a validation error. An error opens it, without changing what the person chose to
    /// remember, so once the error is fixed it goes back to how they keep it.
    /// </summary>
    public void SetError(bool hasError)
    {
        if (!Set(ref _hasError, hasError, nameof(HasError)))
        {
            return;
        }

        if (hasError && !_isExpanded)
        {
            Set(ref _isExpanded, true, nameof(IsExpanded));
            Raise(nameof(Summary));
        }
    }

    /// <summary>Folds or opens the section: what a click on its title or the keyboard does.</summary>
    public void Toggle() => IsExpanded = !IsExpanded;
}
