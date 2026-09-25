// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Diagnostics;
using System.Net;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace Arca.UI.Access;

/// <summary>
/// Copies the recovery key to the clipboard, or writes a small page with it, opens that page so it can be printed
/// from the browser, and removes the page when the screen closes. The page lives only in the user's temporary
/// folder, never leaves the computer, and is deleted as soon as the screen ends.
/// </summary>
public sealed class WindowKeySink(Window window) : IKeySink
{
    readonly List<string> _files = [];

    public async Task CopyAsync(string text)
    {
        if (TopLevel.GetTopLevel(window)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    public Task PrintAsync(string title, IReadOnlyList<string> lines)
    {
        var html = new StringBuilder("<!doctype html><meta charset=\"utf-8\"><title>")
            .Append(WebUtility.HtmlEncode(title))
            .Append("</title><body style=\"font-family:sans-serif;max-width:40em;margin:3em auto\"><h1>")
            .Append(WebUtility.HtmlEncode(title))
            .Append("</h1>");
        foreach (var line in lines)
        {
            html.Append("<p style=\"font-size:1.2em\">").Append(WebUtility.HtmlEncode(line)).Append("</p>");
        }

        html.Append("<script>window.print()</script></body>");
        var file = Path.Combine(Path.GetTempPath(), "arca-" + Guid.NewGuid().ToString("N") + ".html");
        File.WriteAllText(file, html.ToString(), new UTF8Encoding(false));
        _files.Add(file);
        Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        return Task.CompletedTask;
    }

    public void Cleanup()
    {
        foreach (var file in _files)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // The temporary folder is cleaned by the system; nothing else to do.
            }
        }

        _files.Clear();
    }
}
