// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Screens;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Arca.UI.Tests;

public sealed class AppIconUiTests
{
    const string Spec = "icona-d-aplicacio/icona-d-aplicacio: Icono visible en la aplicación";

    [AvaloniaFact]
    [Trait("spec", Spec + " (Modo portable)")]
    public void The_icon_is_read_from_inside_the_assembly_and_is_the_same_one_every_time()
    {
        var first = AppIcon.Load();

        Assert.NotNull(first);
        Assert.Same(first, AppIcon.Load());
    }

    [AvaloniaFact]
    [Trait("spec", Spec + " (Diálogos)")]
    public void A_dialog_window_carries_the_icon_of_the_application()
    {
        var window = new ChoiceWindow(new ChoiceRequest("Títol", "Missatge", [new("a", "A")], "Cancel·la"));

        Assert.NotNull(window.Icon);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + " (Diálogos)")]
    public void A_form_window_carries_the_icon_of_the_application()
    {
        var form = new FormViewModel<string>(
            [new FormFieldModel("Name", "Nom")], _ => Task.FromResult(Arca.Domain.Common.Result<string>.Success("fet")), _ => null, s => s, "Icon",
            new Arca.Testing.RecordingNotifications(), new ResxLocalizer(), new Arca.Testing.RecordingErrorLog(), new Arca.Testing.ManualDelay(), "Formulari", "Desa");

        var window = new FormDialogWindow(form, new ResxLocalizer());

        Assert.NotNull(window.Icon);
    }
}
