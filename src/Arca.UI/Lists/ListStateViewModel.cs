// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Windows.Input;
using Arca.Application.Localization;
using Arca.UI.Common;

namespace Arca.UI.Lists;

/// <summary>What a list shows now: the data is coming, it has rows, it has none, or a filter left none.</summary>
public enum ListViewState
{
    Loading,
    Content,
    Empty,
    NoResults,
}

/// <summary>An action an empty state offers: create the first one, or clear the filter.</summary>
public sealed record EmptyStateAction(string Label, ICommand Command);

/// <summary>
/// The state of a list while it loads and when it has nothing to show (ux-fonaments, components-de-feedback). Loading is
/// never shown as empty. An empty list explains what to do next and offers the main action if there is one; a filter with
/// no results says so and offers to clear it. The messages come from the screen (or from the guidance Application gives),
/// and the generic ones from resource keys.
/// </summary>
public sealed class ListStateViewModel(ILocalizer localizer) : ObservableObject
{
    ListViewState _state = ListViewState.Loading;
    string _message = string.Empty;
    IReadOnlyList<EmptyStateAction> _actions = [];

    public ListViewState State
    {
        get => _state;
        private set
        {
            if (Set(ref _state, value))
            {
                Raise(nameof(IsLoading));
                Raise(nameof(IsEmpty));
                Raise(nameof(IsContent));
            }
        }
    }

    public bool IsLoading => State == ListViewState.Loading;

    /// <summary>True for an empty list and for a filter without results: something to explain instead of rows.</summary>
    public bool IsEmpty => State is ListViewState.Empty or ListViewState.NoResults;

    public bool IsContent => State == ListViewState.Content;

    public string LoadingText => localizer.Get("Common.Loading.Generic");

    /// <summary>What the empty state says. Empty while there is nothing to explain.</summary>
    public string Message
    {
        get => _message;
        private set => Set(ref _message, value);
    }

    /// <summary>The actions offered under the message, if any.</summary>
    public IReadOnlyList<EmptyStateAction> Actions
    {
        get => _actions;
        private set => Set(ref _actions, value);
    }

    /// <summary>The data is being fetched.</summary>
    public void BeginLoading() => Apply(ListViewState.Loading, string.Empty, []);

    /// <summary>There are rows to show.</summary>
    public void ShowContent() => Apply(ListViewState.Content, string.Empty, []);

    /// <summary>There is nothing yet: explain what to do and offer the main action, if there is one.</summary>
    public void ShowEmpty(string? message = null, EmptyStateAction? mainAction = null) =>
        Apply(ListViewState.Empty, message ?? localizer.Get("Common.Empty.Nothing"), mainAction is null ? [] : [mainAction]);

    /// <summary>There is nothing yet and there are several ways to start: explain and offer each one.</summary>
    public void ShowEmpty(string? message, IReadOnlyList<EmptyStateAction> actions) =>
        Apply(ListViewState.Empty, message ?? localizer.Get("Common.Empty.Nothing"), actions);

    /// <summary>A filter left nothing: say so and offer to clear it.</summary>
    public void ShowNoResults(EmptyStateAction? clearFilter = null, string? message = null) =>
        Apply(ListViewState.NoResults, message ?? localizer.Get("Common.Empty.NoResults"), clearFilter is null ? [] : [clearFilter]);

    void Apply(ListViewState state, string message, IReadOnlyList<EmptyStateAction> actions)
    {
        Message = message;
        Actions = actions;
        State = state;
    }
}
