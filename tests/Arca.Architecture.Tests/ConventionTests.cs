// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Reflection;
using System.Text.RegularExpressions;
using Arca.Domain.Common;
using Xunit;

namespace Arca.Architecture.Tests;

public sealed class ConventionTests
{
    /// <summary>Public use-case handlers: every HandleAsync must return Task of Result, never throw for business rules.</summary>
    public static IEnumerable<string> HandlerViolations(IEnumerable<Type> types) =>
        types
            .Where(t => (t.IsPublic || t.IsNestedPublic) && t.IsClass && t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == "HandleAsync")
                .Where(m => !IsTaskOfResult(m.ReturnType))
                .Select(m => $"{t.FullName}.{m.Name} returns {m.ReturnType.Name}"));

    static bool IsTaskOfResult(Type type) =>
        type.IsGenericType
        && type.GetGenericTypeDefinition() == typeof(Task<>)
        && type.GetGenericArguments()[0] is { IsGenericType: true } inner
        && inner.GetGenericTypeDefinition() == typeof(Result<>);

    [Fact]
    [Trait("spec", "arquitectura-base/design: D12 Contrato de resultado, progreso y cancelación en Application")]
    public void Application_use_cases_return_a_result_instead_of_throwing()
    {
        Assert.Empty(HandlerViolations(typeof(Application.AssemblyMarker).Assembly.GetExportedTypes()));
    }

    public sealed class GoodHandler
    {
        public Task<Result<int>> HandleAsync(CancellationToken ct) => Task.FromResult(Result<int>.Success(1));
    }

    public sealed class BadHandler
    {
        public Task<int> HandleAsync(CancellationToken ct) => Task.FromResult(1);
    }

    public sealed class VoidHandler
    {
        public Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public void The_handler_rule_accepts_results_and_rejects_anything_else()
    {
        Assert.Empty(HandlerViolations([typeof(GoodHandler)]));
        Assert.Single(HandlerViolations([typeof(BadHandler)]));
        Assert.Single(HandlerViolations([typeof(VoidHandler)]));
    }

    static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }

        return dir!.FullName;
    }

    [Fact]
    [Trait("spec", "arquitectura-base/design: D15 Carga bajo demanda, sin carga perezosa implícita")]
    public void No_lazy_loading_proxies_are_referenced_or_enabled()
    {
        var src = Path.Combine(Root(), "src");
        var projects = Directory.EnumerateFiles(src, "*.csproj", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("EntityFrameworkCore.Proxies", StringComparison.Ordinal));
        var sources = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), "UseLazyLoadingProxies|ILazyLoader"));

        Assert.Empty(projects);
        Assert.Empty(sources);
    }
}
