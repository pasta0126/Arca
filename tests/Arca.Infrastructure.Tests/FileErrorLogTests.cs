// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Infrastructure.Common;
using Arca.Infrastructure.Tests.Storage;
using Xunit;

namespace Arca.Infrastructure.Tests;

public sealed class FileErrorLogTests
{
    const string Spec = "arquitectura-base/feedback-operacions: Registro técnico local de errores";

    static string ReadAll(string folder) =>
        string.Join("\n", Directory.GetFiles(folder, "arca-*.log").Order().Select(File.ReadAllText));

    static Exception Thrown(Exception e)
    {
        try
        {
            throw e;
        }
        catch (Exception caught)
        {
            return caught;
        }
    }

    [Fact]
    [Trait("spec", Spec + " (referencia de error)")]
    public void Each_entry_gets_a_short_reference_that_is_found_in_the_log()
    {
        using var dir = new TempDirectory();
        string reference;
        using (var log = new FileErrorLog(dir.Path))
        {
            reference = log.LogUnexpected(Thrown(new InvalidOperationException("x")), "CreateLockerRange");
        }

        Assert.Matches("^[0-9A-F]{6}$", reference);
        var text = ReadAll(dir.Path);
        Assert.Contains("ref=" + reference, text, StringComparison.Ordinal);
        Assert.Contains("CreateLockerRange", text, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException", text, StringComparison.Ordinal);
    }

    [Fact]
    public void References_differ_between_entries()
    {
        using var dir = new TempDirectory();
        using var log = new FileErrorLog(dir.Path);

        var references = Enumerable.Range(0, 20).Select(_ => log.LogUnexpected(Thrown(new IOException()), "x")).ToList();

        Assert.True(references.Distinct(StringComparer.Ordinal).Count() > 15);
    }

    [Fact]
    [Trait("spec", Spec + " (sin datos personales)")]
    public void The_log_never_contains_the_exception_message_or_its_data()
    {
        using var dir = new TempDirectory();
        var error = Thrown(new InvalidOperationException("No es pot assignar la taquilla a Núria Garcia Puig de 3r ESO B"));
        error.Data["alumne"] = "Núria Garcia Puig";
        var wrapped = Thrown(new IOException("Error llegint la fitxa de Joan Ferrer, correu joan@example.org", error));
        using (var log = new FileErrorLog(dir.Path))
        {
            log.LogUnexpected(wrapped, "AssignLocker");
        }

        var text = ReadAll(dir.Path);

        foreach (var forbidden in new[] { "Núria", "Garcia", "Puig", "3r ESO", "Joan", "Ferrer", "joan@example.org", "fitxa" })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }

        Assert.Contains("System.IO.IOException", text, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException", text, StringComparison.Ordinal); // the cause is still traceable by type
    }

    [Fact]
    [Trait("spec", Spec + " (registro acotado)")]
    public void The_log_is_bounded_by_dropping_the_oldest_files()
    {
        using var dir = new TempDirectory();
        const int limit = 4_000;
        const int keep = 3;
        using (var log = new FileErrorLog(dir.Path, limit, keep))
        {
            for (var i = 0; i < 400; i++)
            {
                log.LogUnexpected(Thrown(new InvalidOperationException("x")), "Bulk" + i);
            }
        }

        var files = Directory.GetFiles(dir.Path, "arca-*.log");

        Assert.True(files.Length <= keep, $"{files.Length} files, expected at most {keep}");
        Assert.All(files, f => Assert.True(new FileInfo(f).Length <= limit + 2_000, "a file may pass the limit by at most one entry"));
        Assert.Contains("Bulk399", ReadAll(dir.Path), StringComparison.Ordinal); // the newest entries survive
        Assert.DoesNotContain("Bulk0\n", ReadAll(dir.Path), StringComparison.Ordinal); // the oldest are gone
    }

    [Fact]
    public void The_folder_is_created_when_missing()
    {
        using var dir = new TempDirectory();
        var folder = Path.Combine(dir.Path, "logs", "nested");

        using (var log = new FileErrorLog(folder))
        {
            log.LogUnexpected(Thrown(new IOException()), "x");
        }

        Assert.True(Directory.Exists(folder));
    }
}
