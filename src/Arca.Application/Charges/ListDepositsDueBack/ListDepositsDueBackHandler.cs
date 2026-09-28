// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Students;
using Arca.Domain.Common;

namespace Arca.Application.Charges.ListDepositsDueBack;

/// <param name="Text">Optional words to find in the student's name.</param>
public sealed record ListDepositsDueBackRequest(string? Text = null);

/// <summary>A deposit due back. No email, no identifier and no notes: the student's record and the charge hold those.</summary>
public sealed record DepositDueRow(Guid ChargeId, Guid StudentId, string FirstName, string LastName, decimal Amount, DateOnly? PaidOn, DateTimeOffset? RetiredAtUtc);

/// <param name="Rows">The deposits due back, the earliest departure first.</param>
/// <param name="Count">How many.</param>
/// <param name="TotalAmount">The total to give back.</param>
/// <param name="IsEmpty">True when nothing is due back at all, so the screen explains it instead of showing an empty list.</param>
public sealed record DepositsDueBackListing(IReadOnlyList<DepositDueRow> Rows, int Count, decimal TotalAmount, bool IsEmpty);

/// <summary>
/// The deposits due back, ordered by the date the student left, with the total (pagaments, D8). Exempt and waived deposits
/// are never due back, so they are not here.
/// </summary>
public sealed class ListDepositsDueBackHandler(IChargeRepository charges, IStudentRepository students)
{
    public async Task<Result<DepositsDueBackListing>> HandleAsync(ListDepositsDueBackRequest request, CancellationToken ct)
    {
        var due = await charges.ListDepositsDueBackAsync(ct);
        var byId = (await students.ListAsync(ct)).ToDictionary(s => s.Id);
        var words = TextComparer.Key(request.Text).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        IReadOnlyList<DepositDueRow> rows =
        [
            .. due
                .Where(c => byId.ContainsKey(c.StudentId))
                .Select(c => (Charge: c, Student: byId[c.StudentId]))
                .Where(x => words.All(w => x.Student.NameKey.Contains(w, StringComparison.Ordinal)))
                .OrderBy(x => x.Student.RetiredAtUtc)
                .ThenBy(x => x.Student.LastName, TextComparer.Comparer)
                .ThenBy(x => x.Student.FirstName, TextComparer.Comparer)
                .Select(x => new DepositDueRow(x.Charge.Id, x.Student.Id, x.Student.FirstName, x.Student.LastName, x.Charge.Amount.Amount, x.Charge.PaidOn, x.Student.RetiredAtUtc)),
        ];
        return Result<DepositsDueBackListing>.Success(new DepositsDueBackListing(rows, rows.Count, rows.Sum(r => r.Amount), due.Count == 0));
    }
}
