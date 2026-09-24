// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Startup;

/// <summary>One named step of starting the application. It returns null on success or the error that stops the start.</summary>
/// <param name="TextKey">Resource key of the text shown while the stage runs.</param>
/// <param name="Run">The work of the stage.</param>
public sealed record StartupStage(string TextKey, Func<CancellationToken, Task<Error?>> Run);

/// <summary>Progress shown on the start-up screen: which stage is running and how far along.</summary>
/// <param name="TextKey">Resource key of the stage text.</param>
/// <param name="Index">Position of the stage, from 1. Sub-steps inside a stage (backing up, migrating) repeat the index of their stage.</param>
/// <param name="Total">Number of stages.</param>
public sealed record StartupProgress(string TextKey, int Index, int Total);
