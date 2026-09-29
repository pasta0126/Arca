// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Identity;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Notifications;
using Arca.UI.Screens;

namespace Arca.UI.Identity;

/// <summary>What the identity of the centre reads and does, as the composition offers it.</summary>
/// <param name="Get">The identity saved.</param>
/// <param name="Save">Saves it and answers what was saved.</param>
/// <param name="CheckLogo">Whether a file can be a logo by what it is (PNG or JPEG, up to 1 MB): the reason it cannot, or null.</param>
public sealed record IdentityServices(
    Func<CancellationToken, Task<Result<CentreIdentityView>>> Get,
    Func<SaveCentreIdentityRequest, CancellationToken, Task<Result<CentreIdentityView>>> Save,
    Func<byte[], Error?> CheckLogo);

/// <summary>
/// The settings of the identity of the centre (ui-shell, identitat-i-tema): its name, its logo and its accent colour, with a preview
/// of how the header will look before anything is saved. A logo that is refused, for its format, its size or because it cannot be
/// read, is refused when it is chosen and the current one stays. Saving goes through the run-once command, so it acts once.
/// </summary>
public sealed class IdentityViewModel : ObservableObject
{
    readonly IdentityServices _services;
    readonly CentreIdentityModel _model;
    readonly ILogoPicker _picker;
    readonly ILocalizer _localizer;
    readonly ResultNotifier _notifier;
    readonly RunOnceCommand<CentreIdentityView> _save;
    string _name = string.Empty;
    string _accent = string.Empty;
    byte[]? _pendingLogo;
    LogoChange _change = LogoChange.Keep;

    public IdentityViewModel(
        IdentityServices services, CentreIdentityModel model, ILogoPicker picker, ILocalizer localizer, INotificationService notifications, IErrorLog log,
        IDelay delay)
    {
        _services = services;
        _model = model;
        _picker = picker;
        _localizer = localizer;
        _notifier = new ResultNotifier(notifications, localizer, log);
        _save = new RunOnceCommand<CentreIdentityView>(
            async (ct, _) =>
            {
                var saved = await services.Save(new SaveCentreIdentityRequest(Name, _change, _pendingLogo, AccentText.Length == 0 ? null : AccentText), ct);
                if (saved.IsSuccess)
                {
                    _model.Set(saved.Value!); // the header and the accent follow at once
                }

                return saved;
            },
            _ => localizer.Get("Identity.Result.Saved"), "SaveCentreIdentity", notifications, localizer, log, delay, () =>
            {
                Reload();
                return Task.CompletedTask;
            });
        SaveAction = new AppAction("SaveIdentity", localizer.Get("Common.Action.Save"));
        SaveAction.Attach(() => _ = _save.RunAsync());
        ChooseLogoAction = new AppAction("ChooseLogo", localizer.Get("Identity.Action.ChooseLogo"));
        ChooseLogoAction.Attach(() => _ = ChooseLogoAsync());
        RemoveLogoAction = new AppAction("RemoveLogo", localizer.Get("Identity.Action.RemoveLogo"));
        RemoveLogoAction.Attach(RemoveLogo, () => PreviewLogo is null ? Availability.Unavailable(localizer.Get("Identity.Reason.NoLogo")) : Availability.Available);
        ResetAccentAction = new AppAction("ResetAccent", localizer.Get("Identity.Action.ResetAccent"));
        ResetAccentAction.Attach(() => AccentText = string.Empty);
        Reload();
    }

    public AppAction SaveAction { get; }

    public AppAction ChooseLogoAction { get; }

    public AppAction RemoveLogoAction { get; }

    public AppAction ResetAccentAction { get; }

    /// <summary>The save command, so a test or the view can watch it work.</summary>
    public IWorkState Work => _save;

    /// <summary>The name being typed. Empty is refused when saving.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (Set(ref _name, value))
            {
                Raise(nameof(PreviewName));
            }
        }
    }

    /// <summary>The accent being typed as #RRGGBB; empty for the default one.</summary>
    public string AccentText
    {
        get => _accent;
        set
        {
            if (Set(ref _accent, value))
            {
                Raise(nameof(PreviewAccent));
                Raise(nameof(AccentError));
            }
        }
    }

    /// <summary>The message of an accent that is not a colour, next to its field. Null while it is one or empty.</summary>
    public string? AccentError => AccentText.Length == 0 || IsColour(AccentText) ? null : _localizer.Message(new Error("Identity.AccentInvalid"));

    /// <summary>The accent to preview, or null when it is empty or not a colour.</summary>
    public string? PreviewAccent => IsColour(AccentText) ? AccentText : null;

    /// <summary>The name the header will show: the one typed, or the name of the application while there is none.</summary>
    public string PreviewName => Name.Trim().Length > 0 ? Name.Trim() : _localizer.Get("App.Label.Title");

    /// <summary>The logo the header will show: the one chosen, or the current one, or null.</summary>
    public byte[]? PreviewLogo => _change switch
    {
        LogoChange.Replace => _pendingLogo,
        LogoChange.Remove => null,
        _ => _model.Current.Logo,
    };

    /// <summary>Whether the logo is going to change when saving, for the note under the preview.</summary>
    public bool LogoWillChange => _change != LogoChange.Keep;

    static bool IsColour(string text) => text.Length == 7 && text[0] == '#' && text[1..].All(Uri.IsHexDigit);

    /// <summary>Asks for a file, checks it, and puts it in the preview. A file that is refused changes nothing.</summary>
    public async Task ChooseLogoAsync()
    {
        var bytes = await _picker.PickAsync();
        if (bytes is null)
        {
            return;
        }

        if (_services.CheckLogo(bytes) is { } refusal)
        {
            _notifier.Error(refusal);
            return;
        }

        if (LogoImages.TryDecode(bytes) is null)
        {
            _notifier.Error(new Error("Identity.LogoUnreadable")); // it looks like an image but is not one: the current logo stays
            return;
        }

        (_pendingLogo, _change) = (bytes, LogoChange.Replace);
        RaiseLogo();
    }

    void RemoveLogo()
    {
        (_pendingLogo, _change) = (null, LogoChange.Remove);
        RaiseLogo();
    }

    void RaiseLogo()
    {
        Raise(nameof(PreviewLogo));
        Raise(nameof(LogoWillChange));
        RemoveLogoAction.Refresh();
    }

    /// <summary>Fills the form from what is saved and forgets whatever was pending.</summary>
    public void Reload()
    {
        (_pendingLogo, _change) = (null, LogoChange.Keep);
        Name = _model.Current.Name ?? string.Empty;
        AccentText = _model.Current.Accent ?? string.Empty;
        RaiseLogo();
    }

    /// <summary>Saves the identity: the header and the accent follow it at once. A refusal (an empty name, a colour that is not one) is said and nothing is saved.</summary>
    public Task SaveAsync() => _save.RunAsync();

}
