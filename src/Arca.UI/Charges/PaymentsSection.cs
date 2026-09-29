// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.ListDebtors;
using Arca.Application.Localization;
using Arca.Application.Students.ListStudentRows;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Arca.UI.Charges;

/// <summary>Builds the Payments section: pending payments first, and the charges of a student found by a search, as tabs.</summary>
public static class PaymentsSection
{
    public static Control Create(ChargeServices services, ScreenContext context, Func<Task> openCourse)
    {
        var localizer = context.Localizer;
        ChargesScreenViewModel? charges = null;
        SectionScreens? section = null;
        var debtors = new DebtorsViewModel(services, context, async id =>
        {
            section!.Open("Charges");
            await charges!.OpenAsync(id);
        });
        charges = new ChargesScreenViewModel(services, context, new StudentChargesViewModel(services, context, openCourse));
        section = new SectionScreens(
            ShellCatalog.Payments,
            [
                new("Debtors", () => DebtorsView(debtors, localizer)),
                new("Charges", () => ChargesView(charges, localizer)),
            ],
            localizer);
        return section;
    }

    static ScreenView DebtorsView(DebtorsViewModel model, ILocalizer localizer)
    {
        var year = Filter(() => model.YearOptions, () => model.YearFilter, v => model.YearFilter = v, model, nameof(DebtorsViewModel.YearOptions));
        var concept = Filter(() => model.ConceptOptions, () => model.ConceptFilter, v => model.ConceptFilter = v, model, null);
        var zone = Filter(() => model.ZoneOptions, () => model.ZoneFilter, v => model.ZoneFilter = v, model, nameof(DebtorsViewModel.ZoneOptions));
        var level = Filter(() => model.LevelOptions, () => model.LevelFilter, v => model.LevelFilter = v, model, nameof(DebtorsViewModel.LevelOptions));
        var group = Filter(() => model.GroupOptions, () => model.GroupFilter, v => model.GroupFilter = v, model, nameof(DebtorsViewModel.GroupOptions));
        var filters = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        foreach (var box in new[] { year, concept, zone, level, group })
        {
            filters.Children.Add(box);
        }

        var list = new ScreenListView<DebtorRow, Guid>(model.Debtors, localizer, filters);
        var totals = new TextBlock().Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary);
        totals.Bind(TextBlock.TextProperty, new Binding(nameof(DebtorsViewModel.TotalsText)) { Source = model });
        var left = new DockPanel();
        DockPanel.SetDock(totals, Dock.Bottom);
        left.Children.Add(totals);
        left.Children.Add(list);

        var detail = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium).ThemedThickness(Control.MarginProperty, ArcaResourceKeys.SpacingMedium);
        void Rebuild()
        {
            detail.Children.Clear();
            if (model.Debtors.Current is not { } row)
            {
                detail.Children.Add(new TextBlock { Text = localizer.Get("Charges.Empty.PickDebtor"), TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                    .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary));
                return;
            }

            detail.Children.Add(ThemedText.Title(row.FirstName + " " + row.LastName + (row.IsRetired ? " · " + localizer.Get("Students.State.Retired") : string.Empty)));
            foreach (var line in model.Breakdown)
            {
                detail.Children.Add(new TextBlock { Text = "• " + line, TextWrapping = Avalonia.Media.TextWrapping.Wrap }.Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text));
            }

            detail.Children.Add(ActionControls.Button(model.OpenCharges));
        }

        model.Debtors.CurrentChanged += (_, _) => Rebuild();
        Rebuild();
        var screen = new ScreenView(localizer.Get("Shell.Screen.Debtors"), [], left, detail);
        screen.AttachedToVisualTree += (_, _) => _ = model.LoadAsync();
        return screen;
    }

    static ScreenView ChargesView(ChargesScreenViewModel model, ILocalizer localizer)
    {
        var list = new ScreenListView<StudentListRow, Guid>(model.Students, localizer);
        var screen = new ScreenView(localizer.Get("Shell.Screen.Charges"), [], list, new StudentChargesPanel(model.Charges, localizer));
        screen.AttachedToVisualTree += (_, _) => _ = model.LoadAsync();
        return screen;
    }

    static ComboBox Filter(Func<IReadOnlyList<FormOption>> options, Func<string> current, Action<string> set, System.ComponentModel.INotifyPropertyChanged model, string? optionsProperty)
    {
        var box = new ComboBox { ItemsSource = options(), MinWidth = 130 };
        box.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<FormOption>((o, _) => new TextBlock { Text = o?.Label });
        box.SelectedItem = options().FirstOrDefault(o => o.Id == current());
        box.SelectionChanged += (_, _) =>
        {
            if (box.SelectedItem is FormOption option)
            {
                set(option.Id);
            }
        };
        if (optionsProperty is not null)
        {
            model.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == optionsProperty)
                {
                    var keep = current();
                    box.ItemsSource = options();
                    box.SelectedItem = options().FirstOrDefault(o => o.Id == keep) ?? (options().Count > 0 ? options()[0] : null);
                }
            };
        }

        return box;
    }
}
