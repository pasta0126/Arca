// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Startup;
using Arca.Domain.Common;
using Xunit;

namespace Arca.Application.Tests;

public sealed class StartupSequenceTests
{
    sealed class Collect : IProgress<StartupProgress>
    {
        public List<StartupProgress> Reports { get; } = [];

        public void Report(StartupProgress value) => Reports.Add(value);
    }

    static StartupStage Ok(string key, List<string> ran) => new(key, _ =>
    {
        ran.Add(key);
        return Task.FromResult<Error?>(null);
    });

    static StartupStage Fails(string key, Error error, List<string> ran) => new(key, _ =>
    {
        ran.Add(key);
        return Task.FromResult<Error?>(error);
    });

    [Fact]
    [Trait("spec", "arquitectura-base/feedback-operacions: Pantalla de arranque con etapas reales (arranque normal)")]
    public async Task Stages_run_in_order_and_each_is_reported_before_it_starts()
    {
        var ran = new List<string>();
        var progress = new Collect();

        var error = await new StartupSequence([Ok("A", ran), Ok("B", ran), Ok("C", ran)]).RunAsync(progress);

        Assert.Null(error);
        Assert.Equal(["A", "B", "C"], ran);
        Assert.Equal(["A", "B", "C"], progress.Reports.Select(r => r.TextKey));
        Assert.Equal([1, 2, 3], progress.Reports.Select(r => r.Index));
        Assert.All(progress.Reports, r => Assert.Equal(3, r.Total));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/feedback-operacions: Pantalla de arranque con etapas reales (fallo de arranque)")]
    public async Task A_failure_stops_the_sequence_and_returns_its_error()
    {
        var ran = new List<string>();
        var problem = new Error("Storage.Unreadable");

        var error = await new StartupSequence([Ok("A", ran), Fails("B", problem, ran), Ok("C", ran)]).RunAsync();

        Assert.Same(problem, error);
        Assert.Equal(["A", "B"], ran); // C never ran
    }

    [Fact]
    public async Task An_empty_sequence_succeeds()
    {
        Assert.Null(await new StartupSequence([]).RunAsync());
    }

    [Fact]
    public async Task Cancellation_stops_before_the_next_stage()
    {
        var ran = new List<string>();
        using var cts = new CancellationTokenSource();
        var first = new StartupStage("A", _ =>
        {
            ran.Add("A");
            cts.Cancel();
            return Task.FromResult<Error?>(null);
        });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new StartupSequence([first, Ok("B", ran)]).RunAsync(ct: cts.Token));

        Assert.Equal(["A"], ran);
    }

    [Fact]
    public async Task An_unexpected_exception_is_not_swallowed()
    {
        var boom = new StartupStage("A", _ => throw new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => new StartupSequence([boom]).RunAsync());
    }
}
