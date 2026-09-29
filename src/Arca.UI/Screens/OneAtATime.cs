// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Screens;

/// <summary>
/// Refuses to start an operation that is already running (pantalles-de-domini, protección contra doble ejecución): a second click on
/// the same action while the first one is still going does nothing, so a double click on a button of a detail acts once. Operations
/// that go through a form are protected by the form's own command; this is for the rest.
/// </summary>
public sealed class OneAtATime
{
    readonly HashSet<string> _running = [];

    /// <summary>Runs the body unless one with the same name is running. Returns without waiting in that case.</summary>
    public async Task RunAsync(string name, Func<Task> body)
    {
        if (!_running.Add(name))
        {
            return;
        }

        try
        {
            await body();
        }
        finally
        {
            _running.Remove(name);
        }
    }
}
