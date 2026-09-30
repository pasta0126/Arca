// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Home;
using Arca.Application.Localization;
using Arca.Application.Search;
using Arca.Domain.Common;
using Arca.UI.Screens;
using Arca.UI.Shell;

namespace Arca.UI.Home;

/// <summary>
/// The forms of a card (targetes-d-inici, Crear una tarjeta desde una pantalla filtrada y desde Inicio): the short one that only asks for a
/// title when the filter comes from a screen that is already filtered, and the full one of the start screen, to create or edit a card, with
/// the screen and the filters as drop-down lists and the count the filter would give before saving it.
/// </summary>
public static class HomeCardForms
{
    /// <summary>A title made of what the card is about, cut to the longest a title can be.</summary>
    public static string SuggestedTitle(string section, string summary)
    {
        var title = summary.Length == 0 ? section : section + ": " + summary;
        return title.Length <= HomeCardLimits.MaximumTitleLength ? title : title[..(HomeCardLimits.MaximumTitleLength - 1)] + "…";
    }

    /// <summary>The form that only asks for the title, for the criteria of the screen it was opened from.</summary>
    public static FormViewModel<HomeCardSaved> FromScreen(
        ScreenContext context, HomeCardServices services, HomeCardTargetView target, IReadOnlyDictionary<string, string> criteria, string summary, string suggestedTitle)
    {
        var text = context.Localizer;
        var title = new FormFieldModel("Title", text.Get("Shell.Home.Form.FieldTitle")) { Text = suggestedTitle };
        var form = Build(context, [title], ct => services.Create(new CreateHomeCardRequest(title.Text, target, criteria), ct), "CreateHomeCard",
            saved => text.Get("Shell.Home.Notify.Created", saved.Title), text.Get("Shell.Home.Form.NewTitle"), null);
        form.Note = text.Get("Shell.Home.Form.NoteSummary", summary) + "\n" + text.Get("Shell.Home.Form.NoteNames");
        return form;
    }

