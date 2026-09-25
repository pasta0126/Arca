// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Security;
using Xunit;

namespace Arca.Application.Tests.Security;

public sealed class PasswordPolicyTests
{
    const string Spec = "acces-i-xifrat/contrasenya-del-centre";

    [Fact]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Contraseña válida)")]
    public void A_phrase_with_spaces_is_accepted_and_strong()
    {
        var result = PasswordPolicy.Check("riu cadira blau gos");

        Assert.True(result.IsSuccess);
        Assert.Equal(PasswordStrength.Good, result.Value!.Strength);
        Assert.Empty(result.Notices);
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Sin reglas de composición)")]
    public void One_lowercase_word_of_12_letters_is_accepted_with_a_warning()
    {
        var result = PasswordPolicy.Check("insmonturiol");

        Assert.True(result.IsSuccess);
        Assert.Equal(PasswordStrength.Weak, result.Value!.Strength);
        Assert.Contains(result.Notices, n => n.Code == "Keys.PasswordWeak");
    }

    [Theory]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Demasiado corta)")]
    [InlineData("")]
    [InlineData("curta")]
    [InlineData("onzecaracte")]
    public void Too_short_or_empty_is_refused(string password)
    {
        var result = PasswordPolicy.Check(password);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Error!.Code, new[] { "Keys.PasswordTooShort", "Keys.PasswordRequired" });
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Demasiado corta)")]
    public void The_refusal_states_the_minimum_length()
    {
        var error = PasswordPolicy.Check("curta").Error!;

        Assert.Equal(12, Assert.Single(error.Args));
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Sin contraseña)")]
    public void No_password_is_refused()
    {
        Assert.Equal("Keys.PasswordRequired", PasswordPolicy.Check(null).Error!.Code);
    }

    [Theory]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Contraseña demasiado habitual)")]
    [InlineData("contrasenya1234")]
    [InlineData("Contrasenya1234")]
    [InlineData("111111111111")]
    [InlineData("qwertyuiop12")]
    [InlineData("abcabcabcabc")]
    [InlineData("abcdefghijkl")]
    [InlineData("123456789012")]
    [InlineData("password123456")]
    [InlineData("Taquilla2026!!")]
    public void Common_repeated_and_sequential_passwords_are_refused(string password)
    {
        var result = PasswordPolicy.Check(password);

        Assert.False(result.IsSuccess);
        Assert.Equal("Keys.PasswordTooCommon", result.Error!.Code);
    }

    [Theory]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Caracteres libres)")]
    [InlineData("la porta és tancada")]
    [InlineData("çaragossa i l·lucia")]
    [InlineData("ÀÉÍÒÚ àéíòú ñ ü")]
    public void Spaces_accents_and_special_letters_are_accepted(string password)
    {
        Assert.True(PasswordPolicy.Check(password).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Contador de longitud)")]
    public void The_length_is_counted_in_characters_after_normalisation()
    {
        Assert.Equal(19, PasswordPolicy.Check("riu cadira blau gos").Value!.Length);
        Assert.Equal(1, PasswordText.Length("é"));      // decomposed accent counts as one character
        Assert.Equal(3, PasswordText.Length("l·l"));
    }

    [Fact]
    [Trait("spec", Spec + ": Indicador de fortaleza y aviso de pérdida (Contraseña débil)")]
    public void Weak_patterns_are_flagged_without_blocking()
    {
        foreach (var password in new[] { "insmonturiol", "molinsderei99" })
        {
            var result = PasswordPolicy.Check(password);

            Assert.True(result.IsSuccess, password);
            Assert.Equal(PasswordStrength.Weak, result.Value!.Strength);
            Assert.NotEmpty(result.Notices);
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Caracteres libres)")]
    public void Passwords_are_normalised_to_nfc_before_use()
    {
        var composed = PasswordText.ToBytes("açent i cadira");
        var decomposed = PasswordText.ToBytes("açent i cadira");

        Assert.Equal(composed, decomposed);
    }
}
