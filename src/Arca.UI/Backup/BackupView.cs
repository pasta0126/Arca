// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.ComponentModel;
using Arca.UI.Common;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Backup;

/// <summary>The content of the «Còpia de seguretat» block of Settings: the note, the two actions, and what is happening while one runs.</summary>
public sealed class BackupView : UserControl
{
    public BackupView(BackupViewModel model)
    {
        MakeButton = new Button { Content = model.MakeLabel };
        RestoreButton = new Button { Content = model.RestoreLabel };
        CancelButton = new Button { Content = model.CancelLabel, IsVisible = false };
        StatusText = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        Progress = new ProgressBar { IsIndeterminate = true, IsVisible = false };
        MakeButton.Click += async (_, _) => await model.MakeAsync();
        RestoreButton.Click += async (_, _) => await model.RestoreAsync();
        CancelButton.Click += (_, _) => model.Cancel();
        model.PropertyChanged += (_, e) => Refresh(model, e);
        Content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = model.Note, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { MakeButton, RestoreButton, CancelButton } },
                Progress,
                StatusText,
            },
        };
        Refresh(model, null);
    }

    public Button MakeButton { get; }

    public Button RestoreButton { get; }

    public Button CancelButton { get; }

    public TextBlock StatusText { get; }

    public ProgressBar Progress { get; }

    void Refresh(BackupViewModel model, PropertyChangedEventArgs? _)
    {
        MakeButton.IsEnabled = model.CanAct;
        RestoreButton.IsEnabled = model.CanAct;
        CancelButton.IsVisible = model.IsBusy;
        Progress.IsVisible = model.IsBusy;
        StatusText.Text = model.Status;
        StatusText.IsVisible = model.Status.Length > 0;
    }
}
