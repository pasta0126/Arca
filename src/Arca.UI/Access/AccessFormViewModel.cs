// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;

namespace Arca.UI.Access;

/// <summary>
/// One screen of the password flows (acces-i-xifrat): a title, some text, text boxes, live hints, an error and the
/// buttons. Everything the screen does is here and the window only draws it. Submitting runs the flow's check off the
/// screen thread; while it runs the form is busy, so a second Enter or click does nothing and the attempt happens once.
/// </summary>
public sealed class AccessFormViewModel : ObservableObject
{
    readonly Func<AccessFormViewModel, Task<bool>> _submit;
    readonly Func<IReadOnlyList<string>, IReadOnlyList<string>>? _hints;
    readonly TaskCompletionSource<FormOutcome> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _isBusy;
    string _error = string.Empty;
    string _status = string.Empty;
    IReadOnlyList<string> _hintLines = [];

    /// <param name="submit">Checks the form and returns true when it is done (the form then closes); false keeps it open, usually after setting <see cref="Error"/>.</param>
    /// <param name="hints">Lines shown under the fields, recomputed with every keystroke from the fields' texts.</param>
    public AccessFormViewModel(
        string title, string intro, string primaryLabel, string cancelLabel, string busyText,
        IReadOnlyList<FormField> fields, Func<AccessFormViewModel, Task<bool>> submit,
        Func<IReadOnlyList<string>, IReadOnlyList<string>>? hints = null)
    {
        Title = title;
        Intro = intro;
        PrimaryLabel = primaryLabel;
        CancelLabel = cancelLabel;
        BusyText = busyText;
        Fields = fields;
        _submit = submit;
        _hints = hints;
        foreach (var field in fields)
        {
            field.PropertyChanged += (_, _) => RefreshHints();
        }

        RefreshHints();
    }

    public string Title { get; }

    public string Intro { get; }

    /// <summary>A notice that stays on screen, such as that lost keys mean lost data.</summary>
    public string Warning { get; init; } = string.Empty;

    public string PrimaryLabel { get; }

    public string CancelLabel { get; }

    public string BusyText { get; }

    /// <summary>Text of a second button (for example "I forgot my password"); empty for none.</summary>
    public string SecondaryLabel { get; init; } = string.Empty;

    public SecretDisplay? Secret { get; init; }

    public IKeySink? Sink { get; set; }

    public IReadOnlyList<FormField> Fields { get; }

    public IReadOnlyList<string> Hints
    {
        get => _hintLines;
        private set => Set(ref _hintLines, value);
    }

    public string Error
    {
        get => _error;
        set => Set(ref _error, value);
    }

    /// <summary>Feedback of the copy and print buttons.</summary>
    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
            {
                Raise(nameof(CanSubmit));
            }
        }
    }

    public bool CanSubmit => !IsBusy;

    /// <summary>Completes when the form ends, with how it ended.</summary>
    public Task<FormOutcome> Completion => _completion.Task;

    public async Task SubmitAsync()
    {
        if (IsBusy || _completion.Task.IsCompleted)
        {
            return;
        }

        IsBusy = true;
        Error = string.Empty;
        try
        {
            if (await _submit(this))
            {
                _completion.TrySetResult(FormOutcome.Submitted);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Cancel() => _completion.TrySetResult(FormOutcome.Cancelled);

    public void ChooseSecondary()
    {
        if (!IsBusy)
        {
            _completion.TrySetResult(FormOutcome.Secondary);
        }
    }

    public async Task CopyAsync()
    {
        if (Secret is not null && Sink is not null)
        {
            await Sink.CopyAsync(Secret.Formatted);
            Status = Secret.CopiedText;
        }
    }

    public async Task PrintAsync()
    {
        if (Secret is not null && Sink is not null)
        {
            await Sink.PrintAsync(Secret.PrintTitle, Secret.PrintLines);
            Status = Secret.PrintedText;
        }
    }

    void RefreshHints()
    {
        if (_hints is not null)
        {
            Hints = _hints([.. Fields.Select(f => f.Text)]);
        }
    }
}
