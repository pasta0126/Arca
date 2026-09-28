// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Charges;

namespace Arca.Application.Charges;

/// <summary>Where charges are kept. Loads are explicit and complete: there is no lazy loading.</summary>
public interface IChargeRepository
{
    Task<Charge?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Every charge of a student, of any concept and year.</summary>
    Task<IReadOnlyList<Charge>> ListByStudentAsync(Guid studentId, CancellationToken ct);

    /// <summary>
    /// Every pending charge of any student, concept and year: what the debtors query is built from.
    /// <para>
    /// Hook point for <c>cursos-i-historial</c>: reviewing the debt when a year is closed reads this (or, per student,
    /// <c>PaymentStanding.Of</c>) and needs nothing else here. Carrying the debt over is doing nothing (it stays pending and
    /// warns when a locker is assigned); waiving it is <c>WaiveChargeHandler</c> or <c>WaiveChargesInBulkHandler</c>. Closing
    /// a year never touches the deposits, only the student leaving does.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Charge>> ListPendingAsync(CancellationToken ct);

    /// <summary>Every deposit that is due back, of any student.</summary>
    Task<IReadOnlyList<Charge>> ListDepositsDueBackAsync(CancellationToken ct);

    Task AddAsync(Charge charge, CancellationToken ct);

    /// <summary>Saves the changes made to a charge that was loaded from here.</summary>
    Task UpdateAsync(Charge charge, CancellationToken ct);
}
