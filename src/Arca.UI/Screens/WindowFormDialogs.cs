// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Avalonia.Controls;

namespace Arca.UI.Screens;

/// <summary>Shows a form as a modal window over the main window. Only one is open at a time: asking again while one is open answers false.</summary>
public sealed class WindowFormDialogs(Func<Window?> owner, ILocalizer localizer) : IFormDialogs
{
    bool _isOpen;

    public async Task<bool> ShowAsync(IFormModel form)
    {
        if (_isOpen)
        {
            return false;
        }

        _isOpen = true;
        try
        {
            var window = new FormDialogWindow(form, localizer);
            if (owner() is not { } parent)
            {
                window.Show();
                var closed = new TaskCompletionSource<bool>();
                window.Closed += (_, _) => closed.TrySetResult(false);
                return await closed.Task;
            }

            var asker = parent.FocusManager?.GetFocusedElement() as Avalonia.Input.IInputElement;
            try
            {
                return await window.ShowDialog<bool>(parent);
            }
            finally
            {
                asker?.Focus(); // the person carries on from where they were
            }
        }
        finally
        {
            _isOpen = false;
        }
    }
}
