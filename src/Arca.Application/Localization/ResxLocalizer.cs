// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;
using Arca.Domain.Common;

namespace Arca.Application.Localization;

/// <summary>
/// Reads texts from .resx resources. The neutral files are Catalan, so any language that lacks a key falls
/// back to the Catalan text for that key (the resource lookup walks from the language to the neutral one).
/// </summary>
public sealed class ResxLocalizer : ILocalizer
{
    readonly ResourceSource[] _sources;
    readonly ConcurrentDictionary<string, ResourceManager> _managers = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, bool> _missingFiles = new(StringComparer.Ordinal);

    public ResxLocalizer(CultureInfo? culture = null, params ResourceSource[] sources)
    {
        Culture = culture ?? Cultures.Catalan;
        _sources = sources.Length == 0 ? [ResourceSource.Application] : sources;
    }

    public CultureInfo Culture { get; }

    public string Get(string key, params object[] args)
    {
        var text = Lookup(key);
        if (text is null)
        {
            return key;
        }

        try
        {
            return args.Length == 0 ? text : string.Format(Culture, text, args);
        }
        catch (FormatException)
        {
            return text; // a text that does not match its arguments must not break the screen
        }
    }

    public string Message(Error error) => Get(ResourceKeys.For(error), [.. error.Args]);

    public string Message(Notice notice) => Get(ResourceKeys.For(notice), [.. notice.Args]);

    public string Format(Money amount) => amount.ToString(Culture);

    public string Format(DateOnly date) => date.ToString("d", Culture);

    public string Format(DateTimeOffset instant) => instant.ToLocalTime().ToString("g", Culture);

    string? Lookup(string key)
    {
        var capability = ResourceKeys.CapabilityOf(key);
        foreach (var source in _sources)
        {
            var baseName = source.Namespace + "." + capability;
            if (_missingFiles.ContainsKey(baseName))
            {
                continue;
            }

            var manager = _managers.GetOrAdd(baseName, name => new ResourceManager(name, source.Assembly));
            try
            {
                var text = manager.GetString(key, Culture);
                if (text is not null)
                {
                    return text;
                }
            }
            catch (MissingManifestResourceException)
            {
                _missingFiles[baseName] = true; // this source has no file for the capability; do not ask again
            }
        }

        return null;
    }
}
