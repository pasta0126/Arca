// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Identity;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Domain.Common;
using Arca.Domain.Identity;
using Arca.Testing;
using Arca.UI.Identity;
using Arca.UI.Preferences;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Xunit;

namespace Arca.UI.Tests;

public sealed class IdentityAndThemeTests
{
    const string Spec = "ui-shell/identitat-i-tema";

    /// <summary>A 1 by 1 PNG that any decoder reads.</summary>
    static readonly byte[] _png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Saved { get; set; } = new();

        public UiPreferences Load() => Saved;

        public void Save(UiPreferences preferences) => Saved = preferences;
    }

    sealed class FakePicker(byte[]? bytes) : ILogoPicker
    {
        public Task<byte[]?> PickAsync() => Task.FromResult(bytes);
    }

    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly ResxLocalizer _localizer = new();
    readonly CentreIdentityModel _model = new();
    readonly List<SaveCentreIdentityRequest> _saved = [];

    IdentityViewModel Model(byte[]? picked = null, Result<CentreIdentityView>? answer = null) => new(
        new IdentityServices(
            _ => Task.FromResult(Result<CentreIdentityView>.Success(_model.Current)),
            (request, _) =>
            {
                _saved.Add(request);
                return Task.FromResult(answer ?? Result<CentreIdentityView>.Success(new CentreIdentityView(request.Name, request.Logo, request.Logo is null ? null : "image/png", request.Accent)));
            },
            bytes => CentreIdentity.CheckLogo(bytes).Error),
        _model, new FakePicker(picked), _localizer, _notifications, _log, _delay);

    // --- The contrast of the accent ---

    [Theory]
    [Trait("spec", Spec + ": Contraste garantizado (Acento demasiado claro, Acento demasiado oscuro en tema oscuro)")]
    [InlineData("#FFFFFF")]
    [InlineData("#FFFF00")]
    [InlineData("#000000")]
    [InlineData("#010101")]
    [InlineData("#808080")]
    [InlineData("#7F7F00")]
    [InlineData("#0000FF")]
    [InlineData("#FF0000")]
    [InlineData("#123456")]
    public void Whatever_the_accent_the_text_on_it_reads_and_the_focus_is_visible_in_both_themes(string accent)
    {
        foreach (var (theme, dark) in new[] { (PaletteColors.Light, false), (PaletteColors.Dark, true) })
        {
            var colours = AccentTheme.For(accent, theme, dark);

            Assert.True(Contrast.Ratio(colours.OnAccent, colours.Accent) >= Contrast.MinimumForText, $"{accent} text on accent, dark={dark}");
            Assert.True(Contrast.Ratio(colours.OnAccent, colours.Hover) >= Contrast.MinimumForText, $"{accent} text on hover, dark={dark}");
            Assert.True(Contrast.Ratio(colours.OnAccent, colours.Pressed) >= Contrast.MinimumForText, $"{accent} text on pressed, dark={dark}");
            Assert.True(Contrast.Ratio(colours.Focus, theme.Background) >= AccentTheme.MinimumForFocus, $"{accent} focus, dark={dark}");
            if (dark)
            {
                Assert.True(Contrast.Ratio(colours.Accent, theme.Background) >= AccentTheme.MinimumForFocus, $"{accent} selection in the dark theme");
            }
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Contraste garantizado (Acento demasiado oscuro en tema oscuro)")]
    public void A_very_dark_accent_is_lightened_in_the_dark_theme_and_left_alone_in_the_light_one()
    {
        var light = AccentTheme.For("#101820", PaletteColors.Light, dark: false);
        var dark = AccentTheme.For("#101820", PaletteColors.Dark, dark: true);

        Assert.Equal("#101820", light.Accent);
        Assert.NotEqual("#101820", dark.Accent);
    }

    [Fact]
    [Trait("spec", Spec + ": Color de acento (Restablecer)")]
    public void Without_an_accent_the_theme_keeps_its_own()
    {
        var colours = AccentTheme.For(null, PaletteColors.Light, dark: false);

        Assert.Equal(ArcaPalette.Accent, colours.Accent);
        Assert.Equal(ArcaPalette.OnAccent, colours.OnAccent);
    }

    // --- The palettes ---

    [Fact]
    [Trait("spec", Spec + ": Paleta neutra y pastel (Contraste del texto); Tema como recursos con nombre (Estados semánticos)")]
    public void Every_pair_of_text_and_background_of_the_dark_palette_reaches_the_contrast()
    {
        var p = PaletteColors.Dark;
        (string Name, string Text, string Background)[] pairs =
        [
            ("text on background", p.Text, p.Background), ("text on surface", p.Text, p.Surface), ("text on raised", p.Text, p.SurfaceRaised),
            ("text on hover", p.Text, p.SurfaceHover), ("text on pressed", p.Text, p.SurfacePressed),
            ("secondary on background", p.TextSecondary, p.Background), ("secondary on surface", p.TextSecondary, p.Surface),
            ("on accent", p.OnAccent, p.Accent), ("on accent hover", p.OnAccent, p.AccentHover), ("on accent pressed", p.OnAccent, p.AccentPressed),
            ("on success", p.OnSemantic, p.Success), ("on warning", p.OnSemantic, p.Warning), ("on error", p.OnSemantic, p.Error),
            ("on free", p.OnSemantic, p.StatusFree), ("on occupied", p.OnSemantic, p.StatusOccupied), ("on reserved", p.OnSemantic, p.StatusReserved),
            ("on broken", p.OnSemantic, p.StatusBroken), ("on maintenance", p.OnSemantic, p.StatusMaintenance),
            ("error text on background", p.ErrorText, p.Background), ("error text on surface", p.ErrorText, p.Surface),
        ];

        foreach (var (name, text, background) in pairs)
        {
            Assert.True(Contrast.Ratio(text, background) >= Contrast.MinimumForText, $"{name}: {Contrast.Ratio(text, background):0.00}");
        }

        Assert.True(Contrast.Ratio(p.Focus, p.Background) >= 3.0);
    }

    [Fact]
    [Trait("spec", "ux-fonaments/components-de-feedback: Componentes sin colores ni textos propios (contrato de recursos)")]
    public void Both_themes_define_every_resource_of_the_contract_with_the_components()
    {
        var resources = ArcaTheme.CreateResources("#88AACC");

        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var dictionary = (ResourceDictionary)resources.ThemeDictionaries[variant];
            Assert.All(ArcaResourceKeys.Brushes, key => Assert.True(dictionary.ContainsKey(key), $"{variant} {key}"));
        }
    }

    // --- The theme applied ---

    [Fact]
    [Trait("spec", Spec + ": Tema claro, oscuro o del sistema (Tema por defecto, Ajuste ilegible)")]
    public void The_default_is_light_and_a_saved_value_that_means_nothing_gives_light_too()
    {
        var store = new MemoryStore();
        Assert.Equal(ThemeChoice.Light, new UiPreferencesSession(store).Theme);

        store.Saved = new UiPreferences(Theme: (ThemeChoice)99);
        Assert.Equal(ThemeChoice.Light, new UiPreferencesSession(store).Theme);
    }

    [Fact]
    [Trait("spec", Spec + ": Tema claro, oscuro o del sistema (Cambio manual)")]
    public void Choosing_a_theme_applies_it_at_once_and_remembers_it()
    {
        var store = new MemoryStore();
        var applied = new List<(ThemeChoice, string?)>();
        var session = new UiPreferencesSession(store);
        _model.Set(new CentreIdentityView("Centre", null, null, "#88AACC"));
        var settings = new ThemeSettingsViewModel(session, _model, (choice, accent) => applied.Add((choice, accent)));

        settings.Choice = ThemeChoice.Dark;

        Assert.Equal((ThemeChoice.Dark, "#88AACC"), applied[^1]);
        Assert.Equal(ThemeChoice.Dark, new UiPreferencesSession(store).Theme); // remembered on reopening
    }

    [Fact]
    [Trait("spec", Spec + ": Color de acento (Elegir un acento)")]
    public void A_new_accent_is_put_on_the_theme_in_use()
    {
        var applied = new List<(ThemeChoice, string?)>();
        var session = new UiPreferencesSession(new MemoryStore());
        session.SetTheme(ThemeChoice.System);
        _ = new ThemeSettingsViewModel(session, _model, (choice, accent) => applied.Add((choice, accent)));

        _model.Set(new CentreIdentityView("Centre", null, null, "#F2C6A0"));

        Assert.Equal((ThemeChoice.System, "#F2C6A0"), applied.Single());
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Tema claro, oscuro o del sistema (Tema del sistema elegido)")]
    public void The_manager_asks_avalonia_for_the_variant_and_the_system_choice_follows_the_operating_system()
    {
        var app = Avalonia.Application.Current!;
        var manager = new ThemeManager(app);

        manager.Apply(ThemeChoice.Dark, "#88AACC");
        Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
        Assert.Equal("#88AACC", manager.Accent);

        manager.Apply(ThemeChoice.System, "#88AACC");
        Assert.Equal(ThemeVariant.Default, app.RequestedThemeVariant); // «default» is the one that follows the system

        manager.Apply(ThemeChoice.Light, null);
        Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);
        Assert.Null(manager.Accent);
    }

    // --- The identity form ---

    [Fact]
    [Trait("spec", Spec + ": Feedback de la identidad (Guardado); Nombre del centro (Definir el nombre)")]
    public async Task Saving_tells_the_result_and_puts_the_identity_where_the_interface_reads_it()
    {
        var model = Model();
        model.Name = "Institut Exemple";
        model.AccentText = "#F2C6A0";

        await model.SaveAsync();

        Assert.Equal("Identitat del centre desada.", _notifications.Published.Single().Text);
        Assert.Equal("Institut Exemple", _model.Current.Name);
        Assert.Equal("#F2C6A0", _model.Current.Accent);
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback de la identidad (Vista previa)")]
    public void The_preview_shows_the_typed_name_and_accent_before_anything_is_saved_and_the_application_name_without_one()
    {
        var model = Model();
        Assert.Equal(_localizer.Get("App.Label.Title"), model.PreviewName);

        model.Name = "  Institut Exemple ";
        model.AccentText = "#F2C6A0";

        Assert.Equal("Institut Exemple", model.PreviewName);
        Assert.Equal("#F2C6A0", model.PreviewAccent);
        Assert.Empty(_saved); // nothing saved yet
    }

    [Fact]
    [Trait("spec", Spec + ": Color de acento")]
    public void A_text_that_is_not_a_colour_is_marked_and_gives_no_preview_accent()
    {
        var model = Model();

        model.AccentText = "vermell";

        Assert.NotNull(model.AccentError);
        Assert.Null(model.PreviewAccent);
        model.ResetAccentAction.Execute(null);
        Assert.Null(model.AccentError);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Logo del centro (Cargar un logo)")]
    public async Task A_valid_png_goes_to_the_preview_and_is_sent_when_saving()
    {
        var model = Model(picked: _png);

        await model.ChooseLogoAsync();
        Assert.Equal(_png, model.PreviewLogo);
        Assert.True(model.LogoWillChange);
        model.Name = "Centre";
        await model.SaveAsync();

        Assert.Equal(LogoChange.Replace, _saved.Single().Change);
        Assert.Equal(_png, _model.Current.Logo);
    }

    [AvaloniaTheory]
    [Trait("spec", Spec + ": Logo del centro (Formato no admitido, Imagen dañada)")]
    [InlineData("GIF89a-not-a-png", "PNG o JPEG")]
    [InlineData("damaged", "PNG o JPEG")]
    public async Task A_file_of_another_format_is_refused_when_chosen_and_the_current_logo_stays(string content, string expected)
    {
        _model.Set(new CentreIdentityView("Centre", _png, "image/png", null));
        var model = Model(picked: System.Text.Encoding.ASCII.GetBytes(content));

        await model.ChooseLogoAsync();

        Assert.Contains(expected, _notifications.Published.Single().Text, StringComparison.Ordinal);
        Assert.Equal(_png, model.PreviewLogo);
        Assert.False(model.LogoWillChange);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Logo del centro (Imagen dañada)")]
    public async Task A_file_with_the_signature_of_a_png_that_cannot_be_read_is_refused_and_the_logo_stays()
    {
        // The headless platform draws nothing and decodes nothing: only the rendering run (ARCA_SCREENSHOT) has a real decoder.
        Assert.SkipWhen(Environment.GetEnvironmentVariable("ARCA_SCREENSHOT") is null, "Needs the real decoder: set ARCA_SCREENSHOT=<folder>");
        _model.Set(new CentreIdentityView("Centre", _png, "image/png", null));
        var damaged = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };
        var model = Model(picked: damaged);

        await model.ChooseLogoAsync();

        Assert.Contains("No s'ha pogut llegir", _notifications.Published.Single().Text, StringComparison.Ordinal);
        Assert.Equal(_png, model.PreviewLogo);
    }

    [Fact]
    [Trait("spec", Spec + ": Logo del centro (Fichero demasiado grande)")]
    public async Task A_file_over_a_megabyte_is_refused_with_the_maximum()
    {
        var big = new byte[(1024 * 1024) + 1];
        _png.CopyTo(big, 0);
        var model = Model(picked: big);

        await model.ChooseLogoAsync();

        Assert.Contains("1 MB", _notifications.Published.Single().Text, StringComparison.Ordinal);
        Assert.False(model.LogoWillChange);
    }

    [Fact]
    [Trait("spec", Spec + ": Logo del centro (Quitar el logo)")]
    public async Task Removing_the_logo_is_sent_as_a_removal_and_the_action_says_why_it_is_off_without_a_logo()
    {
        var without = Model();
        Assert.False(without.RemoveLogoAction.IsAvailable);
        Assert.NotNull(without.RemoveLogoAction.UnavailableReason);

        _model.Set(new CentreIdentityView("Centre", _png, "image/png", null));
        var model = Model();
        model.RemoveLogoAction.Execute(null);
        await model.SaveAsync();

        Assert.Equal(LogoChange.Remove, _saved.Single().Change);
        Assert.Null(_model.Current.Logo);
    }

    [Fact]
    [Trait("spec", Spec + ": Nombre del centro (Nombre vacío)")]
    public async Task An_empty_name_is_refused_by_application_and_the_error_is_shown_without_changing_the_identity()
    {
        var model = Model(answer: Result<CentreIdentityView>.Failure(IdentityErrors.NameRequired));

        await model.SaveAsync();

        Assert.Contains("nom del centre", _notifications.Published.Single().Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CentreIdentityView.Empty, _model.Current);
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback de la identidad; tasks 6.3")]
    public async Task Saving_twice_at_once_saves_once()
    {
        var gate = new TaskCompletionSource<Result<CentreIdentityView>>();
        var calls = 0;
        var model = new IdentityViewModel(
            new IdentityServices(_ => Task.FromResult(Result<CentreIdentityView>.Success(CentreIdentityView.Empty)), (_, _) =>
            {
                calls++;
                return gate.Task;
            }, _ => null),
            _model, new FakePicker(null), _localizer, _notifications, _log, _delay);
        model.Name = "Centre";

        var first = model.SaveAsync();
        await model.SaveAsync();
        gate.SetResult(Result<CentreIdentityView>.Success(new CentreIdentityView("Centre", null, null, null)));
        await first;

        Assert.Equal(1, calls);
        Assert.Single(_notifications.Published);
    }

    // --- The header ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Nombre del centro (Definir el nombre, Sin nombre definido); Logo del centro (Cargar un logo, Quitar el logo)")]
    public void The_header_shows_the_name_and_the_logo_of_the_centre_and_follows_every_save()
    {
        var state = new Arca.UI.Shell.GlobalStateService(
            _ => Task.FromResult(Result<Arca.Application.GlobalState.GlobalState>.Success(new Arca.Application.GlobalState.GlobalState(null, 0))),
            new Arca.UI.Notifications.ResultNotifier(_notifications, _localizer, _log));
        var header = new HeaderView(state, _localizer, "ARCA");
        header.ShowIdentity(_model);
        Assert.Equal("ARCA", header.Name.Text); // no name defined yet: the name of the application
        Assert.Null(header.Logo.Content);

        _model.Set(new CentreIdentityView("Institut Exemple", _png, "image/png", null));
        Assert.Equal("Institut Exemple", header.Name.Text);
        Assert.NotNull(header.Logo.Content);

        _model.Set(new CentreIdentityView("Institut Exemple", null, null, null));
        Assert.Null(header.Logo.Content); // without the logo, only the name
    }

    [AvaloniaTheory]
    [InlineData("light", false)]
    [InlineData("dark", true)]
    public void Screenshot_of_the_identity_settings(string name, bool dark)
    {
        _model.Set(new CentreIdentityView("Institut Exemple", _png, "image/png", "#F2C6A0"));
        var identity = Model();
        var themes = new ThemeSettingsViewModel(new UiPreferencesSession(new MemoryStore()), _model, (_, _) => { });
        var content = new StackPanel { Spacing = 16, Margin = new Avalonia.Thickness(24) };
        content.Children.Add(IdentitySettingsView.Identity(identity, _localizer));
        content.Children.Add(IdentitySettingsView.Theme(themes, _localizer));
        var window = new Window { Content = content, Width = 700, Height = 620, RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light };
        window.Bind(Window.BackgroundProperty, window.GetResourceObservable(ArcaResourceKeys.Background));
        ScreenshotTests.Take(window, "identity-" + name);
        window.Close();
    }
}
