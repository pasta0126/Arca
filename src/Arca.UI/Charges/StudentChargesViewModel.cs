// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Charges;
using Arca.Application.Charges.GetStudentChargesScreen;
using Arca.Application.Common;
using Arca.Application.ConceptAmounts;
using Arca.Application.Feedback;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Screens;

namespace Arca.UI.Charges;

/// <summary>
/// The charges of one student (pantalles-cobraments), used as the Cobros tab of the record of a student and as the view of the Payments
/// section, from this one model so they never differ (pantalles-de-domini, D7): whether the student is up to date or how much they owe,
/// every charge of any year, the operations of the one chosen, each disabled with its reason when it does not apply, its history, and
/// the key replacement. No rule of its own: what can be done, and why not, is what Application answers.
/// </summary>
public sealed class StudentChargesViewModel : ObservableObject
{
    readonly ChargeServices _services;
    readonly ScreenContext _context;
    readonly Func<Task> _openCourse;
    readonly ChargeResultTexts _texts;
    readonly OneAtATime _once = new();
    StudentChargesScreen? _screen;
    Guid? _studentId;
    int _request;

    public StudentChargesViewModel(ChargeServices services, ScreenContext context, Func<Task> openCourse)
    {
        _services = services;
        _context = context;
        _openCourse = openCourse;
        _texts = new ChargeResultTexts(context.Localizer);
        var text = context.Localizer;
        Charges = new ScreenListViewModel<ChargeLine, Guid>(
            [
                new ListColumn<ChargeLine>("concept", text.Get("Charges.Label.Concept"), c => ConceptNames.Of(text, c.Concept), Width: 3),
                new ListColumn<ChargeLine>("year", text.Get("Charges.Label.Year"), c => c.YearName, Width: 2),
                new ListColumn<ChargeLine>("amount", text.Get("Charges.Label.Amount"), c => text.Format(Money.FromCents((long)Math.Round(c.Amount * 100))), c => c.Amount, Width: 2),
                new ListColumn<ChargeLine>("status", text.Get("Charges.Label.Status"), c => _texts.StatusName(c.Status), Width: 2),
                new ListColumn<ChargeLine>("date", text.Get("Charges.Label.PaidOn"), c => c.PaidOn is { } d ? text.Format(d) : string.Empty, c => c.PaidOn?.DayNumber ?? 0, Width: 2),
                new ListColumn<ChargeLine>("reason", text.Get("Charges.Label.Reason"), c => c.Reason ?? string.Empty, Width: 3),
            ],
            c => c.Id, LoadLinesAsync, text, context.Notifications, context.Log, () => text.Get("Charges.Empty.NoCharges"));
        Detail = new DetailViewModel<Guid, ChargeLine>(LoadLineAsync, BuildActions, text, context.Notifications, context.Log, services.ChargeHistory);
        Charges.CurrentChanged += (_, _) => _ = ShowCurrentAsync();
        KeyReplacement = new AppAction("KeyReplacement", text.Get("Charges.Action.KeyReplacement"));
        KeyReplacement.Attach(() => _ = ChargeKeyReplacementAsync(), () => _screen?.KeyReplacementBlocked is { } blocked ? Availability.Unavailable(text.Message(blocked)) : Availability.Available);
    }

    public ScreenListViewModel<ChargeLine, Guid> Charges { get; }

    public DetailViewModel<Guid, ChargeLine> Detail { get; }

    /// <summary>"Cobrar reposició de clau": the only action of the student as a whole.</summary>
    public AppAction KeyReplacement { get; }

    /// <summary>The student and their standing, once loaded.</summary>
    public StudentChargesScreen? Screen
    {
        get => _screen;
        private set
        {
            if (Set(ref _screen, value))
            {
                Raise(nameof(Summary));
                KeyReplacement.Refresh();
            }
        }
    }

    /// <summary>The operations of a charge, each disabled with its reason when it does not apply. A row of the record builds its buttons from them.</summary>
    public IReadOnlyList<AppAction> ActionsFor(ChargeLine line) => BuildActions(line);

    public string ConceptText(ChargeLine line) => ConceptNames.Of(_context.Localizer, line.Concept);

    public string StatusText(ChargeLine line) => _texts.StatusName(line.Status);

    public string AmountText(ChargeLine line) => _context.Localizer.Format(Money.FromCents((long)Math.Round(line.Amount * 100)));

    /// <summary>Chooses a charge and loads its read-only history, which the row shows under itself.</summary>
    public async Task ShowHistoryAsync(ChargeLine line)
    {
        await Detail.ShowAsync(true, line.Id);
        await Detail.LoadHistoryAsync();
    }

