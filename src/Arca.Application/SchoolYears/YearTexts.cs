// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;

namespace Arca.Application.SchoolYears;

/// <summary>
/// The sentences and the confirmations of the school year screens, prepared as localized data (docs/convenciones.md, section 5):
/// the screen shows them and only calls the use case if the person explicitly confirms.
/// </summary>
public sealed class YearTexts(ILocalizer localizer)
{
    public string Created(AcademicYearSummary year) => localizer.Get("SchoolYears.Result.Created", year.Name);

    public string Activated(AcademicYearSummary year) => localizer.Get("SchoolYears.Result.Activated", year.Name);

    public string Deleted(string name) => localizer.Get("SchoolYears.Result.Deleted", name);

    public string AmountsSaved(string yearName) => localizer.Get("ConceptAmounts.Result.Saved", yearName);

    /// <summary>Activating says what changes: where the work goes from then on.</summary>
    public ConfirmationRequest ForActivate(AcademicYearSummary year) => new(
        localizer.Get("SchoolYears.Label.ActivateTitle", year.Name),
        localizer.Get("SchoolYears.Label.ActivateConsequence", year.Name),
        localizer.Get("SchoolYears.Label.ActivateConfirm"));

    /// <summary>Deleting a year cannot be undone.</summary>
    public ConfirmationRequest ForDelete(AcademicYearSummary year) => new(
        localizer.Get("SchoolYears.Label.DeleteTitle", year.Name),
        localizer.Get("SchoolYears.Label.DeleteConsequence", year.Name),
        localizer.Get("SchoolYears.Label.DeleteConfirm"), Destructive: true);

    /// <summary>Changing an amount when charges exist only affects the charges generated later.</summary>
    public ConfirmationRequest ForAmountChange(AcademicYearSummary year) => new(
        localizer.Get("ConceptAmounts.Label.ChangeTitle", year.Name),
        localizer.Get("ConceptAmounts.Label.ChangeConsequence"),
        localizer.Get("ConceptAmounts.Label.ChangeConfirm"));
}
