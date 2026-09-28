// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Input;

namespace Arca.UI.Assigning;

/// <summary>What the person wants: this student in this locker. Every way of asking (drag, menu, keyboard) produces one.</summary>
public sealed record AssignmentIntent(Guid StudentId, Guid LockerId);

/// <summary>
/// What is carried while a student is dragged: only the identity of the student, in a format of this application. Nothing
/// else can be dropped on a locker to assign it, so dragging a zone, a locker or anything from outside starts nothing and
/// is ignored where it lands (arrossegar-i-deixar-anar, Solo este caso en v1).
/// </summary>
public static class StudentDragPayload
{
    static readonly DataFormat<string> _format = DataFormat.CreateStringApplicationFormat("arca-student");

    public static IDataTransfer Create(Guid studentId)
    {
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(_format, studentId.ToString("N")));
        return data;
    }

    /// <summary>The student being dragged, or null when what is carried is not a student of this application.</summary>
    public static Guid? TryRead(IDataTransfer data) =>
        data.TryGetValue(_format) is { } text && Guid.TryParseExact(text, "N", out var id) ? id : null;
}
