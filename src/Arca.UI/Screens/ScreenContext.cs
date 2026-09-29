// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;

namespace Arca.UI.Screens;

/// <summary>
/// What every domain screen needs besides its use cases: texts, notifications, the technical log, the delay of the busy indicator,
/// the confirmations and the forms, and what to refresh after a write (the global state of the frame).
/// </summary>
public sealed record ScreenContext(
    ILocalizer Localizer, INotificationService Notifications, IErrorLog Log, IDelay Delay, IConfirmationService Confirmations,
    IFormDialogs Forms, Func<Task> AfterWrite, IChoiceDialogs? Choices = null);
