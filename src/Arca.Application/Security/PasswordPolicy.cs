// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// The rules for the centre password (acces-i-xifrat, D5): at least 12 characters of any kind and no composition
/// rules, which push people to patterns like "Taquilla#1". Two simple barriers are added: a list of common passwords
/// plus obvious repetitions and sequences are refused, and an orientative strength indicator warns without blocking.
/// It needs no dependencies and reads nothing but the text it is given.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 12;

    const int MinimumSequenceLength = 6;
    const int MaximumRepeatPeriod = 4;
    const int WordsForGood = 3;
    const int LengthForGood = 20;

    /// <summary>Checks a password. Success carries its assessment and, when it is weak, the warning that advises against it.</summary>
    public static Result<PasswordAssessment> Check(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return Result<PasswordAssessment>.Failure(KeyErrors.PasswordRequired);
        }

        var length = PasswordText.Length(password);
        if (length < MinimumLength)
        {
            return Result<PasswordAssessment>.Failure(KeyErrors.PasswordTooShort(MinimumLength));
        }

        var folded = PasswordText.Fold(password);
        if (IsCommon(folded))
        {
            return Result<PasswordAssessment>.Failure(KeyErrors.PasswordTooCommon);
        }

        var strength = Assess(folded);
        var assessment = new PasswordAssessment(length, strength);
        return strength == PasswordStrength.Weak
            ? Result<PasswordAssessment>.Success(assessment, new Notice("Keys.PasswordWeak"))
            : Result<PasswordAssessment>.Success(assessment);
    }

    static bool IsCommon(string folded)
    {
        var compact = new string(folded.Where(c => !IsSeparator(c)).ToArray());
        var stripped = StripTail(compact);
        return IsRepetition(compact)
            || IsSequence(compact)
            || (stripped.Length >= MinimumSequenceLength && IsSequence(stripped))
            || CommonPasswords.Contains(compact)
            || (stripped.Length > 0 && CommonPasswords.Contains(stripped));
    }

    static PasswordStrength Assess(string folded)
    {
        var words = folded.Split([' ', '-', '_', '.', ','], StringSplitOptions.RemoveEmptyEntries);
        var compact = new string(folded.Where(c => !IsSeparator(c)).ToArray());
        var stripped = StripTail(compact);

        var singleWord = words.Length <= 1 && stripped.All(char.IsLetter);
        var onlyDigits = compact.All(char.IsDigit);
        var fewSymbols = compact.Distinct().Count() <= 5;
        if (singleWord || onlyDigits || fewSymbols)
        {
            return PasswordStrength.Weak;
        }

        return words.Length >= WordsForGood || compact.Length >= LengthForGood ? PasswordStrength.Good : PasswordStrength.Fair;
    }

    static bool IsSeparator(char c) => char.IsWhiteSpace(c) || c is '-' or '_' or '.' or ',';

    /// <summary>Removes the digits and signs added at the end of a word: "contrasenya1234" gives "contrasenya".</summary>
    static string StripTail(string text)
    {
        var end = text.Length;
        while (end > 0 && !char.IsLetter(text[end - 1]))
        {
            end--;
        }

        return text[..end];
    }

    /// <summary>One character repeated, or a short block repeated to fill the text.</summary>
    static bool IsRepetition(string text)
    {
        for (var period = 1; period <= MaximumRepeatPeriod && period < text.Length; period++)
        {
            var repeats = true;
            for (var i = period; i < text.Length && repeats; i++)
            {
                repeats = text[i] == text[i - period];
            }

            if (repeats)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Consecutive characters going up or down, allowing a single break ("1234567890" wraps from 9 to 0).</summary>
    static bool IsSequence(string text)
    {
        if (text.Length < MinimumSequenceLength)
        {
            return false;
        }

        foreach (var step in new[] { 1, -1 })
        {
            var breaks = 0;
            for (var i = 1; i < text.Length; i++)
            {
                if (text[i] - text[i - 1] != step)
                {
                    breaks++;
                }
            }

            if (breaks <= 1)
            {
                return true;
            }
        }

        return false;
    }
}
