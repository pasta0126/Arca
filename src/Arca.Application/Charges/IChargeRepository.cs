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

    Task AddAsync(Charge charge, CancellationToken ct);

    /// <summary>Saves the changes made to a charge that was loaded from here.</summary>
    Task UpdateAsync(Charge charge, CancellationToken ct);
}
