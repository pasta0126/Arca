// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Access;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Arca.UI.Tests.Access;

public sealed class AccessWindowTests
{
    const string Spec = "acces-i-xifrat/contrasenya-del-centre";

    static AccessFormViewModel Form(FormField field, Func<AccessFormViewModel, Task<bool>>? submit = null, string secondary = "", SecretDisplay? secret = null) =>
        new("Títol", "Introducció", "Obre", "Cancel·la", "Comprovant…", [field], submit ?? (_ => Task.FromResult(true)))
        {
            SecondaryLabel = secondary,
            Warning = "Sense clau no es poden recuperar.",
            Secret = secret,
        };

    static AccessWindow Show(AccessFormViewModel model)
    {
        var window = new AccessWindow(model);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    static IEnumerable<string> Texts(Control root) =>
        root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty);

    [AvaloniaFact]
    [Trait("spec", Spec + ": Feedback y guía (Solo teclado)")]
    public void Typing_and_pressing_enter_submits_with_the_keyboard_alone()
    {
        var field = new FormField("Contrasenya", isSecret: true);
        var calls = 0;
        var model = Form(field, _ =>
        {
            calls++;
            return Task.FromResult(false); // refused: the form stays open and the focus must come back to the box
        });
        var window = Show(model);

        window.KeyTextInput("riu cadira blau gos");
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("riu cadira blau gos", field.Text);
        Assert.Equal(1, calls);
        Assert.Same(window.Boxes[0], window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Feedback y guía (Doble Intro)")]
    public async Task Pressing_enter_twice_submits_once()
    {
        var release = new TaskCompletionSource();
        var calls = 0;
        var model = Form(new FormField("Contrasenya", true), async _ =>
        {
            calls++;
            await release.Task;
            return true;
        });
        var window = Show(model);

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.PrimaryButton.IsEnabled); // busy: the button and the boxes are off
        release.SetResult();
        await model.Completion;

        Assert.Equal(1, calls);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Contraseña al abrir la aplicación (Cancelar)")]
    public async Task Escape_cancels_and_closing_the_window_cancels_too()
    {
        var model = Form(new FormField("Contrasenya", true));
        var window = Show(model);

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(FormOutcome.Cancelled, await model.Completion);

        var other = Form(new FormField("Contrasenya", true));
        var closing = Show(other);
        closing.Close();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(FormOutcome.Cancelled, await other.Completion);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Contraseña al abrir la aplicación (Ofrecer la recuperación)")]
    public async Task The_secondary_button_ends_the_form_with_that_choice()
    {
        var model = Form(new FormField("Contrasenya", true), secondary: "He oblidat la contrasenya");
        var window = Show(model);

        window.SecondaryButton!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(FormOutcome.Secondary, await model.Completion);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Feedback y guía (Mensajes)")]
    public void The_window_shows_the_texts_the_error_the_hints_and_hides_the_password()
    {
        var field = new FormField("Contrasenya", true);
        var model = new AccessFormViewModel(
            "Títol", "Introducció", "Obre", "Cancel·la", "Comprovant…", [field], _ => Task.FromResult(false),
            texts => [$"{texts[0].Length} de 12 caràcters"])
        {
            Warning = "Sense clau no es poden recuperar.",
        };
        var window = Show(model);

        field.Text = "abcdefghi";
        model.Error = "La contrasenya no és correcta.";
        Dispatcher.UIThread.RunJobs();

        var texts = Texts(window).ToList();
        Assert.Contains("Títol", texts);
        Assert.Contains("Introducció", texts);
        Assert.Contains("Sense clau no es poden recuperar.", texts);
        Assert.Contains("9 de 12 caràcters", texts);
        Assert.Contains("La contrasenya no és correcta.", texts);
        Assert.Equal('●', window.Boxes[0].PasswordChar);
    }

    [AvaloniaFact]
    [Trait("spec", "acces-i-xifrat/clau-de-recuperacio: Mostrarla una sola vez y confirmar que se ha guardado (Mostrar la clave)")]
    public void The_key_screen_shows_the_key_with_copy_and_print_buttons()
    {
        var secret = new SecretDisplay("K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRS", "Clau", "Copia", "Imprimeix", "Copiada", "Impresa", "Títol", []);
        var model = Form(new FormField("Grup 2", false), secret: secret);
        var window = Show(model);

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();
        var buttons = window.GetVisualDescendants().OfType<Button>().Select(b => b.Content?.ToString()).ToList();

        Assert.Contains("K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRS", texts);
        Assert.Contains("Copia", buttons);
        Assert.Contains("Imprimeix", buttons);
    }
}
