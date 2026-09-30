// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Lists;

/// <summary>A filter that is on, shown as a label the person can remove on its own (navegacio-i-cerca, Patrón común de pantalla).</summary>
/// <param name="Id">A stable name.</param>
/// <param name="Text">What the label says, already in the user's language.</param>
/// <param name="Remove">Takes this filter off and leaves the others as they are.</param>
public sealed record ListFilterTag(string Id, string Text, Action Remove);
