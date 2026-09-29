// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;

namespace Arca.UI.Screens;

/// <summary>Shows a decision as a modal window over the main window. Only one is open at a time: asking again while one is open answers null.</summary>
public sealed class WindowChoiceDialogs(Func<Window?> owner) : IChoiceDialogs
{
    bool _isOpen;

    public async Task<string?> ChooseAsync(ChoiceRequest request)
    {
        if (_isOpen)
        {
            return null;
        }

        _isOpen = true;
        try
        {
            var window = new ChoiceWindow(request);
            if (owner() is not { } parent)
            {
                window.Show();
                var closed = new TaskCompletionSource<string?>();
                window.Closed += (_, _) => closed.TrySetResult(null);
                return await closed.Task;
            }

            return await window.ShowDialog<string?>(parent);
        }
        finally
        {
            _isOpen = false;
        }
    }
}
