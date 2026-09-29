// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Notifications;

namespace Arca.UI.Map;

/// <summary>
/// The panel of active students of the active year who have no locker (ui-shell, Alumnos sin taquilla): a list with a search
/// box and the count, which is where a student is dragged from onto a free locker and where the student for the "assign"
/// action of a locker is chosen. Without an active year it says a year has to be activated and offers nothing to assign.
/// </summary>
public sealed class StudentsWithoutLockerViewModel : ObservableObject
{
    readonly Func<CancellationToken, Task<Result<StudentListing>>> _load;
    readonly ResultNotifier _notifier;
    readonly ILocalizer _localizer;
    IReadOnlyList<StudentRow> _all = [];
    IReadOnlyList<StudentRow> _shown = [];
    string _filterText = string.Empty;
    Guid? _selected;
    bool _noActiveYear;

    public StudentsWithoutLockerViewModel(Func<CancellationToken, Task<Result<StudentListing>>> load, ResultNotifier notifier, ILocalizer localizer)
    {
        _load = load;
        _notifier = notifier;
        _localizer = localizer;
        State = new ListStateViewModel(localizer);
    }

    public ListStateViewModel State { get; }

    /// <summary>The students to show, after the search box, by last name.</summary>
    public IReadOnlyList<StudentRow> Students
    {
        get => _shown;
        private set => Set(ref _shown, value);
    }

    /// <summary>How many students have no locker, whatever the search box hides.</summary>
    public int Count => _all.Count;

    /// <summary>True when there is no active year: nothing can be assigned until one is activated.</summary>
    public bool NoActiveYear
    {
        get => _noActiveYear;
        private set => Set(ref _noActiveYear, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (Set(ref _filterText, value))
            {
                Apply();
            }
        }
    }

    /// <summary>The student chosen in the list, to assign to a locker from its detail.</summary>
    public Guid? SelectedStudentId
    {
        get => _selected;
        private set => Set(ref _selected, value);
    }

    public StudentRow? Selected => _all.FirstOrDefault(s => s.Id == _selected);

    /// <summary>Chooses a student, or clears the choice with null.</summary>
    public void Select(Guid? studentId)
    {
        SelectedStudentId = studentId;
        Raise(nameof(Selected));
    }

    /// <summary>Loads the list again. It is called at the start and after every assignment or release, which change who is without a locker.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _load(ct);
            if (!result.IsSuccess)
            {
                if (result.Error!.Code == "SchoolYears.NoActiveYear")
                {
                    _all = [];
                    NoActiveYear = true;
                    Apply();
                    return;
                }

                _notifier.Error(result.Error);
                return;
            }

            NoActiveYear = false;
            _all = result.Value!.Rows;
            if (_selected is { } id && _all.All(s => s.Id != id))
            {
                Select(null); // assigned meanwhile: the choice is gone
            }

            Raise(nameof(Count));
            Apply();
        }
        catch (OperationCanceledException)
        {
            // Closed meanwhile.
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, "LoadStudentsWithoutLocker");
        }
    }

    void Apply()
    {
        var words = TextComparer.Key(_filterText).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Students =
        [
            .. _all
                .Where(s => words.All(w => TextComparer.Key($"{s.FirstName} {s.LastName} {s.LevelName} {s.GroupName}").Contains(w, StringComparison.Ordinal)))
                .OrderBy(s => s.LastName, TextComparer.Comparer).ThenBy(s => s.FirstName, TextComparer.Comparer),
        ];
        Raise(nameof(Count));
        if (NoActiveYear)
        {
            State.ShowEmpty(_localizer.Get("Shell.Students.NoYear"));
        }
        else if (_all.Count == 0)
        {
            State.ShowEmpty(_localizer.Get("Shell.Students.AllHaveLocker"));
        }
        else if (_shown.Count == 0)
        {
            State.ShowNoResults();
        }
        else
        {
            State.ShowContent();
        }
    }
}
