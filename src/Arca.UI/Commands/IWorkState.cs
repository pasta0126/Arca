// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.ComponentModel;

namespace Arca.UI.Commands;

/// <summary>
/// What a view needs to show the work of an action in progress: the busy indicator (only after 300 ms), the progress
/// "120 de 300", and the cancel button with the reason when it is not available. <see cref="RunOnceCommand{T}"/> is one.
/// </summary>
public interface IWorkState : INotifyPropertyChanged
{
    bool IsRunning { get; }

    bool ShowBusyIndicator { get; }

    bool CanCancel { get; }

    string ProgressText { get; }

    string CancelDisabledReason { get; }

    void Cancel();
}
