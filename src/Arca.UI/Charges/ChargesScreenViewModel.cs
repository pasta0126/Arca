// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Students.ListStudentRows;
using Arca.Domain.Common;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Screens;

namespace Arca.UI.Charges;

/// <summary>
/// The charges view of the Payments section (pantalles-cobraments, Organización de la sección): a search of students that opens the
/// charges of the one chosen, with the same charges model the record of a student uses. A student can also be opened from the
/// pending payments view.
/// </summary>
public sealed class ChargesScreenViewModel : ObservableObject
{
    public ChargesScreenViewModel(ChargeServices services, ScreenContext context, StudentChargesViewModel charges)
    {
        Charges = charges;
        var text = context.Localizer;
        Students = new ScreenListViewModel<StudentListRow, Guid>(
            [
                new ListColumn<StudentListRow>("last", text.Get("Students.Label.LastName"), s => s.LastName, Width: 3),
                new ListColumn<StudentListRow>("first", text.Get("Students.Label.FirstName"), s => s.FirstName, Width: 2),
                new ListColumn<StudentListRow>("level", text.Get("Students.Label.Level"), s => s.LevelName ?? string.Empty, Width: 2),
                new ListColumn<StudentListRow>("state", text.Get("Students.Label.State"), s => s.IsRetired ? text.Get("Students.State.Retired") : string.Empty, Width: 1),
            ],
            s => s.Id, async ct =>
            {
                var listing = await services.ListStudents(ct);
                return listing.IsSuccess ? Result<IReadOnlyList<StudentListRow>>.Success(listing.Value!.Rows) : Result<IReadOnlyList<StudentListRow>>.Failure(listing.Error!);
            },
            text, context.Notifications, context.Log, () => text.Get("Students.Empty.NoStudents"));
        Students.List.SortBy("last");
        Students.CurrentChanged += (_, _) => _ = ShowCurrentAsync();
    }

    public ScreenListViewModel<StudentListRow, Guid> Students { get; }

    public StudentChargesViewModel Charges { get; }

    public async Task LoadAsync(CancellationToken ct = default) => await Students.LoadAsync(ct);

    /// <summary>Opens the charges of a student, looking them up in the list.</summary>
    public async Task OpenAsync(Guid studentId)
    {
        await Students.LoadAsync();
        if (Students.List.Rows.FirstOrDefault(s => s.Id == studentId) is { } row)
        {
            Students.Select(row);
        }
    }

    async Task ShowCurrentAsync()
    {
        var has = Students.TryGetSelectedKey(out var id);
        await Charges.ShowAsync(has ? id : null);
    }
}
