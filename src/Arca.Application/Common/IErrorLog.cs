// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Common;

/// <summary>
/// The technical log for unexpected failures. It returns a short reference the user can quote, and it never
/// records personal data: only the type of the error, where it happened and the stack, never its message.
/// </summary>
public interface IErrorLog
{
    /// <returns>The short reference that identifies this entry in the log.</returns>
    string LogUnexpected(Exception error, string context);
}
