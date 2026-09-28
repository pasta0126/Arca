// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Windows.Input;
using Arca.UI.Common;

namespace Arca.UI.Actions;

/// <summary>
/// One thing the person can do, as an object shared by everything that offers it (ux-fonaments, D11): the button, the
/// menu entry, the context menu and the keyboard shortcut all use this same action, so its name, order, shortcut and
/// availability can never differ between them. An action with no handler attached (the current screen does not offer it)
/// is unavailable and does nothing, silently.
/// </summary>
public sealed class AppAction : ObservableObject, ICommand
{
    Action? _handler;
    Func<Availability>? _availability;

    /// <param name="id">A stable name.</param>
    /// <param name="label">What the button and the menu say, already in the user's language.</param>
    /// <param name="shortcut">The fixed key combination, if the action has one.</param>
    /// <param name="shortcutText">How the shortcut is written next to the action.</param>
    public AppAction(string id, string label, Shortcut? shortcut = null, string shortcutText = "")
    {
        Id = id;
        Label = label;
        Shortcut = shortcut;
        ShortcutText = shortcutText;
    }

    public event EventHandler? CanExecuteChanged;

    public string Id { get; }

    public string Label { get; }

    public Shortcut? Shortcut { get; }

    public string ShortcutText { get; }

    /// <summary>Whether the action can run now: something is attached to it and the screen says it applies.</summary>
    public bool IsAvailable => _handler is not null && (_availability?.Invoke() ?? Availability.Available).IsAvailable;

    /// <summary>Why the action is disabled, when the screen gave a reason. Null when it is available or has none.</summary>
    public string? UnavailableReason => _handler is null ? null : (_availability?.Invoke() ?? Availability.Available).Reason;

    /// <summary>The tooltip: the action and its shortcut, or the reason when it is disabled.</summary>
    public string ToolTipText =>
        !IsAvailable && UnavailableReason is { Length: > 0 } reason ? reason
        : ShortcutText.Length > 0 ? $"{Label} ({ShortcutText})"
        : Label;

    /// <summary>
    /// Makes the action do something on the current screen. Dispose the result when the screen goes away, and the action
    /// stops applying.
    /// </summary>
    public IDisposable Attach(Action handler, Func<Availability>? availability = null)
    {
        _handler = handler;
        _availability = availability;
        Refresh();
        return new Detach(this, handler);
    }

    /// <summary>Tells the views that the availability may have changed, after the state of the screen changed.</summary>
    public void Refresh()
    {
        Raise(nameof(IsAvailable));
        Raise(nameof(UnavailableReason));
        Raise(nameof(ToolTipText));
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanExecute(object? parameter) => IsAvailable;

    public void Execute(object? parameter)
    {
        if (IsAvailable)
        {
            _handler!();
        }
    }

    sealed class Detach(AppAction owner, Action handler) : IDisposable
    {
        public void Dispose()
        {
            if (owner._handler == handler)
            {
                owner._handler = null;
                owner._availability = null;
                owner.Refresh();
            }
        }
    }
}
