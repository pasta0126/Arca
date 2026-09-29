// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Commands;
using Arca.UI.Common;

namespace Arca.UI.Screens;

/// <summary>One field of a form: what it is called, what was written and the error Application gave for it, if any.</summary>
public sealed class FormFieldModel(string id, string label) : ObservableObject
{
    string _text = string.Empty;
    string? _error;

    /// <summary>A stable name, the one the error codes are matched to.</summary>
    public string Id { get; } = id;

    public string Label { get; } = label;

    public string Text
    {
        get => _text;
        set
        {
            if (Set(ref _text, value))
            {
                Error = null; // writing again is answering the error
            }
        }
    }

    /// <summary>The explained error of this field, next to it. Null when it has none.</summary>
    public string? Error
    {
        get => _error;
        set
        {
            if (Set(ref _error, value))
            {
                Raise(nameof(HasError));
            }
        }
    }

    public bool HasError => _error is not null;
}

/// <summary>What a form window needs from the model of a form, whatever it returns on success.</summary>
public interface IFormModel : System.ComponentModel.INotifyPropertyChanged
{
    string Title { get; }

    string? Note { get; }

    string? Summary { get; }

    IReadOnlyList<FormFieldModel> Fields { get; }

    /// <summary>The label of the button that saves.</summary>
    string SaveLabel { get; }

    System.Windows.Input.ICommand SaveCommand { get; }

    IWorkState Work { get; }

    /// <summary>Raised once saving succeeded: the window closes.</summary>
    event EventHandler? Succeeded;
}

/// <summary>
/// The model of a create or edit form (pantalles-de-domini, D3). It validates when saving, not on every key: the rules are those of
/// Application, and the error it returns comes with a stable code that says which field it is about, so the message shows next to
/// that field, in words the resource file gives. An error never closes the form nor clears what was written. Saving goes through
/// the run-once command, so a double click saves once, and what the operation says (or fails with) reaches the person through the
/// common notifications.
/// </summary>
/// <typeparam name="TResult">What saving returns on success.</typeparam>
public sealed class FormViewModel<TResult> : ObservableObject, IFormModel
{
    readonly Dictionary<string, FormFieldModel> _fields;
    readonly Func<Error, string?> _fieldOf;
    readonly ILocalizer _localizer;
    readonly Action<TResult>? _saved;
    string? _summary;
    string? _note;

    /// <param name="fields">The fields, in the order they are shown.</param>
    /// <param name="save">Saves what was written. It reads the fields; it validates nothing itself.</param>
    /// <param name="fieldOf">The field an error is about, or null when it belongs to the form as a whole (that one is shown as a notification).</param>
    /// <param name="saved">Runs once saving succeeded, to close the form or refresh what shows it.</param>
    public FormViewModel(
        IEnumerable<FormFieldModel> fields, Func<CancellationToken, Task<Result<TResult>>> save, Func<Error, string?> fieldOf,
        Func<TResult, string> successText, string context, INotificationService notifications, ILocalizer localizer, IErrorLog log, IDelay delay,
        string title, string saveLabel, Action<TResult>? saved = null, Func<Task>? afterSuccess = null)
    {
        Title = title;
        SaveLabel = saveLabel;
        Fields = [.. fields];
        _fields = Fields.ToDictionary(f => f.Id);
        _fieldOf = fieldOf;
        _localizer = localizer;
        _saved = saved;
        Save = new RunOnceCommand<TResult>(
            async (ct, _) =>
            {
                ClearErrors();
                var result = await save(ct);
                if (result.IsSuccess)
                {
                    _saved?.Invoke(result.Value!);
                    Succeeded?.Invoke(this, EventArgs.Empty);
                }

                return result;
            },
            successText, context, notifications, localizer, log, delay, afterSuccess, ShowError);
    }

    public string Title { get; }

    public string SaveLabel { get; }

    public IReadOnlyList<FormFieldModel> Fields { get; }

    public System.Windows.Input.ICommand SaveCommand => Save;

    public IWorkState Work => Save;

    public event EventHandler? Succeeded;

    /// <summary>A line under the fields that explains something derived from what is written, such as the name of the course.</summary>
    public string? Note
    {
        get => _note;
        set => Set(ref _note, value);
    }

    public FormFieldModel this[string id] => _fields[id];

    /// <summary>Saves the form. Running it again while it runs does nothing.</summary>
    public RunOnceCommand<TResult> Save { get; }

    /// <summary>"Revisa els camps marcats." while some field has an error; null otherwise.</summary>
    public string? Summary
    {
        get => _summary;
        private set => Set(ref _summary, value);
    }

    /// <summary>Puts the fields back to what they were before writing, and removes the errors. For reopening a form.</summary>
    public void Reset()
    {
        foreach (var field in Fields)
        {
            field.Text = string.Empty;
        }

        ClearErrors();
    }

    void ClearErrors()
    {
        foreach (var field in Fields)
        {
            field.Error = null;
        }

        Summary = null;
    }

    /// <summary>What a save answers when the person declined a confirmation on the way: nothing is reported, nothing was saved.</summary>
    public static Error Cancelled { get; } = new("Common.Cancelled");

    bool ShowError(Error error)
    {
        if (error == Cancelled)
        {
            return true;
        }

        if (_fieldOf(error) is not { } id || !_fields.TryGetValue(id, out var field))
        {
            return false;
        }

        field.Error = _localizer.Message(error);
        Summary = _localizer.Get("Common.Form.FixErrors");
        return true;
    }
}
