// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Arca.UI.Assigning;

/// <summary>
/// Lets the person drag a student (a row of a list, a card) toward a locker. It starts only from the control it is
/// attached to, and only after the pointer moves a few pixels with the left button held, so a click is still a click.
/// Escape during the drag is handled by the system and cancels it: nothing has been written until a drop.
/// </summary>
public static class StudentDragSource
{
    const double StartDistance = 4;

    /// <param name="control">What the person grabs.</param>
    /// <param name="studentId">The student it stands for, or null if it is not a student that can be dragged.</param>
    public static void Attach(Control control, Func<Guid?> studentId, AssignmentDropViewModel model)
    {
        PointerPressedEventArgs? pressed = null;
        Point start = default;

        control.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(control).Properties.IsLeftButtonPressed && studentId() is not null)
            {
                pressed = e;
                start = e.GetPosition(control);
            }
        };
        control.PointerReleased += (_, _) => pressed = null;
        control.PointerMoved += async (_, e) =>
        {
            if (pressed is null)
            {
                return;
            }

            var moved = e.GetPosition(control) - start;
            if (Math.Abs(moved.X) < StartDistance && Math.Abs(moved.Y) < StartDistance)
            {
                return;
            }

            var trigger = pressed;
            pressed = null;
            if (studentId() is not { } id)
            {
                return;
            }

            model.BeginDrag(id);
            try
            {
                await DragDrop.DoDragDropAsync(trigger, StudentDragPayload.Create(id), DragDropEffects.Move);
            }
            finally
            {
                model.End(); // dropped on a valid locker or not, the drag itself is over
            }
        };
    }
}
