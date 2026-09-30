// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Home;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Notifications;
using Arca.UI.Screens;
using Arca.UI.Shell;

namespace Arca.UI.Home;

/// <summary>A card of the start screen with what it does: open its screen filtered, move earlier or later, and delete.</summary>
/// <param name="View">The card as it is read: title, count and state.</param>
/// <param name="Open">Opens the screen of the card with its filter on.</param>
/// <param name="MoveEarlier">Moves the card one place earlier; unavailable for the first, with its reason.</param>
/// <param name="MoveLater">Moves the card one place later; unavailable for the last, with its reason.</param>
/// <param name="Delete">Deletes the card after asking.</param>
/// <param name="CountText">The count in words or a dash when there is nothing to count.</param>
/// <param name="StateText">What is wrong with the card, in words, or empty.</param>
public sealed record StartHomeCard(HomeCardView View, AppAction Open, AppAction MoveEarlier, AppAction MoveLater, AppAction Delete, string CountText, string StateText);

/// <summary>
/// The start screen as a panel of cards (targetes-d-inici, Inicio como panel de tarjetas): the active year and the cards of the centre in
/// their order, each with its count and the operations on it. Counts only, never an amount. Without an active year it says so and offers the
/// Course section; in a centre with no lockers and no students it says what to set up first. Nothing is read on a timer.
/// </summary>
public sealed class StartHomeModel : ObservableObject
{
    readonly HomeCardServices _services;
    readonly ScreenFilterRouter _router;
    readonly Func<string, bool> _navigate;
    readonly ScreenContext _context;
    readonly ResultNotifier _notifier;
    readonly OneAtATime _once = new();
    IReadOnlyList<StartHomeCard> _cards = [];
    HomeCardsView? _view;
    bool _defaultsEnsured;

    public StartHomeModel(HomeCardServices services, ScreenFilterRouter router, Func<string, bool> navigate, ScreenContext context)
    {
        _services = services;
        _router = router;
        _navigate = navigate;
        _context = context;
        _notifier = new ResultNotifier(context.Notifications, context.Localizer, context.Log);
        var text = context.Localizer;
        GoToCourse = new AppAction("GoToCourse", text.Get("Shell.Home.Action.GoToCourse"));
        GoToCourse.Attach(() => _navigate(ShellCatalog.Course));
        SetUpLockers = new AppAction("SetUpLockers", text.Get("Shell.Home.Action.SetUpLockers"));
        SetUpLockers.Attach(() => _router.Open(new ScreenFilterRequest(ShellCatalog.Lockers, "Zones", new Dictionary<string, string>())));
        AddStudents = new AppAction("AddStudents", text.Get("Shell.Home.Action.AddStudents"));
        AddStudents.Attach(() => _router.Open(new ScreenFilterRequest(ShellCatalog.Students, null, new Dictionary<string, string>())));
        RestoreDefaults = new AppAction("RestoreDefaults", text.Get("Shell.Home.Action.RestoreDefaults"));
        RestoreDefaults.Attach(() => _ = RestoreAsync());
    }

    /// <summary>True until the first read has answered: the cards are drawn with a loading indicator and not as empty.</summary>
    public bool IsLoading => _view is null;

    public IReadOnlyList<StartHomeCard> Cards => _cards;

    public string YearText => _view?.ActiveYearName is { } year ? _context.Localizer.Get("Shell.Home.Label.Year", year) : string.Empty;

    /// <summary>There is no active year: nothing can be assigned, and the cards of students have no count.</summary>
    public bool NoActiveYear => _view is { ActiveYearName: null };

    /// <summary>There is an active year but no locker and no student yet: the centre is not set up.</summary>
    public bool NotSetUp => _view is { ActiveYearName: not null, HasLockers: false, HasStudents: false };

    /// <summary>The person deleted every card.</summary>
    public bool NoCards => _view is not null && _cards.Count == 0;

    public AppAction GoToCourse { get; }

    public AppAction SetUpLockers { get; }

    public AppAction AddStudents { get; }

    /// <summary>Adds the default cards that are missing, leaving the others as they are.</summary>
    public AppAction RestoreDefaults { get; }

    /// <summary>Reads the cards and their counts. Called when the screen opens and whenever the state of the application changes.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_defaultsEnsured)
            {
                var ensured = await _services.EnsureDefaults(ct);
                if (!ensured.IsSuccess)
                {
                    _notifier.Error(ensured.Error!);
                    return;
                }

                _defaultsEnsured = true;
            }

            var result = await _services.Load(ct);
            if (!result.IsSuccess)
            {
                _notifier.Error(result.Error!);
                return;
            }