    /// <summary>Raised after an operation changed a charge, so whoever shows the state of the student reads it again.</summary>
    public event EventHandler? Changed;

    /// <summary>"Al corrent" or how much the student owes, in words.</summary>
    public string Summary => Screen is not { } s ? string.Empty
        : s.UpToDate ? _context.Localizer.Get(s.ByExemption ? "Charges.Label.SummaryExempt" : "Charges.Label.SummaryUpToDate")
        : _context.Localizer.Get("Charges.Label.SummaryDebt", _context.Localizer.Format(Money.FromCents((long)Math.Round(s.PendingTotal * 100))));

    /// <summary>Shows the charges of a student, or nothing with null. Called when one is chosen and again after every change.</summary>
    public async Task ShowAsync(Guid? studentId)
    {
        _studentId = studentId;
        _request++; // whatever was loading for the previous student is no longer wanted
        Detail.Clear();
        if (studentId is null)
        {
            Screen = null;
            Charges.List.SetItems([]);
            return;
        }

        await Charges.LoadAsync();
    }

    async Task<Result<IReadOnlyList<ChargeLine>>> LoadLinesAsync(CancellationToken ct)
    {
        var mine = ++_request;
        if (_studentId is not { } id)
        {
            return Result<IReadOnlyList<ChargeLine>>.Success([]);
        }

        var screen = await _services.StudentCharges(id, ct);
        if (mine != _request || _studentId != id)
        {
            return Result<IReadOnlyList<ChargeLine>>.Success([]);
        }

        if (!screen.IsSuccess)
        {
            return Result<IReadOnlyList<ChargeLine>>.Failure(screen.Error!);
        }

        Screen = screen.Value;
        return Result<IReadOnlyList<ChargeLine>>.Success(screen.Value!.Lines);
    }

