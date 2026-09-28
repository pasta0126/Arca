// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Arca.UI.Assigning;

/// <summary>
/// Makes a locker on screen a destination for a dragged student: it lights up as valid (or invalid, with the reason as a
/// tooltip) while a student is over it, does not accept the drop when it is invalid, and starts the assignment when a student
/// is dropped on a valid one. Only a student of this application is accepted; anything else is ignored.
/// </summary>
public static class LockerDropTarget
{
    /// <param name="cell">The locker as drawn: a border, so its outline can show the answer.</param>
    /// <param name="lockerId">The locker it stands for, or null if it cannot receive an assignment.</param>
    public static void Attach(Border cell, Func<Guid?> lockerId, AssignmentDropViewModel model)
    {
        DragDrop.SetAllowDrop(cell, true);

        DragDrop.AddDragEnterHandler(cell, async (_, e) =>
        {
            if (StudentDragPayload.TryRead(e.DataTransfer) is null || lockerId() is not { } id)
            {
                return;
            }

            Show(cell, await model.PreviewAsync(id));
        });
        DragDrop.AddDragOverHandler(cell, (_, e) =>
        {
            var known = lockerId() is { } id ? model.Known(id) : null;
            e.DragEffects = StudentDragPayload.TryRead(e.DataTransfer) is not null && known is { State: DropTargetState.Valid }
                ? DragDropEffects.Move
                : DragDropEffects.None;
        });
        DragDrop.AddDragLeaveHandler(cell, (_, _) => Show(cell, null));
        DragDrop.AddDropHandler(cell, async (_, e) =>
        {
            Show(cell, null);
            if (StudentDragPayload.TryRead(e.DataTransfer) is not null && lockerId() is { } id)
            {
                await model.DropAsync(id);
            }
        });
    }

    /// <summary>Draws the answer on the locker: an outline in the theme's success or error colour, and the reason as a tooltip.</summary>
    static void Show(Border cell, DropTargetFeedback? feedback)
    {
        if (feedback is null)
        {
            cell.ClearValue(Border.BorderBrushProperty);
            cell.ClearValue(Border.BorderThicknessProperty);
            ToolTip.SetTip(cell, null);
            return;
        }

        cell.Themed(Border.BorderBrushProperty, feedback.State == DropTargetState.Valid ? ArcaResourceKeys.Success : ArcaResourceKeys.Error);
        cell.BorderThickness = new Thickness(3);
        ToolTip.SetTip(cell, feedback.Reason);
    }
}
