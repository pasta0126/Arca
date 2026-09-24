// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Testing;
using Arca.UI.Notifications;
using Xunit;

namespace Arca.UI.Tests;

public sealed class NotificationCenterTests
{
    const string Spec = "arquitectura-base/feedback-operacions";

    readonly ManualDelay _delay = new();
    readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.Zero));

    NotificationCenter Center() => new(_clock, _delay);

    [Fact]
    [Trait("spec", Spec + ": Notificaciones no bloqueantes (notificación de éxito)")]
    public void Success_disappears_by_itself_after_five_seconds_but_stays_in_the_history()
    {
        var center = Center();

        center.Publish(NotificationKind.Success, "Fet");
        Assert.Single(center.Visible);

        _delay.Elapse(TimeSpan.FromSeconds(4.9));
        Assert.Single(center.Visible);
        _delay.Elapse(TimeSpan.FromSeconds(0.2));

        Assert.Empty(center.Visible);
        Assert.Single(center.History);
    }

    [Theory]
    [InlineData(NotificationKind.Error)]
    [InlineData(NotificationKind.Warning)]
    [Trait("spec", Spec + ": Notificaciones no bloqueantes (notificación de error)")]
    public void Errors_and_warnings_stay_until_the_user_closes_them(NotificationKind kind)
    {
        var center = Center();
        center.Publish(kind, "Ha fallat");

        _delay.Elapse(TimeSpan.FromMinutes(10));
        Assert.Single(center.Visible);

        center.Dismiss(center.Visible[0].Id);

        Assert.Empty(center.Visible);
        Assert.Single(center.History);
    }

    [Fact]
    [Trait("spec", Spec + ": Notificaciones no bloqueantes (historial de la sesión)")]
    public void History_lists_the_session_newest_first()
    {
        var center = Center();

        center.Publish(NotificationKind.Success, "primera");
        _clock.Advance(TimeSpan.FromMinutes(1));
        center.Publish(NotificationKind.Error, "segona");
        _clock.Advance(TimeSpan.FromMinutes(1));
        center.Publish(NotificationKind.Warning, "tercera");

        Assert.Equal(["tercera", "segona", "primera"], center.History.Select(n => n.Text));
        Assert.True(center.History[0].At > center.History[2].At);
    }

    [Fact]
    public void Dismissing_an_unknown_notification_does_nothing()
    {
        var center = Center();
        center.Publish(NotificationKind.Error, "x");

        center.Dismiss(Guid.NewGuid());

        Assert.Single(center.Visible);
    }

    [Fact]
    public void Details_are_kept_for_the_user_who_asks_for_them()
    {
        var center = Center();

        center.Publish(NotificationKind.Error, "Error inesperat", "ref A7F3C9");

        Assert.Equal("ref A7F3C9", center.History[0].Details);
    }
}
