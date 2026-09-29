// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Zones;
using Arca.Application.Zones.ListZoneRows;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Screens;

namespace Arca.UI.Lockers;

/// <summary>
/// The Zones view of the Lockers section (pantalles-taquilles-i-zones, Vista de zonas): the zones ordered as Catalan is read, each
/// with its state and its number of active lockers, and the operations on the one chosen: rename, deactivate, reactivate and delete.
/// Each operation that does not apply is shown disabled with the reason Application gives.
/// </summary>
public sealed class ZonesViewModel : ObservableObject
{
    readonly LockerServices _services;
    readonly ScreenContext _context;
    readonly OneAtATime _once = new();

    public ZonesViewModel(LockerServices services, ScreenContext context, Func<Task> afterChange)
    {
        _services = services;
        _context = context;
        AfterChange = afterChange;
        var text = context.Localizer;
        NewZone = new AppAction("NewZone", text.Get("Zones.Action.New"));
        NewZone.Attach(() => _ = NewZoneAsync());
        Zones = new ScreenListViewModel<ZoneRow, Guid>(
            [
                new ListColumn<ZoneRow>("name", text.Get("Zones.Label.Name"), z => z.Name, Width: 3),
                new ListColumn<ZoneRow>("state", text.Get("Zones.Label.State"), z => StateOf(z), Width: 2),
                new ListColumn<ZoneRow>("lockers", text.Get("Zones.Label.ActiveLockers"), z => z.ActiveLockers.ToString(CultureInfo.InvariantCulture), z => z.ActiveLockers, Width: 2),
            ],
            z => z.Id, services.ListZones, text, context.Notifications, context.Log,
            () => text.Get("Zones.Empty.NoZones"), () => new EmptyStateAction(NewZone.Label, NewZone));
        Detail = new DetailViewModel<Guid, ZoneRow>(LoadAsync, BuildActions, text, context.Notifications, context.Log);
        Zones.CurrentChanged += (_, _) => _ = ShowCurrentAsync();
    }

    public ScreenListViewModel<ZoneRow, Guid> Zones { get; }

    public DetailViewModel<Guid, ZoneRow> Detail { get; }

    public AppAction NewZone { get; }

    public IReadOnlyList<AppAction> MainActions => [NewZone];

    /// <summary>What the rest of the section refreshes after a zone changes: the lockers list shows zone names and filters.</summary>
    Func<Task> AfterChange { get; }

    /// <summary>The state of a zone, told with words.</summary>
    public string StateOf(ZoneRow zone) => _context.Localizer.Get(zone.IsActive ? "Zones.State.Active" : "Zones.State.Inactive");

    public async Task LoadAsync(CancellationToken ct = default) => await Zones.LoadAsync(ct);

    async Task<Result<ZoneRow?>> LoadAsync(Guid id, CancellationToken ct)
    {
        var rows = await _services.ListZones(ct);
        return rows.IsSuccess ? Result<ZoneRow?>.Success(rows.Value!.FirstOrDefault(z => z.Id == id)) : Result<ZoneRow?>.Failure(rows.Error!);
    }

    async Task ShowCurrentAsync()
    {
        var has = Zones.TryGetSelectedKey(out var id);
        await Detail.ShowAsync(has, id);
    }

    async Task RefreshAsync(Guid? select = null)
    {
        await LoadAsync();
        if (select is { } id && Zones.List.Rows.FirstOrDefault(z => z.Id == id) is { } row)
        {
            Zones.Select(row);
        }
        else if (Zones.TryGetSelectedKey(out _))
        {
            await ShowCurrentAsync();
        }

        await AfterChange();
        await _context.AfterWrite();
    }

    IReadOnlyList<AppAction> BuildActions(ZoneRow zone)
    {
        var actions = new ActionSet(_context.Localizer);
        actions.Add("Rename", "Zones.Action.Rename", () => _ = RenameAsync(zone));
        if (zone.IsActive)
        {
            actions.Add("Deactivate", "Zones.Action.Deactivate", () => _ = RunAsync(ct => _services.DeactivateZone(zone.Id, ct), "DeactivateZone", zone.Id), () => zone.DeactivationBlocked);
        }
        else
        {
            actions.Add("Reactivate", "Zones.Action.Reactivate", () => _ = RunAsync(ct => _services.ReactivateZone(zone.Id, ct), "ReactivateZone", zone.Id), () => zone.ReactivationBlocked);
        }

        actions.Add("Delete", "Zones.Action.Delete", () => _ = DeleteAsync(zone), () => zone.DeletionBlocked);
        return actions.Actions;
    }

    Task RunAsync(Func<CancellationToken, Task<Result<string>>> operation, string name, Guid? select) =>
        _once.RunAsync(name, () => new RunOnceCommand<string>((ct, _) => operation(ct), sentence => sentence, name, _context.Notifications, _context.Localizer, _context.Log, _context.Delay, () => RefreshAsync(select)).RunAsync());

    async Task DeleteAsync(ZoneRow zone)
    {
        if (await _context.Confirmations.ConfirmAsync(new ZoneConfirmations(_context.Localizer).ForDelete(zone.Name)))
        {
            await RunAsync(ct => _services.DeleteZone(zone.Id, ct), "DeleteZone", null);
        }
    }

    static string? FieldOf(Error error) => error.Code is "Zones.NameRequired" or "Zones.NameTooLong" or "Zones.NameDuplicate" ? "Name" : null;

    /// <summary>Opens the form of a new zone; a name that another zone has, without telling case or accents apart, is marked in the field.</summary>
    public async Task NewZoneAsync()
    {
        var text = _context.Localizer;
        var name = new FormFieldModel("Name", text.Get("Zones.Label.Name"));
        FormViewModel<string>? form = null;
        form = new FormViewModel<string>(
            [name], ct => _services.CreateZone(name.Text, ct), FieldOf, sentence => sentence, "CreateZone", _context.Notifications, text,
            _context.Log, _context.Delay, text.Get("Zones.Action.New"), text.Get("Common.Action.Save"), null, () => RefreshAsync());
        await _context.Forms.ShowAsync(form);
    }

    async Task RenameAsync(ZoneRow zone)
    {
        var text = _context.Localizer;
        var name = new FormFieldModel("Name", text.Get("Zones.Label.Name")) { Text = zone.Name };
        var form = new FormViewModel<string>(
            [name], ct => _services.RenameZone(zone.Id, name.Text, ct), FieldOf, sentence => sentence, "RenameZone", _context.Notifications, text,
            _context.Log, _context.Delay, text.Get("Zones.Action.Rename"), text.Get("Common.Action.Save"), null, () => RefreshAsync(zone.Id));
        await _context.Forms.ShowAsync(form);
    }
}
