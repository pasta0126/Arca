// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;

namespace Arca.UI.Shell;

/// <summary>
/// A request to open a section already filtered: which section and screen, and the filters to put on, by name and value, as the screen
/// understands them (for example Status=Free on the map, Payment=pending on the students). It is what a card of the start screen will
/// be made of: a filter that leads to a screen.
/// </summary>
/// <param name="Section">The section to open.</param>
/// <param name="Screen">The screen of the section, or null for its first.</param>
/// <param name="Filters">The filters to put on; every other filter of the screen is taken off first.</param>
public sealed record ScreenFilterRequest(string Section, string? Screen, IReadOnlyDictionary<string, string> Filters);

/// <summary>
/// Opens a section with a filter already on (navegacio-i-cerca, Patrón común de pantalla). The request is held until the section takes it,
/// because a section is built the first time it is opened, which may be by this very request; a section that is already open gets it at once.
/// </summary>
public sealed class ScreenFilterRouter : ObservableObject
{
    readonly Dictionary<string, ScreenFilterRequest> _pending = [];
    NavigationViewModel? _navigation;

    public void Bind(NavigationViewModel navigation) => _navigation = navigation;

    /// <summary>Raised when a section is asked to open with a filter, for the ones already built.</summary>
    public event EventHandler<ScreenFilterRequest>? Requested;

    /// <summary>Opens the section of the request and hands the filter to it.</summary>
    public void Open(ScreenFilterRequest request)
    {
        _pending[request.Section] = request;
        _navigation?.Navigate(request.Section);
        Requested?.Invoke(this, request);
    }

    /// <summary>Takes the request a section has waiting, once: a section calls this when it is built and after it has loaded.</summary>
    public ScreenFilterRequest? Take(string section)
    {
        if (_pending.Remove(section, out var request))
        {
            return request;
        }

        return null;
    }
}
