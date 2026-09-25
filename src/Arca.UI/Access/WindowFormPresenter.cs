// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;
using Avalonia.Threading;

namespace Arca.UI.Access;

/// <summary>Shows each form in an <see cref="AccessWindow"/> on the screen thread, over the window that is open (the start-up screen).</summary>
/// <param name="owner">The window to open over, if there is one.</param>
public sealed class WindowFormPresenter(Func<Window?> owner) : IFormPresenter
{
    public async Task<FormOutcome> ShowAsync(AccessFormViewModel form, CancellationToken ct)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var window = new AccessWindow(form);
            form.Sink = new WindowKeySink(window);
            ct.Register(() => form.Cancel());
            if (owner() is { } parent && parent.IsVisible)
            {
                _ = window.ShowDialog(parent);
            }
            else
            {
                window.Show();
            }
        });
        return await form.Completion;
    }
}
