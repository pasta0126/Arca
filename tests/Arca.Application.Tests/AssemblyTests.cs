// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Xunit;

namespace Arca.Application.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Assembly_loads() => Assert.NotNull(typeof(Arca.Application.AssemblyMarker).Assembly);
}
