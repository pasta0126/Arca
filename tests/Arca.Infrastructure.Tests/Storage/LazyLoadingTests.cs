// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Infrastructure.Storage;
using Arca.Testing;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

public sealed class LazyLoadingTests
{
    [Fact]
    [Trait("spec", "arquitectura-base/design: D15 Carga bajo demanda, sin carga perezosa implícita")]
    public void Lazy_loading_is_off_so_relations_are_loaded_explicitly()
    {
        using var key = TestKeys.FromSeed("lazy");
        using var context = new ArcaDbContext(Path.Combine(Path.GetTempPath(), "never-opened.db"), key);

        Assert.False(context.ChangeTracker.LazyLoadingEnabled);
    }
}
