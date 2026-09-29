// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Identity;
using Arca.UI.Common;

namespace Arca.UI.Identity;

/// <summary>
/// The identity of the centre as the interface has it now: read when the application opens and again after every save, and told to
/// whatever shows it (the header, the theme). It holds no rule: it only keeps what Application answered.
/// </summary>
public sealed class CentreIdentityModel : ObservableObject
{
    CentreIdentityView _current = CentreIdentityView.Empty;

    /// <summary>The identity as saved; empty while the centre has not defined any.</summary>
    public CentreIdentityView Current
    {
        get => _current;
        private set => Set(ref _current, value);
    }

    /// <summary>Raised when the identity changes, so the header and the accent follow it at once.</summary>
    public event EventHandler? Changed;

    public void Set(CentreIdentityView identity)
    {
        Current = identity;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
