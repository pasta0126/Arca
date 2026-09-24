// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;

namespace Arca.UI.Confirmation;

/// <summary>What the confirmation dialog shows for an irreversible or bulk action.</summary>
public sealed class ConfirmationViewModel(ConfirmationRequest request, ILocalizer localizer)
{
    public string Title => request.Title;

    public string Consequence => request.Consequence;

    public IReadOnlyList<string> Details => request.Details ?? [];

    public string ConfirmLabel => request.ConfirmLabel;

    public string CancelLabel => localizer.Get("Common.Label.Cancel");

    /// <summary>A destructive action starts with the focus on Cancel, so an Enter by reflex does not destroy anything.</summary>
    public bool CancelHasInitialFocus => request.Destructive;
}
