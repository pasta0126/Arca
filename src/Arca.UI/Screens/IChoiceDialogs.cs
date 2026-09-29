// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Screens;

/// <summary>One thing to pick in a decision: what it does, in words, and the name the screen knows it by.</summary>
public sealed record ChoiceOption(string Id, string Label);

/// <summary>What the person has to decide before anything changes: a title, what is at stake, and the ways forward.</summary>
public sealed record ChoiceRequest(string Title, string Message, IReadOnlyList<ChoiceOption> Options, string CancelLabel);

/// <summary>Asks the person to choose one of several ways forward. Cancelling answers null and nothing is decided.</summary>
public interface IChoiceDialogs
{
    Task<string?> ChooseAsync(ChoiceRequest request);
}
