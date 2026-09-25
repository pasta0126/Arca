// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Students;

/// <summary>
/// The list of students that match the filters, in a stable order and ready to be shown in a virtualised list, with the
/// counters of the whole active year. Its rows carry no email (alumnes-i-assignacions, D11).
/// </summary>
public sealed record StudentListing(IReadOnlyList<StudentRow> Rows, StudentCounters Counters);