    Task<Result<ChargeLine?>> LoadLineAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Result<ChargeLine?>.Success(Screen?.Lines.FirstOrDefault(l => l.Id == id)));

    async Task ShowCurrentAsync()
    {
        var has = Charges.TryGetSelectedKey(out var id);
        await Detail.ShowAsync(has, id);
    }

    async Task RefreshAsync(Guid? select = null)
    {
        await Charges.LoadAsync();
        if (select is { } id && Charges.List.Rows.FirstOrDefault(l => l.Id == id) is { } row)
        {
            Charges.Select(row);
        }

        if (Charges.TryGetSelectedKey(out _))
        {
            await ShowCurrentAsync();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        await _context.AfterWrite();
    }

    // --- Actions of a charge ---

    IReadOnlyList<AppAction> BuildActions(ChargeLine line)
    {
        var actions = new ActionSet(_context.Localizer);
        actions.Add("Pay", "Charges.Action.Pay", () => _ = PayAsync(line), () => line.PayBlocked);
        actions.Add("Exempt", "Charges.Action.Exempt", () => _ = ReasonAsync(line, "Charges.Action.Exempt", (id, reason, ct) => _services.Exempt(id, reason, ct), "ExemptCharge", null), () => line.ExemptBlocked);
        actions.Add("Waive", "Charges.Action.Waive", () => _ = ReasonAsync(line, "Charges.Action.Waive", (id, reason, ct) => _services.Waive(id, reason, ct), "WaiveCharge", null), () => line.WaiveBlocked);
        actions.Add("Adjust", "Charges.Action.Adjust", () => _ = AdjustAsync(line), () => line.AdjustBlocked);
        actions.Add("Revert", "Charges.Action.Revert", () => _ = ReasonAsync(line, "Charges.Action.Revert", (id, reason, ct) => _services.Revert(id, reason, ct), "RevertCharge",
            new ChargeConfirmations(_context.Localizer).ForRevert(line.Concept, Screen?.StudentName ?? string.Empty, line.Status, line.Amount)), () => line.RevertBlocked);
        actions.Add("Void", "Charges.Action.Void", () => _ = ReasonAsync(line, "Charges.Action.Void", (id, reason, ct) => _services.Void(id, reason, ct), "VoidCharge",
            new ChargeConfirmations(_context.Localizer).ForVoid(line.Concept, Screen?.StudentName ?? string.Empty, line.Amount)), () => line.VoidBlocked);
        return actions.Actions;
    }

    static string? FieldOf(Error error) => error.Code switch
    {
        "Charges.DateInvalid" or "Common.DateInvalid" => "Date",
        "Charges.ReasonRequired" or "Charges.ReasonTooLong" or "Charges.NoteTooLong" => "Reason",
        "Charges.AmountInvalid" or "Common.NumberInvalid" => "Amount",
        _ => null,
    };

    /// <summary>Opens the form to mark a charge as paid, with today as the date; a future date is marked in its field.</summary>
    async Task PayAsync(ChargeLine line)
    {
        var text = _context.Localizer;
        var date = new FormFieldModel("Date", text.Get("Charges.Label.PaidOn")) { Text = text.Format(_services.Today()) };
        var form = new FormViewModel<string>(
            [date],
            ct => DateOnly.TryParse(date.Text, text.Culture, DateTimeStyles.None, out var paidOn)
                ? _services.Pay(line.Id, paidOn, ct)
                : Task.FromResult(Result<string>.Failure(FormErrors.DateInvalid(date.Id))),
            FieldOf, sentence => sentence, "MarkChargePaid", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Charges.Action.Pay"), text.Get("Common.Label.Confirm"), null, () => RefreshAsync(line.Id));
        await _context.Forms.ShowAsync(form);
    }

    /// <summary>Opens a form that asks for the reason, which is required, and confirms first when the operation needs it (reverting, voiding).</summary>
    async Task ReasonAsync(
        ChargeLine line, string titleKey, Func<Guid, string?, CancellationToken, Task<Result<string>>> operation, string name, ConfirmationRequest? confirmation)
    {
        var text = _context.Localizer;
        var reason = new FormFieldModel("Reason", text.Get("Charges.Label.Reason"));
        var form = new FormViewModel<string>(
            [reason],
            async ct =>
            {
                if (confirmation is not null && !await _context.Confirmations.ConfirmAsync(confirmation, ct))
                {
                    return Result<string>.Failure(FormViewModel<string>.Cancelled);
                }

                return await operation(line.Id, reason.Text, ct);
            },
            FieldOf, sentence => sentence, name, _context.Notifications, text, _context.Log, _context.Delay,
            text.Get(titleKey), text.Get("Common.Label.Confirm"), null, () => RefreshAsync(line.Id));
        form.Note = text.Get("Charges.Note.ReasonRequired");
        await _context.Forms.ShowAsync(form);
    }

    async Task AdjustAsync(ChargeLine line)
    {
        var text = _context.Localizer;
        var amount = new FormFieldModel("Amount", text.Get("Charges.Label.Amount")) { Text = line.Amount.ToString("0.00", text.Culture) };
        var reason = new FormFieldModel("Reason", text.Get("Charges.Label.Reason"));
        var form = new FormViewModel<string>(
            [amount, reason],
            ct => decimal.TryParse(amount.Text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, text.Culture, out var value)
                ? _services.Adjust(line.Id, value, reason.Text, ct)
                : Task.FromResult(Result<string>.Failure(FormErrors.NumberInvalid(amount.Id))),
            FieldOf, sentence => sentence, "AdjustChargeAmount", _context.Notifications, text, _context.Log, _context.Delay,
            text.Get("Charges.Action.Adjust"), text.Get("Common.Label.Confirm"), null, () => RefreshAsync(line.Id));
        form.Note = text.Get("Charges.Note.ReasonRequired");
        await _context.Forms.ShowAsync(form);
    }

    // --- Key replacement ---

    /// <summary>Charges a key replacement of the active year after confirming. When its amount is not defined, says so and offers to go to the Course section.</summary>
    public async Task ChargeKeyReplacementAsync()
    {
        var text = _context.Localizer;
        if (Screen is not { ActiveYearId: { } yearId } screen)
        {
            return;
        }

        if (screen.KeyReplacementAmount is not { } amount)
        {
            var choice = _context.Choices is null ? null : await _context.Choices.ChooseAsync(new ChoiceRequest(
                text.Get("Charges.Label.KeyNotDefinedTitle"), text.Get("Charges.Empty.AmountsNotDefined"),
                [new("course", text.Get("Students.Action.GoToCourse"))], text.Get("Common.Label.Cancel")));
            if (choice == "course")
            {
                await _openCourse();
            }

            return;
        }

        var request = new ConfirmationRequest(
            text.Get("Charges.Label.KeyTitle", screen.StudentName), text.Get("Charges.Label.KeyConsequence", text.Format(Money.FromCents((long)Math.Round(amount * 100))), screen.ActiveYearName ?? string.Empty),
            text.Get("Charges.Label.KeyConfirm"));
        if (!await _context.Confirmations.ConfirmAsync(request))
        {
            return;
        }

        await _once.RunAsync("KeyReplacement", () => new RunOnceCommand<string>(
            (ct, _) => _services.ChargeKeyReplacement(screen.StudentId, yearId, ct), sentence => sentence, "ChargeKeyReplacement", _context.Notifications, text,
            _context.Log, _context.Delay, () => RefreshAsync()).RunAsync());
    }
}