            _view = result.Value;
            _cards = [.. result.Value!.Cards.Select(Build)];
            Raise(nameof(IsLoading));
            Raise(nameof(Cards));
            Raise(nameof(YearText));
            Raise(nameof(NoActiveYear));
            Raise(nameof(NotSetUp));
            Raise(nameof(NoCards));
        }
        catch (OperationCanceledException)
        {
            // Closed while loading.
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, "LoadStartCards");
        }
    }

    StartHomeCard Build(HomeCardView view)
    {
        var text = _context.Localizer;
        var open = new AppAction("OpenCard", text.Get("Shell.Home.Card.Open", view.Title));
        open.Attach(() => _ = OpenAsync(view));
        var earlier = new AppAction("MoveCardEarlier", text.Get("Shell.Home.Card.MoveEarlier", view.Title));
        earlier.Attach(() => _ = MoveAsync(view, HomeCardMove.Earlier), () => view.IsFirst ? Availability.Unavailable(text.Get("Shell.Home.Reason.First")) : Availability.Available);
        var later = new AppAction("MoveCardLater", text.Get("Shell.Home.Card.MoveLater", view.Title));
        later.Attach(() => _ = MoveAsync(view, HomeCardMove.Later), () => view.IsLast ? Availability.Unavailable(text.Get("Shell.Home.Reason.Last")) : Availability.Available);
        var delete = new AppAction("DeleteCard", text.Get("Shell.Home.Card.Delete", view.Title));
        delete.Attach(() => _ = DeleteAsync(view));
        var countText = view.Count is { } count ? count.ToString(System.Globalization.CultureInfo.CurrentCulture) : "—";
        return new StartHomeCard(view, open, earlier, later, delete, countText, StateTextOf(view));
    }

    /// <summary>The name of a criterion in words, each from its own key.</summary>
    string CriterionText(string name) => name switch
    {
        "Zone" => _context.Localizer.Get("Shell.Home.Criterion.Zone"),
        "Level" => _context.Localizer.Get("Shell.Home.Criterion.Level"),
        "Group" => _context.Localizer.Get("Shell.Home.Criterion.Group"),
        _ => name,
    };

    string StateTextOf(HomeCardView view)
    {
        var text = _context.Localizer;
        return view.State switch
        {
            HomeCardState.Obsolete => text.Get("Shell.Home.Card.Obsolete", string.Join(", ", view.Ignored.Select(CriterionText))),
            HomeCardState.NoCount => text.Get("Shell.Home.Card.NoCount"),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Opens the screen of a card with its filter on. A criterion that no longer exists is left out and said, so the person knows why the
    /// screen shows more than the card describes.
    /// </summary>
    async Task OpenAsync(HomeCardView view)
    {
        try
        {
            var resolved = await _services.Resolve(view.Id, default);
            if (!resolved.IsSuccess)
            {
                _notifier.Error(resolved.Error!);
                await LoadAsync();
                return;
            }

            var card = resolved.Value!;
            if (card.Ignored.Count > 0)
            {
                var text = _context.Localizer;
                _context.Notifications.Publish(
                    NotificationKind.Warning, text.Get("Shell.Home.Warning.Ignored", card.Title, string.Join(", ", card.Ignored.Select(CriterionText))));
            }

            var (section, screen) = card.Target switch
            {
                HomeCardTargetView.Students => (ShellCatalog.Students, (string?)null),
                HomeCardTargetView.Lockers => (ShellCatalog.Lockers, "Lockers"),
                _ => (ShellCatalog.Lockers, "LockerMap"),
            };
            _router.Open(new ScreenFilterRequest(section, screen, card.Criteria));
        }
        catch (Exception e)
        {
            _notifier.Unexpected(e, "OpenCard");
        }
    }

    Task MoveAsync(HomeCardView view, HomeCardMove move) => _once.RunAsync("MoveCard", () => new RunOnceCommand<bool>(
        (ct, _) => _services.Move(new MoveHomeCardRequest(view.Id, move), ct), _ => _context.Localizer.Get("Shell.Home.Notify.Moved", view.Title), "MoveHomeCard",
        _context.Notifications, _context.Localizer, _context.Log, _context.Delay, () => LoadAsync()).RunAsync());

    async Task DeleteAsync(HomeCardView view)
    {
        var text = _context.Localizer;
        var request = new ConfirmationRequest(
            text.Get("Shell.Home.Confirm.DeleteTitle", view.Title), text.Get("Shell.Home.Confirm.DeleteConsequence"), text.Get("Shell.Home.Confirm.DeleteConfirm"), Destructive: true);
        if (!await _context.Confirmations.ConfirmAsync(request))
        {
            return;
        }

        await _once.RunAsync("DeleteCard", () => new RunOnceCommand<string>(
            (ct, _) => _services.Delete(new DeleteHomeCardRequest(view.Id), ct), title => _context.Localizer.Get("Shell.Home.Notify.Deleted", title), "DeleteHomeCard",
            _context.Notifications, _context.Localizer, _context.Log, _context.Delay, () => LoadAsync()).RunAsync());
    }

    Task RestoreAsync() => _once.RunAsync("RestoreCards", () => new RunOnceCommand<int>(
        (ct, _) => _services.RestoreDefaults(ct),
        added => _context.Localizer.Get(added == 0 ? "Shell.Home.Notify.NothingToRestore" : "Shell.Home.Notify.Restored", added), "RestoreHomeCards",
        _context.Notifications, _context.Localizer, _context.Log, _context.Delay, () => LoadAsync()).RunAsync());
}
