// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.GlobalState;
using Arca.Domain.Common;
using Arca.UI.Common;
using Arca.UI.Notifications;

namespace Arca.UI.Shell;

/// <summary>
/// The global state of the application as the frame sees it (ui-shell, D3): the header, the notices and the indicators of the
/// sidebar all read it from here. It is loaded when the application starts and again after every write, never by polling, and
/// it announces a change only when something is different. If loading fails it keeps what it had and tells the person once.
/// </summary>
public sealed class GlobalStateService(Func<CancellationToken, Task<Result<GlobalState>>> load, ResultNotifier notifier) : ObservableObject
{
    GlobalState? _current;

    /// <summary>The last state loaded, or null while the first one has not arrived.</summary>
    public GlobalState? Current => _current;

    /// <summary>True once a state has been loaded.</summary>
    public bool IsLoaded => _current is not null;

    /// <summary>Raised when the state changed: the header, the notices and the indicators redraw.</summary>
    public event EventHandler? Changed;

    /// <summary>Loads the state. Call it at the start and after each operation that writes.</summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await load(ct);
            if (!result.IsSuccess)
            {
                notifier.Error(result.Error!);
                return;
            }

            if (result.Value! != _current)
            {
                _current = result.Value;
                Raise(nameof(Current));
                Raise(nameof(IsLoaded));
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            // A newer refresh replaced this one.
        }
        catch (Exception e)
        {
            notifier.Unexpected(e, "GlobalState");
        }
    }
}
