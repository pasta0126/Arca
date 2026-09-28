// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Lists;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

public sealed class ListTests
{
    const string Spec = "ux-fonaments/llistes-i-passos";

    sealed record Student(Guid Id, string Name, int Locker);

    readonly ResxLocalizer _localizer = new();

    ListViewModel<Student, Guid> Model() => new(
        [
            new ListColumn<Student>("name", "Nom", s => s.Name, Width: 3),
            new ListColumn<Student>("locker", "Taquilla", s => s.Locker.ToString(System.Globalization.CultureInfo.InvariantCulture), s => s.Locker),
        ],
        s => s.Id, _localizer);

    static List<Student> Students(int count) =>
        [.. Enumerable.Range(1, count).Select(i => new Student(Guid.NewGuid(), $"Alumne {i:000}", i))];

    // --- Order ---

    [Fact]
    [Trait("spec", Spec + ": Lista virtualizada con orden y filtro (Orden por columna)")]
    public void A_header_orders_by_its_column_and_pressing_it_again_reverses_the_order()
    {
        var model = Model();
        model.SetItems([new Student(Guid.NewGuid(), "Berta", 10), new Student(Guid.NewGuid(), "Àlex", 2), new Student(Guid.NewGuid(), "Zoe", 33)]);

        model.SortBy("name");
        Assert.Equal(["Àlex", "Berta", "Zoe"], model.Rows.Select(r => r.Name)); // as people read it: the accent does not put Àlex last
        model.SortBy("name");

        Assert.Equal(["Zoe", "Berta", "Àlex"], model.Rows.Select(r => r.Name));
        Assert.True(model.SortDescending);
    }

    [Fact]
    [Trait("spec", Spec + ": Lista virtualizada con orden y filtro (Orden por columna)")]
    public void A_column_with_a_sort_key_orders_by_it_so_numbers_order_as_numbers()
    {
        var model = Model();
        model.SetItems([new Student(Guid.NewGuid(), "A", 10), new Student(Guid.NewGuid(), "B", 2), new Student(Guid.NewGuid(), "C", 33)]);

        model.SortBy("locker");

        Assert.Equal([2, 10, 33], model.Rows.Select(r => r.Locker)); // as text, "10" would come before "2"
    }

    [Fact]
    [Trait("spec", Spec + ": Lista virtualizada con orden y filtro (Orden por columna)")]
    public void Ordering_by_another_column_starts_ascending_again()
    {
        var model = Model();
        model.SetItems(Students(3));
        model.SortBy("name");
        model.SortBy("name");

        model.SortBy("locker");

        Assert.False(model.SortDescending);
        Assert.Equal("locker", model.SortColumnId);
    }

    // --- Filter and selection ---

    [Fact]
    [Trait("spec", Spec + ": Lista virtualizada con orden y filtro (Selección y filtro)")]
    public void The_marks_of_rows_a_filter_hides_are_kept_and_counted()
    {
        var model = Model();
        var students = Students(300);
        model.SetItems(students);
        foreach (var student in students.Take(10))
        {
            model.SetSelected(student, true);
        }

        model.FilterText = "alumne 25"; // 025, 125, 225 and 250 to 259: none of the marked ones (001 to 010)

        Assert.Equal(13, model.Rows.Count);
        Assert.Equal(10, model.SelectedCount);
        Assert.Equal(10, model.HiddenSelectedCount);
        Assert.Equal("10 de 300", model.SelectionText);

        model.FilterText = string.Empty;
        Assert.Equal(0, model.HiddenSelectedCount);
        Assert.Equal(10, model.SelectedCount);
    }

