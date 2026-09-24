// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Startup;
using Arca.Domain.Common;
using Arca.UI.Common;

namespace Arca.UI.Startup;

/// <summary>
/// The start-up screen: the name of the application, the stage that is running, and, if starting fails, the cause,
/// the recommended action and a reference (the stable error code, or the log reference of an unexpected failure).
/// </summary>
public sealed class SplashViewModel(ILocalizer localizer) : ObservableObject
{
    string _stageText = localizer.Get("Startup.Label.Starting");
    bool _isError;
    string _errorTitle = string.Empty;
    string _message = string.Empty;
    string _reference = string.Empty;

    public string Name => localizer.Get("App.Label.Title");

    public string StageText
    {
        get => _stageText;
        private set => Set(ref _stageText, value);
    }

    public bool IsError
    {
        get => _isError;
        private set => Set(ref _isError, value);
    }

    public string ErrorTitle
    {
        get => _errorTitle;
        private set => Set(ref _errorTitle, value);
    }

    public string Message
    {
        get => _message;
        private set => Set(ref _message, value);
    }

    public string Reference
    {
        get => _reference;
        private set => Set(ref _reference, value);
    }

    public string CloseLabel => localizer.Get("App.Label.Close");

    /// <summary>Shows which stage is running, with its position when there are several.</summary>
    public void Show(StartupProgress progress) =>
        StageText = localizer.Get(progress.TextKey);

    /// <summary>Switches the same window to the error mode. The window stays open until the user closes it.</summary>
    public void ShowError(Error error, string? reference = null)
    {
        ErrorTitle = localizer.Get("App.Label.StartupErrorTitle");
        Message = localizer.Message(error);
        Reference = localizer.Get("Startup.Label.ErrorReference", reference ?? error.Code);
        IsError = true;
    }
}
