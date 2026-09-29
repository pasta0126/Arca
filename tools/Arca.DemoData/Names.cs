// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.DemoData;

/// <summary>
/// Invented names to fill a demonstration centre. Nothing here belongs to a real person: the lists are written for this tool,
/// and the emails use the domain test.cat, which is not anybody's.
/// </summary>
static class Names
{
    public static readonly string[] First =
    [
        "Aina", "Arnau", "Berta", "Biel", "Carla", "Cesc", "Clàudia", "Dani", "Eva", "Ferran", "Gemma", "Guillem", "Helena", "Hugo",
        "Iris", "Jan", "Júlia", "Laia", "Leo", "Lluna", "Marc", "Mireia", "Nil", "Núria", "Oriol", "Paula", "Pau", "Queralt", "Roger",
        "Sara", "Sergi", "Sílvia", "Talia", "Toni", "Ubaldo", "Valèria", "Vera", "Xavier", "Yara", "Zoe", "Àlex", "Èlia", "Ivet", "Jordi",
    ];

    public static readonly string[] Last =
    [
        "Abad", "Bosch", "Camps", "Casals", "Clos", "Costa", "Diaz", "Esteve", "Farré", "Ferrer", "Font", "Garcia", "Gil", "Grau",
        "Guasch", "Jané", "Lluch", "Marín", "Martí", "Mas", "Molina", "Montserrat", "Navarro", "Olivé", "Pla", "Prat", "Puig", "Quer",
        "Ribas", "Roca", "Roig", "Salvà", "Sala", "Serra", "Soler", "Tarrés", "Torres", "Valls", "Vidal", "Vila", "Xicota", "Ylla",
    ];

    public static readonly string[] Zones = ["Planta baixa", "Planta 1", "Planta 2", "Gimnàs", "Pati", "Annex"];

    public static readonly int[] ZoneSizes = [120, 140, 140, 80, 60, 60];

    public static readonly string[] Levels = ["1r ESO", "2n ESO", "3r ESO", "4t ESO", "1r Batxillerat", "2n Batxillerat"];

    public static readonly string[] Groups = ["A", "B", "C", "D"];
}
