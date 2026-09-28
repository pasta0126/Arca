// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Xunit;

// The interface tests share one UI thread and one application: running them one after another keeps a control built by
// one test from being updated by the thread of another, which made the whole suite fail now and then.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Arca.UI.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Assembly_loads() => Assert.NotNull(typeof(Arca.UI.AssemblyMarker).Assembly);
}
