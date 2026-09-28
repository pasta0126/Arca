// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Windows.Input;

namespace Arca.UI.Tests;

/// <summary>A command that can always run and does nothing, for components that need one to be drawn.</summary>
internal sealed class NoOpCommand : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
    }
}
