// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text;
using Arca.Application.Security;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Xunit;

namespace Arca.Infrastructure.Tests.Security;

public sealed class RecoveryAccessTests
{
    const string Spec = "acces-i-xifrat/clau-de-recuperacio";
    const string Password = "riu cadira blau gos";
    const string Other = "gat ratllat sota pluja";

    static readonly Argon2Parameters _cost = new(1024, 1, 1);

    static AccessService Service() => new(new NSecKeyCrypto(), new FileKeyFileStore(), _cost);

    static (byte[] Key, string Recovery) Setup(string database)
    {
        using var access = Service().CreateAccess(Password, Password).Value!;
        KeyFileStore.Write(database, access.KeyFile);
        return (access.DataKey.ToArray(), access.RecoveryKey);
    }

    /// <summary>The groups a person would type for a challenge, read from the key they were shown.</summary>
    static string[] Answers(RecoveryKeyChallenge challenge, string key) =>
        [.. challenge.Indices.Select(i => RecoveryKey.Groups(key)[i])];

    [Fact]
    [Trait("spec", Spec + ": Mostrarla una sola vez y confirmar que se ha guardado (Confirmación correcta)")]
    public void Typing_the_two_groups_asked_confirms_the_key_at_creation()
    {
        using var access = Service().CreateAccess(Password, Password).Value!;

        Assert.Equal(2, access.Challenge.Indices.Count);
        Assert.Equal(2, access.Challenge.Indices.Distinct().Count());
        Assert.All(access.Challenge.Indices, i => Assert.InRange(i, 0, 4));
        Assert.Null(access.Challenge.Verify(Answers(access.Challenge, access.RecoveryKey)));
    }

    [Fact]
    [Trait("spec", Spec + ": Mostrarla una sola vez y confirmar que se ha guardado (Confirmación correcta)")]
    public void Groups_are_read_with_the_same_tolerance_as_the_whole_key()
    {
        var key = "K7F2P9XQ4MABCDEFGHJKMNPQRS";
        var challenge = new RecoveryKeyChallenge(key, [0, 4]);

        Assert.Null(challenge.Verify(["k7f2p", " mnpq-rs "]));
        Assert.Null(new RecoveryKeyChallenge("00111ABCDEFGHJKMNPQRSTVWXY", [0, 1]).Verify(["OOI1L", "abcde"]));
    }

    [Fact]
    [Trait("spec", Spec + ": Mostrarla una sola vez y confirmar que se ha guardado (Confirmación incorrecta)")]
    public void Wrong_groups_do_not_confirm()
    {
        var challenge = new RecoveryKeyChallenge("K7F2P9XQ4MABCDEFGHJKMNPQRS", [1, 3]);

        Assert.Equal("Keys.ConfirmationIncorrect", challenge.Verify(["9XQ4M", "WRONG"])!.Code);
        Assert.Equal("Keys.ConfirmationIncorrect", challenge.Verify(["ABCDE", "FGHJK"])!.Code); // right groups, wrong places
    }

