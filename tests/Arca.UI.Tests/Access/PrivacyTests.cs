// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Security;
using Arca.Domain.Common;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Security;
using Arca.Testing;
using Arca.UI.Access;
using Xunit;

namespace Arca.UI.Tests.Access;

public sealed class PrivacyTests
{
    /// <summary>A presenter that fails the way a bug might, with the secrets in the exception message.</summary>
    sealed class LeakyPresenter(string message) : IFormPresenter
    {
        public Task<FormOutcome> ShowAsync(AccessFormViewModel form, CancellationToken ct) => throw new InvalidOperationException(message);
    }

    [Fact]
    [Trait("spec", "acces-i-xifrat/clau-de-recuperacio: Feedback (Registro técnico)")]
    public async Task An_unexpected_failure_leaves_no_password_or_recovery_key_in_the_log_or_in_what_the_user_sees()
    {
        var folder = Path.Combine(Path.GetTempPath(), "arca-privacy-" + Guid.NewGuid().ToString("N"));
        const string Password = "una contrasenya única i secreta";
        var recovery = RecoveryKey.Generate();
        try
        {
            var access = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore(), new Argon2Parameters(1024, 1, 1));
            var flows = new AccessFlows(access, new LeakyPresenter($"{Password} {recovery} {RecoveryKey.Format(recovery)}"), new ResxLocalizer(),
                (_, _, _, _) => Task.FromResult(Result<bool>.Success(true)));
            var notifications = new RecordingNotifications();
            string reference;
            using (var log = new FileErrorLog(folder))
            {
                var model = new SecurityViewModel(flows, "nowhere.db", notifications, new ResxLocalizer(), log);
                await model.ChangePasswordAsync();
                await model.RegenerateKeyAsync();
                reference = "logged";
            }

            var written = string.Join("\n", Directory.EnumerateFiles(folder).Select(File.ReadAllText));
            Assert.NotEmpty(reference);
            Assert.NotEmpty(written);
            Assert.DoesNotContain(Password, written, StringComparison.Ordinal);
            Assert.DoesNotContain(recovery, written, StringComparison.Ordinal);
            Assert.DoesNotContain(RecoveryKey.Format(recovery), written, StringComparison.Ordinal);
            Assert.All(notifications.Published, n =>
            {
                Assert.DoesNotContain(Password, n.Text, StringComparison.Ordinal);
                Assert.DoesNotContain(recovery, n.Text, StringComparison.Ordinal);
            });
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
