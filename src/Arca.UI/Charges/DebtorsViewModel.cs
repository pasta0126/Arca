// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Charges;
using Arca.Application.Charges.ListDebtors;
using Arca.Application.ConceptAmounts;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Screens;

namespace Arca.UI.Charges;

/// <summary>
/// The pending payments view of the Payments section (pantalles-cobraments, Consulta de morosos; the interface says «Pendents de
/// pagament», never «morosos»): the students who owe something, by surname, with what each owes, filters by year, concept, level,
/// group and zone, the number of students and the total according to the filters, and the breakdown of the one chosen by concept and
/// year. When nothing is pending it says so as good news, not as an empty list. No email and no identifier anywhere.
/// </summary>
public sealed class DebtorsViewModel : ObservableObject
{
    readonly ChargeServices _services;
    readonly ScreenContext _context;
    IReadOnlyDictionary<Guid, string> _yearNames = new Dictionary<Guid, string>();
    DebtorsEmptyState _emptyState;
    IReadOnlyList<FormOption> _yearOptions = [];
    IReadOnlyList<FormOption> _zoneOptions = [];
    IReadOnlyList<FormOption> _levelOptions = [];
    IReadOnlyList<FormOption> _groupOptions = [];
    string _yearFilter = string.Empty;
    string _conceptFilter = string.Empty;
    string _zoneFilter = string.Empty;
    string _levelFilter = string.Empty;
    string _groupFilter = string.Empty;

