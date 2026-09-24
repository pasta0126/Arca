// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application;

/// <summary>What the information screen shows: the application version and the version of the open database's schema.</summary>
public sealed record AppInfo(string ApplicationVersion, string SchemaVersion);
