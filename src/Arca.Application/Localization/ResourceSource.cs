// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Reflection;

namespace Arca.Application.Localization;

/// <summary>
/// Where resource files live: an assembly and the namespace of its Resources folder. A key's capability
/// names the file, so Storage.Error.Unreadable is read from "&lt;namespace&gt;.Storage".
/// </summary>
public sealed record ResourceSource(Assembly Assembly, string Namespace)
{
    public static ResourceSource Application { get; } = new(typeof(ResourceSource).Assembly, "Arca.Application.Resources");
}
