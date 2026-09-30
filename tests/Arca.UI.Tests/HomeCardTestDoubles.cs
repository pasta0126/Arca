// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Home;
using Arca.Domain.Common;
using Arca.UI.Home;
using Arca.UI.Screens;

namespace Arca.UI.Tests;

/// <summary>Shows no window: it keeps the forms it was asked to show, so a test fills them in and saves them.</summary>
internal sealed class CapturingForms : IFormDialogs
{
    public List<IFormModel> Shown { get; } = [];

    public IFormModel Last => Shown[^1];

    public Task<bool> ShowAsync(IFormModel form)
    {
        Shown.Add(form);
        return Task.FromResult(false);
    }
}

/// <summary>The cards of the start screen as the screens reach them, in memory: what was created and edited is kept, and a wrong title is refused as the domain refuses it.</summary>
internal sealed class FakeHomeCards
{
    public List<CreateHomeCardRequest> Created { get; } = [];

    public List<EditHomeCardRequest> Edited { get; } = [];

    public CardOptions Options { get; set; } = new([new CardZoneOption(Guid.NewGuid(), "Planta 1")], ["1r ESO", "2n ESO"], ["A", "B"]);

    public int? Preview { get; set; } = 7;

    public HomeCardServices Services() => new(
        _ => Task.FromResult(Result<int>.Success(0)),
        _ => Task.FromResult(Result<int>.Success(0)),
        _ => Task.FromResult(Result<HomeCardsView>.Success(new HomeCardsView("2026-2027", [], true, true))),
        (_, _) => Task.FromResult(Result<ResolvedHomeCard>.Failure(new Error("HomeCards.NotFound"))),
        (request, _) =>
        {
            var title = request.Title?.Trim() ?? string.Empty;
            if (title.Length == 0)
            {
                return Task.FromResult(Result<HomeCardSaved>.Failure(new Error("HomeCards.TitleRequired")));
            }

            if (title.Length > 60)
            {
                return Task.FromResult(Result<HomeCardSaved>.Failure(new Error("HomeCards.TitleTooLong", Args: [60])));
            }

            Created.Add(request);
            return Task.FromResult(Result<HomeCardSaved>.Success(new HomeCardSaved(Guid.NewGuid(), title)));
        },
        (request, _) =>
        {
            Edited.Add(request);
            return Task.FromResult(Result<HomeCardSaved>.Success(new HomeCardSaved(request.Id, request.Title!.Trim())));
        },
        (_, _) => Task.FromResult(Result<bool>.Success(true)),
        (_, _) => Task.FromResult(Result<string>.Success("x")),
        _ => Task.FromResult(Result<CardOptions>.Success(Options)),
        (_, _) => Task.FromResult(Result<int?>.Success(Preview)));
}
