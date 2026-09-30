// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;

namespace Arca.UI.Shell;

/// <summary>
/// A section of the sidebar (ui-shell, D1): everything the frame needs to draw it and to open it. Adding or moving a screen
/// is editing a definition; the window never changes.
/// </summary>
/// <param name="Id">A stable name.</param>
/// <param name="TitleKey">The resource key of its title.</param>
/// <param name="Order">Where it goes in the sidebar, from the top.</param>
/// <param name="CreateRoot">Builds the screen that opens with the section. Null shows the list of what the section will hold.</param>
/// <param name="Attention">How many things need attention in it (a count for its badge), or null if it has none to show.</param>
public sealed record SectionDefinition(string Id, string TitleKey, int Order, Func<Control>? CreateRoot = null, Func<int>? Attention = null);

/// <summary>A screen of the application and the one section that holds it.</summary>
public sealed record ScreenDefinition(string Id, string TitleKey, string SectionId);

/// <summary>The sections and screens of the application, as the specification distributes them (navegacio-i-cerca).</summary>
public static class ShellCatalog
{
    public const string Home = "Home";
    public const string Lockers = "Lockers";
    public const string Students = "Students";
    public const string KeysAndIncidents = "KeysAndIncidents";
    public const string Reports = "Reports";
    public const string Course = "Course";
    public const string Settings = "Settings";

    /// <summary>The seven sections, in the order of the sidebar. Their roots are supplied by whoever composes the application.</summary>
    public static IReadOnlyList<SectionDefinition> Sections { get; } =
    [
        Section(Home, 1), Section(Lockers, 2), Section(Students, 3),
        Section(KeysAndIncidents, 4), Section(Reports, 5), Section(Course, 6), Section(Settings, 7),
    ];

    /// <summary>
    /// Every screen in its one section: Lockers (lockers, zones and their history), Students (students, their charges and
    /// deposit, import and assignments), Keys and incidents, Reports, Course (current year, amounts,
    /// closing and data retention) and Settings (identity, theme, registration, backup, guided setup, incident reasons and data folder).
    /// </summary>
    public static IReadOnlyList<ScreenDefinition> Screens { get; } =
    [
        Screen("LockerMap", Home),
        Screen("Lockers", Lockers), Screen("Zones", Lockers), Screen("LockerHistory", Lockers),
        Screen("Students", Students), Screen("StudentImport", Students), Screen("Assignments", Students),
        Screen("Deposits", Students),
        Screen("Keys", KeysAndIncidents), Screen("Incidents", KeysAndIncidents), Screen("BulkMaintenance", KeysAndIncidents),
        Screen("Reports", Reports), Screen("Export", Reports),
        Screen("CurrentYear", Course), Screen("Amounts", Course), Screen("YearClosing", Course), Screen("DataRetention", Course),
        Screen("Identity", Settings), Screen("Theme", Settings), Screen("Registration", Settings), Screen("Backup", Settings),
        Screen("Setup", Settings), Screen("IncidentReasons", Settings), Screen("DataFolder", Settings),
    ];

    static SectionDefinition Section(string id, int order) => new(id, "Shell.Section." + id, order);

    static ScreenDefinition Screen(string id, string section) => new(id, "Shell.Screen." + id, section);
}
