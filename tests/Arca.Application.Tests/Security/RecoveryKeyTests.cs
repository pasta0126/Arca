// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Security;
using Xunit;

namespace Arca.Application.Tests.Security;

public sealed class RecoveryKeyTests
{
    const string Spec = "acces-i-xifrat/clau-de-recuperacio";

    [Fact]
    [Trait("spec", Spec + ": Generación de la clave de recuperación (formato)")]
    public void A_generated_key_has_26_characters_of_the_unambiguous_alphabet_in_five_groups()
    {
        var key = RecoveryKey.Generate();

        Assert.Equal(26, key.Length);
        Assert.All(key, c => Assert.Contains(c, RecoveryKey.Alphabet));
        Assert.DoesNotContain(key, c => "ILOU".Contains(c, StringComparison.Ordinal));
        Assert.Equal([5, 5, 5, 5, 6], RecoveryKey.Groups(key).Select(g => g.Length));
        Assert.Equal(key, string.Concat(RecoveryKey.Groups(key)));
        Assert.Matches("^[0-9A-Z]{5}(-[0-9A-Z]{5}){3}-[0-9A-Z]{6}$", RecoveryKey.Format(key));
    }

    [Fact]
    [Trait("spec", Spec + ": Generación de la clave de recuperación (aleatoria)")]
    public void Generated_keys_are_different_and_use_the_whole_alphabet()
    {
        var keys = Enumerable.Range(0, 200).Select(_ => RecoveryKey.Generate()).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(RecoveryKey.Alphabet.Length, string.Concat(keys).Distinct().Count()); // 5,200 characters cover all 32 symbols
    }

    [Theory]
    [Trait("spec", Spec + ": Entrar y restablecer con la clave de recuperación (tolerancia al escribirla)")]
    [InlineData("K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRS")]
    [InlineData("k7f2p-9xq4m-abcde-fghjk-mnpqrs")]
    [InlineData("K7F2P 9XQ4M ABCDE FGHJK MNPQRS")]
    [InlineData("k7f2p9xq4mabcdefghjkmnpqrs")]
    [InlineData("  K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRS  ")]
    [InlineData("K7F2P‐9XQ4M–ABCDE FGHJK-MNPQRS")]
    public void Any_way_of_writing_the_key_normalises_to_the_same_canonical_form(string typed)
    {
        var result = RecoveryKey.Normalize(typed);

        Assert.True(result.IsSuccess);
        Assert.Equal("K7F2P9XQ4MABCDEFGHJKMNPQRS", result.Value);
    }

    [Theory]
    [InlineData("O0O0O-11111-ABCDE-FGHJK-MNPQRS", "0000011111ABCDEFGHJKMNPQRS")] // O reads as 0
    [InlineData("0000I-1L111-ABCDE-FGHJK-MNPQRS", "0000111111ABCDEFGHJKMNPQRS")] // I and L read as 1
    public void Look_alike_characters_are_read_as_crockford_does(string typed, string canonical)
    {
        Assert.Equal(canonical, RecoveryKey.Normalize(typed).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("K7F2P-9XQ4M")] // too short
    [InlineData("K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRSTV")] // too long
    [InlineData("K7F2P-9XQ4M-ABCDE-FGHJK-MNPQR!")] // symbol outside the alphabet
    [InlineData("K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRU")] // U is not in the alphabet
    [Trait("spec", Spec + ": Entrar y restablecer con la clave de recuperación (clave incorrecta)")]
    public void Text_that_cannot_be_a_key_is_rejected_without_detail(string? typed)
    {
        var result = RecoveryKey.Normalize(typed);

        Assert.Equal("Keys.RecoveryKeyInvalid", result.Error!.Code);
    }

    [Fact]
    public void A_generated_key_normalises_to_itself_from_its_displayed_form()
    {
        var key = RecoveryKey.Generate();

        Assert.Equal(key, RecoveryKey.Normalize(RecoveryKey.Format(key)).Value);
        Assert.Equal(key, RecoveryKey.Normalize(RecoveryKey.Format(key).ToLowerInvariant()).Value);
    }

    [Fact]
    public void Secret_bytes_do_not_depend_on_how_the_key_was_typed()
    {
        var a = RecoveryKey.SecretBytes(RecoveryKey.Normalize("k7f2p 9xq4m abcde fghjk mnpqrs").Value!);
        var b = RecoveryKey.SecretBytes(RecoveryKey.Normalize("K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRS").Value!);

        Assert.Equal(a, b);
    }
}
