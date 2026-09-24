// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Startup;

/// <summary>
/// Runs the stages in order, reports each one before it starts, and stops at the first failure with its error
/// (arquitectura-base, D13). An unexpected exception is not caught here: the caller turns it into a logged error.
/// </summary>
public sealed class StartupSequence(IReadOnlyList<StartupStage> stages)
{
    /// <returns>Null when every stage succeeded; otherwise the error of the stage that failed.</returns>
    public async Task<Error?> RunAsync(IProgress<StartupProgress>? progress = null, CancellationToken ct = default)
    {
        for (var i = 0; i < stages.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new StartupProgress(stages[i].TextKey, i + 1, stages.Count));
            var error = await stages[i].Run(ct);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }
}
