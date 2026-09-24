// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Avalonia.Controls;

namespace Arca.UI.Confirmation;

/// <summary>Shows the confirmation as a modal dialog over the window that owns it.</summary>
public sealed class DialogConfirmationService(Func<Window?> owner, ILocalizer localizer) : IConfirmationService
{
    public async Task<bool> ConfirmAsync(ConfirmationRequest request, CancellationToken ct = default)
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

        return await window.ShowDialog<bool>(parent);
    }
}
