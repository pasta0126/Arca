// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Feedback;

public interface IConfirmationService
{
    /// <summary>Asks the user and returns true only if they explicitly confirm.</summary>
    Task<bool> ConfirmAsync(ConfirmationRequest request, CancellationToken ct = default);
}
