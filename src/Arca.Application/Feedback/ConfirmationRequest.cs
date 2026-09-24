// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Feedback;

/// <summary>What the confirmation dialog for an irreversible or bulk action shows.</summary>
/// <param name="Title">Short title of the action.</param>
/// <param name="Consequence">What will happen, including that it cannot be undone when that is the case.</param>
/// <param name="ConfirmLabel">Text of the confirm button, naming the action.</param>
/// <param name="Destructive">A destructive action starts with the focus on Cancel.</param>
/// <param name="Details">Optional lines, such as counts ("40 taquilles").</param>
public sealed record ConfirmationRequest(
    string Title, string Consequence, string ConfirmLabel, bool Destructive = false, IReadOnlyList<string>? Details = null);
