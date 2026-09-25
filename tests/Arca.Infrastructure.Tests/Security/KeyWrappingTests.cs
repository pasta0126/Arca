// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text;
using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Infrastructure.Security;
using Xunit;

namespace Arca.Infrastructure.Tests.Security;

public sealed class KeyWrappingTests
{
    const string Spec = "acces-i-xifrat/xifrat-de-la-base";

    // Small cost so the tests are quick; the real cost is checked separately.
    static readonly Argon2Parameters _cost = new(MemoryKiB: 1024, Passes: 1, Parallelism: 1);

    static readonly NSecKeyCrypto _crypto = new();
    static readonly byte[] _password = Encoding.UTF8.GetBytes("riu cadira blau gos");
    const string Recovery = "K7F2P9XQ4MABCDEFGHJKMNPQRS";

    static (DatabaseKey Key, KeyFile File) Create()
    {
        var key = _crypto.GenerateDataKey();
        return (key, KeyWrapping.Create(_crypto, key, _password, Recovery, _cost));
    }

    [Fact]
    [Trait("spec", Spec + ": Cifrado con llave aleatoria (llave única por base)")]
    public void Each_generated_key_is_32_random_bytes_and_different()
    {
        var a = _crypto.GenerateDataKey().ToArray();
        var b = _crypto.GenerateDataKey().ToArray();

        Assert.Equal(32, a.Length);
        Assert.NotEqual(a, b);
        Assert.Contains(a, x => x != 0);
    }

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (con la contraseña)")]
    public void The_password_unwraps_the_same_key()
    {
        var (key, file) = Create();

        var opened = KeyWrapping.UnwrapWithPassword(_crypto, file, _password);

        Assert.True(opened.IsSuccess);
        Assert.Equal(key.ToArray(), opened.Value!.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (con la clave de recuperación)")]
    public void The_recovery_key_unwraps_the_same_key()
    {
        var (key, file) = Create();

        var opened = KeyWrapping.UnwrapWithRecoveryKey(_crypto, file, Recovery);

        Assert.True(opened.IsSuccess);
        Assert.Equal(key.ToArray(), opened.Value!.ToArray());
    }

    [Fact]
    public void A_wrong_password_or_wrong_recovery_key_fails_without_detail()
    {
        var (_, file) = Create();

        var badPassword = KeyWrapping.UnwrapWithPassword(_crypto, file, Encoding.UTF8.GetBytes("riu cadira blau gat"));
        var badRecovery = KeyWrapping.UnwrapWithRecoveryKey(_crypto, file, "K7F2P9XQ4MABCDEFGHJKMNPQRT");

        Assert.Equal("Keys.WrongCredentials", badPassword.Error!.Code);
        Assert.Equal("Keys.WrongCredentials", badRecovery.Error!.Code);
    }

    [Fact]
    public void The_password_does_not_open_the_recovery_wrapper_nor_the_other_way_round()
    {
        var (_, file) = Create();
        var swapped = file with { Password = file.Password with { Wrapped = file.Recovery.Wrapped } };

        Assert.False(KeyWrapping.UnwrapWithPassword(_crypto, swapped, _password).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero de claves protegido (manipulación)")]
    public void Altering_the_wrapped_key_makes_the_authentication_fail()
    {
        var (_, file) = Create();
        var altered = (byte[])file.Password.Wrapped.Clone();
        altered[^1] ^= 1;

        var result = KeyWrapping.UnwrapWithPassword(_crypto, file with { Password = file.Password with { Wrapped = altered } }, _password);

        Assert.Equal("Keys.WrongCredentials", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero de claves protegido (manipulación)")]
    public void Altering_the_header_parameters_makes_the_authentication_fail()
    {
        var (_, file) = Create();

        // Lowering the cost or changing the salt would help an attacker; the header is authenticated with the key.
        var weaker = file with { Password = file.Password with { Parameters = new Argon2Parameters(1024, 2, 1) } };
        var otherSalt = file with { Password = file.Password with { Salt = _crypto.RandomBytes(16) } };
        var otherVersion = file with { FormatVersion = 2 };
        var recoverySalt = file with { Recovery = file.Recovery with { Salt = _crypto.RandomBytes(16) } };

        Assert.False(KeyWrapping.UnwrapWithPassword(_crypto, weaker, _password).IsSuccess);
        Assert.False(KeyWrapping.UnwrapWithPassword(_crypto, otherSalt, _password).IsSuccess);
        Assert.False(KeyWrapping.UnwrapWithPassword(_crypto, otherVersion, _password).IsSuccess);
        Assert.False(KeyWrapping.UnwrapWithRecoveryKey(_crypto, recoverySalt, Recovery).IsSuccess);
    }

    [Fact]
    public void Truncated_or_empty_wrapped_data_is_rejected_not_thrown()
    {
        var (_, file) = Create();

        Assert.False(KeyWrapping.UnwrapWithPassword(_crypto, file with { Password = file.Password with { Wrapped = [] } }, _password).IsSuccess);
        Assert.False(KeyWrapping.UnwrapWithPassword(_crypto, file with { Password = file.Password with { Wrapped = new byte[10] } }, _password).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Derivación resistente a fuerza bruta (parámetros guardados)")]
    public void The_parameters_and_fresh_salts_are_kept_in_the_file_model()
    {
        var (_, one) = Create();
        var (_, two) = Create();

        Assert.Equal(_cost, one.Password.Parameters);
        Assert.Equal("argon2id", one.Password.Kdf);
        Assert.Equal("hkdf-sha256", one.Recovery.Kdf);
        Assert.Equal(KeyFile.CurrentVersion, one.FormatVersion);
        Assert.NotEqual(one.Password.Salt, two.Password.Salt);
        Assert.NotEqual(one.Recovery.Salt, two.Recovery.Salt);
        Assert.NotEqual(one.Password.Wrapped, two.Password.Wrapped); // fresh nonce every time
    }

    [Fact]
    public void Password_and_recovery_wrappers_of_the_same_key_differ()
    {
        var (_, file) = Create();

        Assert.NotEqual(file.Password.Wrapped, file.Recovery.Wrapped);
    }

    [Fact]
    [Trait("spec", "acces-i-xifrat/contrasenya-del-centre: Caracteres libres")]
    public void Passwords_with_accents_c_cedilla_and_l_middle_dot_l_work_and_are_case_sensitive()
    {
        var key = _crypto.GenerateDataKey();
        var text = "Ça fa l·l àèòü — 3r d'ESO";
        var file = KeyWrapping.Create(_crypto, key, Encoding.UTF8.GetBytes(text), Recovery, _cost);

        Assert.True(KeyWrapping.UnwrapWithPassword(_crypto, file, Encoding.UTF8.GetBytes(text)).IsSuccess);
        Assert.False(KeyWrapping.UnwrapWithPassword(_crypto, file, Encoding.UTF8.GetBytes(text.ToUpperInvariant())).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Derivación resistente a fuerza bruta")]
    public void The_password_key_is_deterministic_for_the_same_salt_and_cost_and_changes_with_either()
    {
        var salt = _crypto.RandomBytes(16);

        var a = _crypto.DerivePasswordKey(_password, salt, _cost);
        var b = _crypto.DerivePasswordKey(_password, salt, _cost);
        var otherSalt = _crypto.DerivePasswordKey(_password, _crypto.RandomBytes(16), _cost);
        var otherCost = _crypto.DerivePasswordKey(_password, salt, new Argon2Parameters(1024, 2, 1));

        Assert.Equal(32, a.Length);
        Assert.Equal(a, b);
        Assert.NotEqual(a, otherSalt);
        Assert.NotEqual(a, otherCost);
    }

    [Fact]
    public void The_recovery_key_is_deterministic_for_the_same_salt_and_changes_with_either()
    {
        var salt = _crypto.RandomBytes(16);
        var secret = RecoveryKey.SecretBytes(Recovery);

        Assert.Equal(_crypto.DeriveRecoveryKey(secret, salt), _crypto.DeriveRecoveryKey(secret, salt));
        Assert.NotEqual(_crypto.DeriveRecoveryKey(secret, salt), _crypto.DeriveRecoveryKey(secret, _crypto.RandomBytes(16)));
        Assert.NotEqual(_crypto.DeriveRecoveryKey(secret, salt), _crypto.DeriveRecoveryKey(RecoveryKey.SecretBytes(RecoveryKey.Generate()), salt));
    }
}
