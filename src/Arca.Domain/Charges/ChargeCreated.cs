// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Domain.Charges;

/// <summary>A charge just generated, and its history event.</summary>
public sealed record ChargeCreated(Charge Charge, HistoryEvent Event);
