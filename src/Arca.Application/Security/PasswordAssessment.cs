// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Security;

/// <summary>What the password screen shows about an accepted password.</summary>
/// <param name="Length">Characters written, counted after Unicode normalisation.</param>
/// <param name="Strength">The strength indicator.</param>
public sealed record PasswordAssessment(int Length, PasswordStrength Strength);