    [Fact]
    [Trait("spec", Spec + ": Lista virtualizada con orden y filtro (Selección y filtro)")]
    public void The_filter_ignores_case_and_accents_and_can_be_combined_with_another_condition()
    {
        var model = Model();
        model.SetItems([new Student(Guid.NewGuid(), "Núria Puig", 1), new Student(Guid.NewGuid(), "Nuria Roca", 2), new Student(Guid.NewGuid(), "Pau Abad", 3)]);

        model.FilterText = "NURIA";
        Assert.Equal(2, model.Rows.Count);

        model.SetPredicate(s => s.Locker > 1);
        Assert.Equal(["Nuria Roca"], model.Rows.Select(r => r.Name));

        model.SetPredicate(null);
        Assert.Equal(2, model.Rows.Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Lista virtualizada con orden y filtro (Selección y filtro)")]
    public void The_selection_survives_ordering_because_it_belongs_to_the_rows_and_not_to_positions()
    {
        var model = Model();
        var students = Students(5);
        model.SetItems(students);
        model.SetSelected(students[0], true);

        model.SortBy("name");
        model.SortBy("name"); // now the first student is the last row

        Assert.True(model.IsSelected(model.Rows[^1]));
        Assert.False(model.IsSelected(model.Rows[0]));
        Assert.Equal(1, model.SelectedCount);
    }

    [Fact]
    [Trait("spec", Spec + ": Selección múltiple editable (Recuento)")]
    public void Loading_the_data_again_keeps_the_marks_of_the_rows_still_there_and_drops_the_others()
    {
        var model = Model();
        var students = Students(4);
        model.SetItems(students);
        students.ForEach(s => model.SetSelected(s, true));

        model.SetItems([.. students.Take(3).Select(s => s with { Name = s.Name + " (editat)" })]); // new objects, same identities

        Assert.Equal(3, model.SelectedCount);
        Assert.Equal("3 de 3", model.SelectionText);
        Assert.All(model.Rows, r => Assert.True(model.IsSelected(r)));
    }

    [Fact]
    [Trait("spec", Spec + ": Selección múltiple editable (Recuento)")]
    public void Unmarking_five_of_three_hundred_shows_295_of_300()
    {
        var model = Model();
        var students = Students(300);
        model.SetItems(students);
        students.ForEach(s => model.SetSelected(s, true));

        students.Take(5).ToList().ForEach(s => model.Toggle(s));

        Assert.Equal("295 de 300", model.SelectionText);
    }

    // --- The view ---

    static (VirtualizedListView<Student, Guid> View, Window Window) Show(ListViewModel<Student, Guid> model)
    {
        var view = new VirtualizedListView<Student, Guid>(model);
        var window = new Window { Width = 700, Height = 400, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (view, window);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Lista virtualizada con orden y filtro (Lista larga)")]
    public void A_list_of_300_rows_builds_only_the_rows_in_view()
    {
        var model = Model();
        model.SetItems(Students(300));

        var (view, _) = Show(model);

        var built = view.List.GetRealizedContainers().Count();
        Assert.InRange(built, 1, 60);
        Assert.Equal(300, model.TotalCount);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Lista virtualizada con orden y filtro (Orden por columna)")]
    public void Pressing_a_header_orders_the_rows_shown_and_marks_the_column()
    {
        var model = Model();
        model.SetItems([new Student(Guid.NewGuid(), "Zoe", 1), new Student(Guid.NewGuid(), "Àlex", 2)]);
        var (view, _) = Show(model);

        view.HeaderButtons[0].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Àlex", model.Rows[0].Name);
        Assert.Equal("Àlex", ((Student)view.List.ItemsSource!.Cast<object>().First()).Name);
        Assert.Equal("Nom ▲", view.HeaderButtons[0].Content);
    }

    [AvaloniaFact]
    [Trait("spec", Spec + ": Selección múltiple editable (Seleccionar con el teclado)")]
    public void Space_marks_and_unmarks_the_focused_row_and_the_count_follows()
    {
        var model = Model();
        model.SetItems(Students(3));
        var (view, window) = Show(model);
        view.List.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        view.List.ContainerFromIndex(0)!.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, model.SelectedCount);
        Assert.Equal("1 de 3", view.CountText);

        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, model.SelectedCount);
    }
}
