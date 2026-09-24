// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Testing;
using Xunit;

namespace Arca.Application.Tests.Localization;

#pragma warning disable CA1711 // xunit names collection definitions by their attribute, not by a type suffix
[CollectionDefinition("Culture", DisableParallelization = true)]
public sealed class CultureTestCollection;
#pragma warning restore CA1711

[Collection("Culture")]
public sealed class LocalizationTests
{
    const string Spec = "arquitectura-base/internacionalitzacio";

    static readonly ResourceSource _sample = new(typeof(FakeClock).Assembly, "Arca.Testing.Resources");

    [Fact]
    [Trait("spec", Spec + ": Errores de negocio con código estable")]
    public void Error_message_comes_from_a_resource_derived_from_the_code()
    {
        var localizer = new ResxLocalizer();

        var text = localizer.Message(StorageErrors.PathNotAccessible("/dades/arca.db"));

        Assert.Contains("/dades/arca.db", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Storage.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_without_arguments_resolves_too()
    {
        Assert.Contains("versió més nova", new ResxLocalizer().Message(StorageErrors.SchemaNewer), StringComparison.Ordinal);
    }

    [Fact]
    public void Unexpected_error_carries_its_log_reference()
    {
        var text = new ResxLocalizer().Message(CommonErrors.Unexpected("A7F3"));

        Assert.Contains("A7F3", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Lockers.NumberInUse", "Lockers.Error.NumberInUse")]
    [InlineData("SchoolYears.NotActive", "SchoolYears.Error.NotActive")]
    public void Error_code_derives_its_key(string code, string key)
    {
        Assert.Equal(key, ResourceKeys.For(new Error(code)));
    }

    [Fact]
    public void Notice_code_derives_a_warning_key()
    {
        Assert.Equal("Assignments.Warning.PriorDebt", ResourceKeys.For(new Notice("Assignments.PriorDebt")));
    }

    [Fact]
    public void Capability_is_the_first_segment_of_the_key()
    {
        Assert.Equal("History", ResourceKeys.CapabilityOf("History.Locker.NumberChanged"));
        Assert.Equal("Lockers", ResourceKeys.CapabilityOf("Lockers.Label.Number"));
    }

    [Fact]
    [Trait("spec", Spec + ": Detección de claves faltantes (clave inexistente en ejecución)")]
    public void Unknown_key_shows_the_key_itself_and_does_not_fail()
    {
        var localizer = new ResxLocalizer();

        Assert.Equal("Nothing.Label.Here", localizer.Get("Nothing.Label.Here"));
        Assert.Equal("Storage.Label.Missing", localizer.Get("Storage.Label.Missing"));
        Assert.Equal("NoDots", localizer.Get("NoDots", 1, 2));
    }

    [Fact]
    [Trait("spec", Spec + ": Catalán como único idioma de la v1")]
    public void Base_language_is_catalan()
    {
        var localizer = new ResxLocalizer(sources: [_sample]);

        Assert.Equal("ca-ES", localizer.Culture.Name);
        Assert.Equal("Tots dos", localizer.Get("Sample.Label.Both"));
    }

    [Fact]
    [Trait("spec", Spec + ": Añadir un idioma sin cambiar código (idioma con traducción incompleta)")]
    public void Incomplete_extra_language_falls_back_to_catalan_per_key()
    {
        var spanish = new ResxLocalizer(new CultureInfo("es"), _sample);

        Assert.Equal("Los dos", spanish.Get("Sample.Label.Both")); // translated
        Assert.Equal("Només en català", spanish.Get("Sample.Label.OnlyCatalan")); // missing in es: Catalan text
        Assert.Equal("Sample.Label.Nobody", spanish.Get("Sample.Label.Nobody")); // missing everywhere: the key
    }

    [Fact]
    public void An_extra_language_with_no_resources_at_all_shows_catalan_messages()
    {
        var english = new ResxLocalizer(new CultureInfo("en"));

        Assert.Contains("versió més nova", english.Message(StorageErrors.SchemaNewer), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Formatos según la cultura catalana (importe)")]
    public void Amounts_are_formatted_in_catalan_even_when_the_system_is_in_another_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            var text = new ResxLocalizer().Format(Money.FromCents(123450));

            Assert.Equal("1.234,50 €", text.Replace(' ', ' '));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Formatos según la cultura catalana (fecha)")]
    public void Dates_are_short_catalan_dates_with_the_day_first_even_in_another_system_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US"); // month first

            var text = new ResxLocalizer().Format(new DateOnly(2026, 9, 4));

            Assert.StartsWith("4/9/", text, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Instants_are_formatted_with_the_catalan_culture()
    {
        var text = new ResxLocalizer().Format(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));

        Assert.Contains("/9/", text, StringComparison.Ordinal); // day/month/year, not month/day
    }

    [Fact]
    [Trait("spec", Spec + ": Catalán como único idioma de la v1 (equipo con sistema operativo en otro idioma)")]
    public async Task Culture_setup_makes_every_thread_catalan_regardless_of_the_system()
    {
        var previousCulture = CultureInfo.DefaultThreadCurrentCulture;
        var previousUi = CultureInfo.DefaultThreadCurrentUICulture;
        var current = CultureInfo.CurrentCulture;
        var currentUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-ES");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("es-ES");

            CultureSetup.Apply();

            var seen = await Task.Run(() => (CultureInfo.CurrentCulture.Name, CultureInfo.CurrentUICulture.Name));
            Assert.Equal(("ca-ES", "ca-ES"), seen);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = current;
            CultureInfo.CurrentUICulture = currentUi;
        }
    }
}
