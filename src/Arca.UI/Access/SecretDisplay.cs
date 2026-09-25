// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Access;

/// <summary>The recovery key as a form shows it, with the texts of its copy and print actions.</summary>
/// <param name="Formatted">The key in groups separated by hyphens.</param>
/// <param name="Label">Caption above the key.</param>
/// <param name="CopyLabel">Text of the copy button.</param>
/// <param name="PrintLabel">Text of the print button.</param>
/// <param name="CopiedText">Feedback after copying.</param>
/// <param name="PrintedText">Feedback after opening the page to print.</param>
/// <param name="PrintTitle">Title of the printed page.</param>
/// <param name="PrintLines">Lines of the printed page, the key included.</param>
public sealed record SecretDisplay(
    string Formatted, string Label, string CopyLabel, string PrintLabel, string CopiedText, string PrintedText, string PrintTitle,
    IReadOnlyList<string> PrintLines);
