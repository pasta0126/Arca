// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Access;

/// <summary>Puts a form in front of the person and completes when it ends. The real one opens a window; tests script it.</summary>
public interface IFormPresenter
{
    Task<FormOutcome> ShowAsync(AccessFormViewModel form, CancellationToken ct);
}
