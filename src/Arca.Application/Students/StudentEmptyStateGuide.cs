// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>The text of an empty state of the list of students, in the active language, and the actions to offer, in order.</summary>
public sealed record StudentEmptyStateGuide(string Message, IReadOnlyList<StudentSuggestedAction> Actions);
