// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Security;

/// <summary>Orientative strength of an accepted password. It advises and never blocks (acces-i-xifrat, D5).</summary>
public enum PasswordStrength
{
    /// <summary>A single word, only digits, or a predictable pattern.</summary>
    Weak,

    /// <summary>Long enough, without an obvious weakness.</summary>
    Fair,

    /// <summary>Several unrelated words or a long phrase.</summary>
    Good,
}
