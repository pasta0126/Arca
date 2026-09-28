// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls;

namespace Arca.UI.Common;

/// <summary>
/// Binds a property of a component to a named theme resource (<see cref="Theme.ArcaResourceKeys"/>), so a component never
/// holds a raw colour, size or margin and follows whatever resources the theme defines (ux-fonaments, D13).
/// </summary>
public static class ThemeBinding
{
    /// <summary>Binds a property (a brush, a font size, a spacing) directly to the resource.</summary>
    public static T Themed<T>(this T control, AvaloniaProperty property, string key) where T : Control
    {
        control.Bind(property, control.GetResourceObservable(key));
        return control;
    }

    /// <summary>Binds a margin or padding to a spacing resource: the same distance on every side.</summary>
    public static T ThemedThickness<T>(this T control, AvaloniaProperty property, string key) where T : Control
    {
        control.Bind(property, new ThicknessOf(control.GetResourceObservable(key)));
        return control;
    }

    sealed class ThicknessOf(IObservable<object?> spacing) : IObservable<object?>
    {
        public IDisposable Subscribe(IObserver<object?> observer) => spacing.Subscribe(new Adapter(observer));

        sealed class Adapter(IObserver<object?> next) : IObserver<object?>
        {
            public void OnNext(object? value) => next.OnNext(value is double size ? new Thickness(size) : new Thickness(0));

            public void OnError(Exception error) => next.OnError(error);

            public void OnCompleted() => next.OnCompleted();
        }
    }
}
