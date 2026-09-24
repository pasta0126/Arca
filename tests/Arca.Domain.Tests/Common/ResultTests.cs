// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Xunit;

namespace Arca.Domain.Tests.Common;

public sealed class ResultTests
{
    static class SampleErrors
    {
        public static Error NumberInUse(int number) => new("Lockers.NumberInUse", Args: [number]);

        public static readonly Error Retired = new("Lockers.Retired");
    }

    [Fact]
    public void Success_carries_value_and_notices()
    {
        var result = Result<int>.Success(40, new Notice("Assignments.PriorDebt", [12]));

        Assert.True(result.IsSuccess);
        Assert.Equal(40, result.Value);
        Assert.Null(result.Error);
        Assert.Equal("Assignments.PriorDebt", Assert.Single(result.Notices).Code);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/internacionalitzacio: Errores de negocio con código estable")]
    public void Failure_carries_a_stable_code_and_arguments_not_text()
    {
        var result = Result<int>.Failure(SampleErrors.NumberInUse(15));

        Assert.False(result.IsSuccess);
        Assert.Equal("Lockers.NumberInUse", result.Error!.Code);
        Assert.Equal(15, Assert.Single(result.Error.Args));
        Assert.Equal(Severity.Error, result.Error.Severity);
    }

    [Fact]
    public void Error_without_arguments_has_an_empty_list()
    {
        Assert.Empty(SampleErrors.Retired.Args);
    }

    [Fact]
    public void Failure_has_no_value_and_no_notices()
    {
        var result = Result<string>.Failure(SampleErrors.Retired);

        Assert.Null(result.Value);
        Assert.Empty(result.Notices);
    }
}
