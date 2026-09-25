// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;

namespace Arca.UI.Access;

/// <summary>One text box of a form. Secret fields show dots.</summary>
public sealed class FormField(string label, bool isSecret) : ObservableObject
{
    string _text = string.Empty;

    public string Label { get; } = label;

    public bool IsSecret { get; } = isSecret;

    public string Text
    {
        get => _text;
        set => Set(ref _text, value);
    }
}