    /// <summary>The full form of the start screen: a new card when <paramref name="existing"/> is null, or the edition of that card.</summary>
    public static FormViewModel<HomeCardSaved> Full(ScreenContext context, HomeCardServices services, CardOptions options, HomeCardView? existing, Func<Task>? afterSuccess)
    {
        var text = context.Localizer;
        FormOption Any() => new(string.Empty, text.Get("Shell.Home.Form.Any"));
        var criteria = existing?.Criteria ?? new Dictionary<string, string>();
        string Given(string name) => criteria.TryGetValue(name, out var value) ? value : string.Empty;

        var title = new FormFieldModel("Title", text.Get("Shell.Home.Form.FieldTitle")) { Text = existing?.Title ?? string.Empty };
        var target = new FormFieldModel("Target", text.Get("Shell.Home.Form.FieldTarget"),
            [
                new(nameof(HomeCardTargetView.LockerMap), text.Get("Shell.Home.Form.TargetMap")),
                new(nameof(HomeCardTargetView.Lockers), text.Get("Shell.Home.Form.TargetLockers")),
                new(nameof(HomeCardTargetView.Students), text.Get("Shell.Home.Form.TargetStudents")),
            ]) { Text = (existing?.Target ?? HomeCardTargetView.LockerMap).ToString() };
        var status = new FormFieldModel("Status", text.Get("Shell.Home.Form.FieldStatus"),
            [Any(), .. new[] { LockerStatusView.Free, LockerStatusView.Occupied, LockerStatusView.Reserved, LockerStatusView.Broken, LockerStatusView.Maintenance }
                .Select(s => new FormOption(s.ToString(), LockerStatusPresentation.Text(s, text)))]) { Text = Given("Status") };
        var zone = new FormFieldModel("Zone", text.Get("Shell.Home.Form.FieldZone"), [Any(), .. options.Zones.Select(z => new FormOption(z.Id.ToString(), z.Name))]) { Text = Given("Zone") };
        var locker = new FormFieldModel("Locker", text.Get("Shell.Home.Form.FieldLocker"),
            [Any(), new("with", text.Get("Shell.Home.Form.LockerWith")), new("without", text.Get("Shell.Home.Form.LockerWithout"))]) { Text = Given("Locker") };
        var payment = new FormFieldModel("Payment", text.Get("Shell.Home.Form.FieldPayment"),
            [Any(), new("pending", text.Get("Students.Label.WithPendingPayments")), new("upToDate", text.Get("Students.Label.PaymentsUpToDate"))]) { Text = Given("Payment") };
        var level = new FormFieldModel("Level", text.Get("Shell.Home.Form.FieldLevel"), [Any(), .. options.Levels.Select(l => new FormOption(l, l))]) { Text = Given("Level") };
        var group = new FormFieldModel("Group", text.Get("Shell.Home.Form.FieldGroup"), [Any(), .. options.Groups.Select(g => new FormOption(g, g))]) { Text = Given("Group") };
        var retired = new FormFieldModel("IncludeRetired", text.Get("Shell.Home.Form.FieldRetired"),
            [new(string.Empty, text.Get("Shell.Home.Form.RetiredNo")), new("true", text.Get("Shell.Home.Form.RetiredYes"))]) { Text = Given("IncludeRetired") };

        (HomeCardTargetView Target, Dictionary<string, string> Criteria) Read()
        {
            var chosen = Enum.TryParse<HomeCardTargetView>(target.Text, out var parsed) ? parsed : HomeCardTargetView.LockerMap;
            var fields = chosen == HomeCardTargetView.Students ? new[] { locker, payment, level, group, retired } : [status, zone];
            return (chosen, fields.Where(f => f.Text.Length > 0).ToDictionary(f => f.Id, f => f.Text));
        }

        var form = Build(
            context, [title, target, status, zone, locker, payment, level, group, retired],
            ct =>
            {
                var (chosen, read) = Read();
                return existing is { } card
                    ? services.Edit(new EditHomeCardRequest(card.Id, title.Text, chosen, read), ct)
                    : services.Create(new CreateHomeCardRequest(title.Text, chosen, read), ct);
            },
            existing is null ? "CreateHomeCard" : "EditHomeCard",
            saved => text.Get(existing is null ? "Shell.Home.Notify.Created" : "Shell.Home.Notify.Edited", saved.Title),
            text.Get(existing is null ? "Shell.Home.Form.NewTitle" : "Shell.Home.Form.EditTitle"), afterSuccess);
        var guidance = text.Get("Shell.Home.Form.NoteOnlyScreen") + "\n" + text.Get("Shell.Home.Form.NoteNames");
        form.Note = guidance;

        // The count the filter gives, as the person changes it: only the answer to the latest change is shown.
        var request = 0;
        async void Preview()
        {
            var mine = ++request;
            var (chosen, read) = Read();
            try
            {
                var counted = await services.Preview(new PreviewHomeCardRequest(chosen, read), default);
                if (mine != request)
                {
                    return;
                }

                form.Note = guidance + "\n" + (counted.IsSuccess
                    ? counted.Value is { } count ? text.Get("Shell.Home.Form.NotePreview", count, text.Get(chosen == HomeCardTargetView.Students ? "Shell.Home.Form.NounStudents" : "Shell.Home.Form.NounLockers")) : text.Get("Shell.Home.Form.NotePreviewNoYear")
                    : string.Empty);
            }
            catch (Exception)
            {
                if (mine == request)
                {
                    form.Note = guidance; // the preview is a help: if it fails the form still works
                }
            }
        }

        foreach (var field in new[] { target, status, zone, locker, payment, level, group, retired })
        {
            field.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FormFieldModel.Text))
                {
                    Preview();
                }
            };
        }

        Preview();
        return form;
    }

    static FormViewModel<HomeCardSaved> Build(
        ScreenContext context, IEnumerable<FormFieldModel> fields, Func<CancellationToken, Task<Result<HomeCardSaved>>> save, string name,
        Func<HomeCardSaved, string> successText, string title, Func<Task>? afterSuccess) => new(
        fields, save, FieldOf, successText, name, context.Notifications, context.Localizer, context.Log, context.Delay, title,
        context.Localizer.Get("Shell.Home.Form.SaveLabel"), null, afterSuccess);

    static string? FieldOf(Error error) => error.Code switch
    {
        "HomeCards.TitleRequired" or "HomeCards.TitleTooLong" => "Title",
        _ => null,
    };
}
