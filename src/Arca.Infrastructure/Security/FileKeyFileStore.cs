// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Security;
using Arca.Domain.Common;

namespace Arca.Infrastructure.Security;

/// <summary>The key file store on disk: <see cref="KeyFileStore"/> behind the port that Application uses.</summary>
public sealed class FileKeyFileStore : IKeyFileStore
{
    public Result<KeyFile> Read(string databasePath) => KeyFileStore.Read(databasePath);

    public void Write(string databasePath, KeyFile file) => KeyFileStore.Write(databasePath, file);

    public void DiscardPrevious(string databasePath) => KeyFileStore.DiscardPrevious(databasePath);
}
