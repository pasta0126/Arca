// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers;

/// <summary>The text of an empty state in the active language and the actions to offer, in order.</summary>
public sealed record EmptyStateGuide(string Message, IReadOnlyList<SuggestedAction> Actions);
