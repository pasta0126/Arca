// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Security;
using Arca.Domain.Common;
using Arca.Infrastructure.Security;
using Arca.UI.Access;
using Arca.Testing;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests.Access;

public sealed class SecurityTests
{
    const string Spec = "acces-i-xifrat/clau-de-recuperacio";
    static readonly ResxLocalizer _localizer = new();

    /// <summary>Flows whose presenter never gets to run: the model tests only need the flow to be replaceable.</summary>
    sealed class HoldingPresenter(TaskCompletionSource release) : IFormPresenter
    {
        public int Shown { get; private set; }

        public async Task<FormOutcome> ShowAsync(AccessFormViewModel form, CancellationToken ct)
        {
            Shown++;
            await release.Task;
            form.Cancel();
            return FormOutcome.Cancelled;
        }
    }

    static (SecurityViewModel Model, RecordingNotifications Notifications, RecordingErrorLog Log, HoldingPresenter Presenter, TaskCompletionSource Release) Build()
    {
        var release = new TaskCompletionSource();
        var presenter = new HoldingPresenter(release);
        var access = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore(), new Argon2Parameters(1024, 1, 1));
        var flows = new AccessFlows(access, presenter, _localizer, (_, _, _, _) => Task.FromResult(Result<bool>.Success(true)));
        var notifications = new RecordingNotifications();
        var log = new RecordingErrorLog();
        return (new SecurityViewModel(flows, Path.Combine(Path.GetTempPath(), "arca-nothing.db"), notifications, _localizer, log), notifications, log, presenter, release);
    }

    [Fact]
    [Trait("spec", Spec + ": Aviso permanente de que sin llave no hay recuperación (Ajustes)")]
    public void The_security_section_reminds_that_there_is_no_recovery_without_the_keys()
    {
        var (model, _, _, _, _) = Build();

        Assert.Contains("no es poden recuperar", model.Warning, StringComparison.Ordinal);
        Assert.Contains("no es pot tornar a veure", model.Note, StringComparison.Ordinal);
        Assert.Equal("Canvia la contrasenya…", model.ChangeLabel);
        Assert.Equal("Genera una clau de recuperació nova…", model.RegenerateLabel);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Aviso permanente de que sin llave no hay recuperación (Ajustes)")]
    public void The_view_shows_the_reminder_and_both_actions()
    {
        var (model, _, _, _, _) = Build();
        var view = new SecurityView(model);
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var texts = view.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();

        Assert.Contains(texts, t => t.Contains("no es poden recuperar", StringComparison.Ordinal));
        Assert.Equal("Canvia la contrasenya…", view.ChangeButton.Content);
        Assert.Equal("Genera una clau de recuperació nova…", view.RegenerateButton.Content);
    }

    [Fact]
    [Trait("spec", "acces-i-xifrat/contrasenya-del-centre: Feedback y guía (Doble Intro)")]
    public async Task While_an_action_is_open_neither_can_start_again()
    {
        var (model, _, _, presenter, release) = Build();

        var first = model.ChangePasswordAsync();
        await model.ChangePasswordAsync();
        await model.RegenerateKeyAsync();
        Assert.False(model.CanAct);
        release.SetResult();
        await first;

        Assert.Equal(1, presenter.Shown);
        Assert.True(model.CanAct);
    }

    [Fact]
    [Trait("spec", "acces-i-xifrat/contrasenya-del-centre: Cambiar la contraseña (Aviso sobre las copias)")]
    public async Task Giving_up_publishes_nothing_and_leaves_the_actions_available()
    {
        var (model, notifications, _, _, release) = Build();
        release.SetResult();

        await model.ChangePasswordAsync();

        Assert.Empty(notifications.Published);
        Assert.True(model.CanAct);
    }

    [Fact]
    [Trait("spec", "acces-i-xifrat/xifrat-de-la-base: Feedback y errores (Error inesperado)")]
    public async Task An_unexpected_failure_shows_a_reference_and_no_secret()
    {
        var access = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore(), new Argon2Parameters(1024, 1, 1));
        var flows = new AccessFlows(access, new ThrowingPresenter(), _localizer, (_, _, _, _) => Task.FromResult(Result<bool>.Success(true)));
        var notifications = new RecordingNotifications();
        var log = new RecordingErrorLog();
        var model = new SecurityViewModel(flows, "nowhere.db", notifications, _localizer, log);

        await model.RegenerateKeyAsync();

        var published = Assert.Single(notifications.Published);
        Assert.Equal(NotificationKind.Error, published.Kind);
        Assert.Contains("REF1", published.Text, StringComparison.Ordinal);
        Assert.Equal("RegenerateRecoveryKey", Assert.Single(log.Entries).Context);
        Assert.True(model.CanAct);
    }

    sealed class ThrowingPresenter : IFormPresenter
    {
        public Task<FormOutcome> ShowAsync(AccessFormViewModel form, CancellationToken ct) => throw new InvalidOperationException("boom");
    }
}
