// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Avalonia.Controls;

namespace Arca.UI.Confirmation;

/// <summary>Shows the confirmation as a modal dialog over the window that owns it.</summary>
public sealed class DialogConfirmationService(Func<Window?> owner, ILocalizer localizer) : IConfirmationService
{
    bool _isOpen;

    /// <summary>
    /// Only one confirmation is open at a time. A second request while one is open (a double click, a repeated shortcut) is
    /// not opened and is not confirmed: it answers false, so the action it belongs to does not run twice.
    /// </summary>
    public async Task<bool> ConfirmAsync(ConfirmationRequest request, CancellationToken ct = default)
    {
        if (_isOpen)
        {
            return false;
        }

        _isOpen = true;
        try
        {
            return await ShowAsync(request);
        }
        finally
        {
            _isOpen = false;
        }
    }

    async Task<bool> ShowAsync(ConfirmationRequest request)
    {
        var window = new ConfirmationWindow(new ConfirmationViewModel(request, localizer));
        var parent = owner();
        if (parent is null)
        {
            window.Show();
            var closed = new TaskCompletionSource<bool>();
            window.Closed += (_, _) => closed.TrySetResult(false);
            return await closed.Task;
        }

        // The focus goes back to the control that asked, so the person carries on from where they were (teclat-i-menus).
        var asker = parent.FocusManager?.GetFocusedElement() as Avalonia.Input.IInputElement;
        try
        {
            return await window.ShowDialog<bool>(parent);
        }
        finally
        {
            asker?.Focus();
        }
    }
}
