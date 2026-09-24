// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Feedback;

/// <summary>
/// Progress of a long operation, reported through IProgress (arquitectura-base, D12). The operation also takes a
/// CancellationToken, and honours it only while <paramref name="CanCancel"/> is true: once it starts its
/// indivisible save phase it reports false and stops listening.
/// </summary>
/// <param name="Current">Items processed so far.</param>
/// <param name="Total">Items to process.</param>
/// <param name="CanCancel">Whether cancelling now leaves no data half done.</param>
public sealed record OperationProgress(int Current, int Total, bool CanCancel = true);
