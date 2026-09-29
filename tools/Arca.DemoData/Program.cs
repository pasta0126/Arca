// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Security;
using Arca.DemoData;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;

// Creates a database full of invented data for trying ARCA (docs/datos-de-ejemplo.md). Not part of the product.
//
//   dotnet run --project tools/Arca.DemoData -- [--folder <folder>] [--password <text>] [--seed <n>] [--force]
//
// The default folder is the one the portable application uses when it is run from a build (src/Arca.Desktop/bin/Debug/net10.0/data),
// so `build/run.sh` opens it. Close ARCA before running this: it needs the database to itself.

const string DefaultPassword = "arca";
var options = args.Select((a, i) => (a, i)).Where(x => x.a.StartsWith("--", StringComparison.Ordinal)).ToDictionary(x => x.a, x => x.i + 1 < args.Length && !args[x.i + 1].StartsWith("--", StringComparison.Ordinal) ? args[x.i + 1] : string.Empty);
var folder = Path.GetFullPath(options.GetValueOrDefault("--folder") is { Length: > 0 } given ? given : "src/Arca.Desktop/bin/Debug/net10.0/data");
var password = options.GetValueOrDefault("--password") is { Length: > 0 } typed ? typed : DefaultPassword;
var seed = int.TryParse(options.GetValueOrDefault("--seed"), out var parsed) ? parsed : 2026;
var database = Path.Combine(folder, "arca.db");

if (File.Exists(database))
{
    if (!options.ContainsKey("--force"))
    {
        Console.Error.WriteLine($"Ja existeix una base de dades a {folder}. Afegeix --force per esborrar-la i crear-ne una de nova.");
        return 1;
    }

    foreach (var file in Directory.GetFiles(folder, "arca.db*").Concat(Directory.GetFiles(folder, "arca.keys*")))
    {
        File.Delete(file);
    }
}

var access = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore()).CreateAccess(password, password);
if (!access.IsSuccess)
{
    Console.Error.WriteLine("La contrasenya no compleix la política: " + access.Error!.Code);
    return 1;
}

using var newAccess = access.Value!;
var recovery = newAccess.RecoveryKey;
var groups = newAccess.Challenge.Indices.Select(i => RecoveryKey.Groups(recovery)[i]).ToArray();
var created = await DatabaseCreator.CreateAsync(database, newAccess, groups);
if (!created.IsSuccess)
{
    Console.Error.WriteLine("No s'ha pogut crear la base de dades: " + created.Error!.Code);
    return 1;
}

var key = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore()).Unlock(database, password).Value!;
var builder = new DemoBuilder(() => new ArcaDbContext(database, key), new Random(seed));
await builder.BuildAsync(Console.WriteLine);

Console.WriteLine();
Console.WriteLine($"Base de dades creada a {folder}");
Console.WriteLine($"Contrasenya: {password}");
Console.WriteLine($"Clau de recuperació: {RecoveryKey.Format(recovery)}");
return 0;
