// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Arca.UI.Backup;

/// <summary>The file chooser of the system over the main window, for files of backup of ARCA.</summary>
public sealed class WindowBackupFilePicker(Func<Window?> owner, ILocalizer localizer) : IBackupFilePicker
{
    const string Extension = "arcabackup";

    public async Task<string?> PickSaveAsync(string suggestedName, string? folder)
    {
        if (owner()?.StorageProvider is not { } storage)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = localizer.Get("Backup.Label.PickSaveTitle"),
            SuggestedFileName = suggestedName,
            DefaultExtension = Extension,
            ShowOverwritePrompt = false, // asked by ARCA itself, with the same wording in every system
            SuggestedStartLocation = await StartAsync(storage, folder),
            FileTypeChoices = [FileType()],
        });
        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickOpenAsync(string? folder)
    {
        if (owner()?.StorageProvider is not { } storage)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = localizer.Get("Backup.Label.PickOpenTitle"),
            AllowMultiple = false,
            SuggestedStartLocation = await StartAsync(storage, folder),
            FileTypeFilter = [FileType()],
        });
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    FilePickerFileType FileType() => new(localizer.Get("Backup.Label.FileTypeName")) { Patterns = [$"*.{Extension}"] };

    static async Task<IStorageFolder?> StartAsync(IStorageProvider storage, string? folder) =>
        string.IsNullOrEmpty(folder) ? null : await storage.TryGetFolderFromPathAsync(folder);
}
