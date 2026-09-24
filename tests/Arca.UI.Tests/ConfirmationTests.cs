// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.UI.Confirmation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

public sealed class ConfirmationTests
{
    const string Spec = "arquitectura-base/feedback-operacions: Confirmación de acciones irreversibles";

    static readonly ILocalizer _localizer = new ResxLocalizer();

    static ConfirmationRequest Request(bool destructive) => new(
        "Dona de baixa la taquilla",
        "La taquilla 15 es donarà de baixa. Aquesta acció no es pot desfer.",
        "Dona de baixa",
        destructive,
        ["1 taquilla"]);

    [Fact]
    public void Model_exposes_the_consequence_the_counts_and_a_catalan_cancel_label()
    {
        var model = new ConfirmationViewModel(Request(destructive: true), _localizer);

        Assert.Contains("no es pot desfer", model.Consequence, StringComparison.Ordinal);
        Assert.Equal(["1 taquilla"], model.Details);
        Assert.Equal("Cancel·la", model.CancelLabel);
        Assert.True(model.CancelHasInitialFocus);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + " (acción irreversible)")]
    public void A_destructive_action_starts_with_the_focus_on_cancel()
    {
        var window = new ConfirmationWindow(new ConfirmationViewModel(Request(destructive: true), _localizer));
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.CancelButton.IsFocused);
        Assert.False(window.ConfirmButton.IsFocused);
    }

    [AvaloniaFact]
    public void A_harmless_action_starts_with_the_focus_on_confirm()
    {
        var window = new ConfirmationWindow(new ConfirmationViewModel(Request(destructive: false), _localizer));
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.ConfirmButton.IsFocused);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + " (cancelar la confirmación)")]
    public async Task Escape_cancels_and_nothing_is_confirmed()
    {
        var owner = new Window();
        owner.Show();
        var window = new ConfirmationWindow(new ConfirmationViewModel(Request(destructive: true), _localizer));
        var result = window.ShowDialog<bool>(owner);
        Dispatcher.UIThread.RunJobs();

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();

        Assert.False(await result);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + " (cancelar la confirmación)")]
    public async Task The_cancel_button_cancels()
    {
        var owner = new Window();
        owner.Show();
        var window = new ConfirmationWindow(new ConfirmationViewModel(Request(destructive: true), _localizer));
        var result = window.ShowDialog<bool>(owner);
        Dispatcher.UIThread.RunJobs();

        window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.False(await result);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + " (acción irreversible)")]
    public async Task Only_the_confirm_button_confirms()
    {
        var owner = new Window();
        owner.Show();
        var window = new ConfirmationWindow(new ConfirmationViewModel(Request(destructive: true), _localizer));
        var result = window.ShowDialog<bool>(owner);
        Dispatcher.UIThread.RunJobs();

        window.ConfirmButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.True(await result);
    }
}