    [Theory]
    [Trait("spec", Spec + ": Mostrarla una sola vez y confirmar que se ha guardado (Sin confirmar)")]
    [InlineData(null)]
    [InlineData("")]
    public void Not_confirming_is_not_allowed(string? blank)
    {
        var challenge = new RecoveryKeyChallenge("K7F2P9XQ4MABCDEFGHJKMNPQRS", [1, 3]);

        Assert.Equal("Keys.ConfirmationRequired", challenge.Verify(null)!.Code);
        Assert.Equal("Keys.ConfirmationRequired", challenge.Verify([blank, blank])!.Code);
        Assert.Equal("Keys.ConfirmationRequired", challenge.Verify(["9XQ4M"])!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Entrar y restablecer con la clave de recuperación (Clave correcta)")]
    public void The_recovery_key_opens_the_data_and_a_new_password_and_key_replace_the_old_ones_once_confirmed()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, oldRecovery) = Setup(database);

        var prepared = Service().PrepareReset(database, oldRecovery.ToLowerInvariant(), Other, Other);

        Assert.True(prepared.IsSuccess);
        using var pending = prepared.Value!;
        Assert.Equal(key, pending.DataKey.ToArray());
        Assert.NotEqual(oldRecovery, pending.RecoveryKey);
        Assert.True(Service().Commit(database, pending, Answers(pending.Challenge, pending.RecoveryKey)).IsSuccess);
        Assert.Equal(key, Service().Unlock(database, Other).Value!.ToArray());
        Assert.Equal("Keys.WrongCredentials", Service().Unlock(database, Password).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Entrar y restablecer con la clave de recuperación (Nueva clave tras el restablecimiento)")]
    public void After_a_reset_the_new_recovery_key_works_and_the_old_one_does_not()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, oldRecovery) = Setup(database);
        using var pending = Service().PrepareReset(database, oldRecovery, Other, Other).Value!;
        Service().Commit(database, pending, Answers(pending.Challenge, pending.RecoveryKey));

        var withNew = Service().PrepareReset(database, pending.RecoveryKey, Other, Other);
        var withOld = Service().PrepareReset(database, oldRecovery, Other, Other);

        Assert.Equal(key, withNew.Value!.DataKey.ToArray());
        Assert.Equal("Keys.WrongCredentials", withOld.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Entrar y restablecer con la clave de recuperación (Nueva clave tras el restablecimiento)")]
    public void A_reset_that_is_not_confirmed_changes_nothing()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, oldRecovery) = Setup(database);
        var before = File.ReadAllBytes(KeyFileStore.PathFor(database));
        using var pending = Service().PrepareReset(database, oldRecovery, Other, Other).Value!;

        var wrong = Service().Commit(database, pending, ["XXXXX", "YYYYY"]);
        var none = Service().Commit(database, pending, null);

        Assert.Equal("Keys.ConfirmationIncorrect", wrong.Error!.Code);
        Assert.Equal("Keys.ConfirmationRequired", none.Error!.Code);
        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(database)));
        Assert.Equal(key, Service().Unlock(database, Password).Value!.ToArray());
    }

    [Theory]
    [Trait("spec", Spec + ": Entrar y restablecer con la clave de recuperación (Clave incorrecta)")]
    [InlineData("no es una clau", "Keys.RecoveryKeyInvalid")]
    [InlineData("", "Keys.RecoveryKeyInvalid")]
    public void A_key_that_cannot_be_a_recovery_key_is_refused(string typed, string code)
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(database);

        Assert.Equal(code, Service().PrepareReset(database, typed, Other, Other).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Entrar y restablecer con la clave de recuperación (Clave incorrecta)")]
    public void A_well_formed_but_wrong_key_says_nothing_more_and_allows_trying_again()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, recovery) = Setup(database);

        var wrong = Service().PrepareReset(database, RecoveryKey.Generate(), Other, Other);
        var again = Service().PrepareReset(database, recovery, Other, Other);

        Assert.Equal("Keys.WrongCredentials", wrong.Error!.Code);
        Assert.Empty(wrong.Error.Args);
        Assert.Equal(key, again.Value!.DataKey.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Entrar y restablecer con la clave de recuperación (Clave correcta)")]
    public void The_new_password_of_a_reset_must_meet_the_rules()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (_, recovery) = Setup(database);

        Assert.Equal("Keys.PasswordTooShort", Service().PrepareReset(database, recovery, "curta", "curta").Error!.Code);
        Assert.Equal("Keys.PasswordMismatch", Service().PrepareReset(database, recovery, Other, Password).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Regenerar la clave (Regenerar)")]
    public void Regenerating_gives_a_new_key_that_opens_the_data_and_invalidates_the_old_one()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, oldRecovery) = Setup(database);

        using var pending = Service().PrepareRegeneration(database, Password).Value!;
        Assert.True(Service().Commit(database, pending, Answers(pending.Challenge, pending.RecoveryKey)).IsSuccess);

        Assert.Equal(key, Service().PrepareReset(database, pending.RecoveryKey, Other, Other).Value!.DataKey.ToArray());
        Assert.Equal("Keys.WrongCredentials", Service().PrepareReset(database, oldRecovery, Other, Other).Error!.Code);
        Assert.Equal(key, Service().Unlock(database, Password).Value!.ToArray()); // the password is untouched
    }

    [Fact]
    [Trait("spec", Spec + ": Regenerar la clave (Cancelar antes de confirmar)")]
    public void Cancelling_a_regeneration_keeps_the_previous_key_valid()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, oldRecovery) = Setup(database);
        var before = File.ReadAllBytes(KeyFileStore.PathFor(database));

        using (Service().PrepareRegeneration(database, Password).Value!)
        {
            // cancelled: the pending change is dropped without committing
        }

        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(database)));
        Assert.Equal(key, Service().PrepareReset(database, oldRecovery, Other, Other).Value!.DataKey.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Regenerar la clave (Regenerar)")]
    public void Regenerating_asks_for_the_current_password()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(database);

        Assert.Equal("Keys.WrongCredentials", Service().PrepareRegeneration(database, Other).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Nunca se guarda en claro (Consulta posterior)")]
    public void The_recovery_key_is_never_written_in_the_clear()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (_, oldRecovery) = Setup(database);
        using var pending = Service().PrepareRegeneration(database, Password).Value!;
        Service().Commit(database, pending, Answers(pending.Challenge, pending.RecoveryKey));

        foreach (var file in Directory.EnumerateFiles(dir.Path))
        {
            var text = Encoding.UTF8.GetString(File.ReadAllBytes(file));
            Assert.DoesNotContain(oldRecovery, text, StringComparison.Ordinal);
            Assert.DoesNotContain(pending.RecoveryKey, text, StringComparison.Ordinal);
            Assert.DoesNotContain(RecoveryKey.Format(pending.RecoveryKey), text, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback (Registro técnico)")]
    public void A_failed_use_of_the_recovery_key_leaves_no_trace_of_it()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(database);
        var wrong = RecoveryKey.Generate();

        var result = Service().PrepareReset(database, wrong, Other, Other);

        Assert.Empty(result.Error!.Args);
        Assert.DoesNotContain(wrong, result.Error.Code, StringComparison.Ordinal);
        Assert.All(Directory.EnumerateFiles(dir.Path), f =>
            Assert.DoesNotContain(wrong, Encoding.UTF8.GetString(File.ReadAllBytes(f)), StringComparison.Ordinal));
    }
}
