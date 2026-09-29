// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Actions;

namespace Arca.UI.Screens;

/// <summary>
/// The actions of a screen or of its detail, as a list that keeps its order (pantalles-de-domini, D2). Each one is an
/// <see cref="AppAction"/>, so the button, the context menu entry and the shortcut are the same object. Whether an action applies
/// is never worked out here: it comes from Application, which answers with the reason (an error with a stable code) when the
/// operation would be refused, and this turns that reason into the words the disabled action explains itself with.
/// </summary>
public sealed class ActionSet(ILocalizer localizer)
{
    readonly List<AppAction> _actions = [];

    public IReadOnlyList<AppAction> Actions => _actions;

    /// <summary>Adds an action, or attaches the handler to a shared one, such as New.</summary>
    /// <param name="check">Application's answer to "can this be done now?": null when it can, the reason when not.</param>
    public AppAction Add(string id, string labelKey, Action run, Func<Error?>? check = null) =>
        Add(new AppAction(id, localizer.Get(labelKey)), run, check);

    public AppAction Add(AppAction action, Action run, Func<Error?>? check = null)
    {
        action.Attach(run, check is null ? null : () => AvailabilityOf(check()));
        _actions.Add(action);
        return action;
    }

    /// <summary>Tells every action to read its availability again, after what it depends on changed.</summary>
    public void Refresh()
    {
        foreach (var action in _actions)
        {
            action.Refresh();
        }
    }

    /// <summary>What the disabled action says: the message of the error, or nothing when it applies.</summary>
    public Availability AvailabilityOf(Error? reason) =>
        reason is null ? Availability.Available : Availability.Unavailable(localizer.Message(reason));
}