    public DebtorsViewModel(ChargeServices services, ScreenContext context, Func<Guid, Task> openCharges)
    {
        _services = services;
        _context = context;
        var text = context.Localizer;
        Debtors = new ScreenListViewModel<DebtorRow, Guid>(
            [
                new ListColumn<DebtorRow>("last", text.Get("Students.Label.LastName"), d => d.LastName, Width: 3),
                new ListColumn<DebtorRow>("first", text.Get("Students.Label.FirstName"), d => d.FirstName, Width: 2),
                new ListColumn<DebtorRow>("level", text.Get("Students.Label.Level"), d => d.LevelName ?? string.Empty, Width: 2),
                new ListColumn<DebtorRow>("group", text.Get("Students.Label.Group"), d => d.GroupName ?? string.Empty, Width: 1),
                new ListColumn<DebtorRow>("locker", text.Get("Students.Label.Locker"), d => d.LockerNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, d => d.LockerNumber ?? int.MaxValue, Width: 1),
                new ListColumn<DebtorRow>("pending", text.Get("Charges.Label.Pending"), d => text.Format(Money.FromCents((long)Math.Round(d.PendingTotal * 100))), d => d.PendingTotal, Width: 2),
                new ListColumn<DebtorRow>("state", text.Get("Students.Label.State"), d => d.IsRetired ? text.Get("Students.State.Retired") : string.Empty, Width: 1),
            ],
            d => d.StudentId, ReadRowsAsync, text, context.Notifications, context.Log, () => text.Get(_emptyState == DebtorsEmptyState.NoDebt ? "Charges.Empty.NoDebt" : "Charges.Empty.NoDebtResults"));
        Debtors.List.SortBy("last");
        Debtors.List.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ListViewModel<DebtorRow, Guid>.Rows))
            {
                Raise(nameof(TotalsText));
            }
        };
        OpenCharges = new AppAction("OpenCharges", text.Get("Charges.Action.OpenCharges"));
        OpenCharges.Attach(() => _ = Debtors.TryGetSelectedKey(out var id) ? openCharges(id) : Task.CompletedTask,
            () => Debtors.Current is null ? Availability.Unavailable(text.Get("Charges.Reason.PickStudent")) : Availability.Available);
        Debtors.CurrentChanged += (_, _) =>
        {
            OpenCharges.Refresh();
            Raise(nameof(Breakdown));
        };
    }

    public ScreenListViewModel<DebtorRow, Guid> Debtors { get; }

    /// <summary>Goes from the student chosen to all their charges, where they are managed.</summary>
    public AppAction OpenCharges { get; }

    // --- Filters ---

    public IReadOnlyList<FormOption> YearOptions { get => _yearOptions; private set => Set(ref _yearOptions, value); }

    public IReadOnlyList<FormOption> ZoneOptions { get => _zoneOptions; private set => Set(ref _zoneOptions, value); }

    public IReadOnlyList<FormOption> LevelOptions { get => _levelOptions; private set => Set(ref _levelOptions, value); }

    public IReadOnlyList<FormOption> GroupOptions { get => _groupOptions; private set => Set(ref _groupOptions, value); }

    public IReadOnlyList<FormOption> ConceptOptions =>
    [
        new(string.Empty, _context.Localizer.Get("Charges.Label.AllConcepts")),
        .. ConceptNames.All.Select(c => new FormOption(c, ConceptNames.Of(_context.Localizer, c))),
    ];

    /// <summary>The year, concept and zone narrow what is asked for, so changing one reads the list again.</summary>
    public string YearFilter { get => _yearFilter; set => SetServerFilter(ref _yearFilter, value, nameof(YearFilter)); }

    public string ConceptFilter { get => _conceptFilter; set => SetServerFilter(ref _conceptFilter, value, nameof(ConceptFilter)); }

    public string ZoneFilter { get => _zoneFilter; set => SetServerFilter(ref _zoneFilter, value, nameof(ZoneFilter)); }

    /// <summary>The level and the group narrow the rows already read.</summary>
    public string LevelFilter
    {
        get => _levelFilter;
        set
        {
            if (Set(ref _levelFilter, value))
            {
                ApplyLocalFilters();
            }
        }
    }

    public string GroupFilter
    {
        get => _groupFilter;
        set
        {
            if (Set(ref _groupFilter, value))
            {
                ApplyLocalFilters();
            }
        }
    }

    void SetServerFilter(ref string field, string value, string name)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Raise(name);
        _ = Debtors.LoadAsync();
    }

    void ApplyLocalFilters() => Debtors.List.SetPredicate(d =>
        (LevelFilter.Length == 0 || d.LevelName == LevelFilter) && (GroupFilter.Length == 0 || d.GroupName == GroupFilter));

    /// <summary>Removes the search and every filter, which is what the state of a list without results offers.</summary>
    public void ClearFilters()
    {
        _yearFilter = _conceptFilter = _zoneFilter = _levelFilter = _groupFilter = string.Empty;
        foreach (var name in new[] { nameof(YearFilter), nameof(ConceptFilter), nameof(ZoneFilter), nameof(LevelFilter), nameof(GroupFilter) })
        {
            Raise(name);
        }

        Debtors.ClearFilter();
        ApplyLocalFilters();
        _ = Debtors.LoadAsync();
    }

    /// <summary>"3 alumnes · 250,00 €": how many students and how much, according to the filters.</summary>
    public string TotalsText
    {
        get
        {
            var rows = Debtors.List.Rows;
            return _context.Localizer.Get(
                "Charges.Label.Totals", rows.Count, _context.Localizer.Format(Money.FromCents((long)Math.Round(rows.Sum(r => r.PendingTotal) * 100))));
        }
    }

    /// <summary>The debt of the student chosen by concept and year, one line each: the breakdown.</summary>
    public IReadOnlyList<string> Breakdown
    {
        get
        {
            if (Debtors.Current is not { } row)
            {
                return [];
            }

            var text = _context.Localizer;
            return [.. row.Breakdown.Select(l => text.Get(
                "Charges.Label.DebtLine", ConceptNames.Of(text, l.Concept.ToString()), _yearNames.GetValueOrDefault(l.YearId, string.Empty),
                text.Format(Money.FromCents((long)Math.Round(l.Amount * 100)))))];
        }
    }

    public async Task LoadAsync(CancellationToken ct = default) => await Debtors.LoadAsync(ct);

    async Task<Result<IReadOnlyList<DebtorRow>>> ReadRowsAsync(CancellationToken ct)
    {
        var text = _context.Localizer;
        var years = await _services.ListYears(ct);
        if (years.IsSuccess)
        {
            _yearNames = years.Value!.ToDictionary(y => y.Id, y => y.Name);
            YearOptions = [new(string.Empty, text.Get("Charges.Label.AllYears")), .. years.Value!.Select(y => new FormOption(y.Id.ToString(), y.Name))];
        }

        var zones = await _services.ListZones(ct);
        if (zones.IsSuccess)
        {
            ZoneOptions = [new(string.Empty, text.Get("Lockers.Label.AllZones")), .. zones.Value!.Select(z => new FormOption(z.Id.ToString(), z.Name))];
        }

        var listing = await _services.ListDebtors(
            Guid.TryParse(YearFilter, out var year) ? year : null, ConceptFilter.Length == 0 ? null : ConceptFilter, Guid.TryParse(ZoneFilter, out var zone) ? zone : null, ct);
        if (!listing.IsSuccess)
        {
            return Result<IReadOnlyList<DebtorRow>>.Failure(listing.Error!);
        }

        _emptyState = listing.Value!.EmptyState;
        LevelOptions = [new(string.Empty, text.Get("Students.Label.AllLevels")), .. listing.Value.Rows.Select(r => r.LevelName).Where(n => n is not null).Distinct().Order(TextComparer.Comparer).Select(n => new FormOption(n!, n!))];
        GroupOptions = [new(string.Empty, text.Get("Students.Label.AllGroups")), .. listing.Value.Rows.Select(r => r.GroupName).Where(n => n is not null).Distinct().Order(TextComparer.Comparer).Select(n => new FormOption(n!, n!))];
        Raise(nameof(Breakdown));
        return Result<IReadOnlyList<DebtorRow>>.Success(listing.Value.Rows);
    }
}
