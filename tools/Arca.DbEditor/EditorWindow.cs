// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace Arca.DbEditor;

/// <summary>Window of the editor. Everything is built here in code, like the rest of the repository, with no markup files.</summary>
public sealed class EditorWindow : Window
{
    const int PageSize = 200;

    readonly TextBox _path = new() { PlaceholderText = "Fitxer de base de dades (.db, .sqlite, arca.db…)", MinWidth = 320 };
    readonly TextBox _secret = new() { PlaceholderText = "Contrasenya del centre o clau en hexadecimal (buit si no va xifrat)", PasswordChar = '•', MinWidth = 280 };
    readonly ListBox _tables = new() { MinWidth = 180 };
    readonly TextBox _filter = new() { PlaceholderText = "Filtra files (qualsevol columna)", MinWidth = 220 };
    readonly DataGrid _grid = new() { IsReadOnly = false, CanUserSortColumns = false, CanUserResizeColumns = true, SelectionMode = DataGridSelectionMode.Extended, GridLinesVisibility = DataGridGridLinesVisibility.All };
    readonly DataGrid _results = new() { IsReadOnly = true, CanUserResizeColumns = true, GridLinesVisibility = DataGridGridLinesVisibility.All };
    readonly TextBox _sql = new() { AcceptsReturn = true, FontFamily = new FontFamily("Menlo,Consolas,monospace"), MinHeight = 90, PlaceholderText = "SELECT * FROM …" };
    readonly TextBlock _status = new() { Margin = new Thickness(8, 4), TextWrapping = TextWrapping.Wrap };
    readonly TextBlock _page = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
    readonly TextBlock _sqlInfo = new() { Margin = new Thickness(0, 4) };
    readonly Button _add = new() { Content = "Afegeix fila" };
    readonly Button _delete = new() { Content = "Elimina seleccionades" };
    readonly Button _save = new() { Content = "Desa els canvis", IsEnabled = false };
    readonly Button _discard = new() { Content = "Descarta", IsEnabled = false };
    readonly Button _previous = new() { Content = "‹" };
    readonly Button _next = new() { Content = "›" };

    DbSession? _session;
    TableInfo? _table;
    DataPage? _data;
    int _offset;
    bool _loading;
    bool _warnedClosing;

    public EditorWindow(string? file)
    {
        Title = "ARCA · Editor de bases de dades";
        Width = 1180;
        Height = 720;

        var browse = new Button { Content = "Obre…" };
        var connect = new Button { Content = "Connecta", IsDefault = true };
        browse.Click += async (_, _) => await BrowseAsync();
        connect.Click += (_, _) => Connect();
        _tables.SelectionChanged += (_, _) => OnTableChosen();
        _filter.TextChanged += (_, _) => { if (!_loading) { _offset = 0; LoadPage(); } };
        _previous.Click += (_, _) => { _offset = Math.Max(0, _offset - PageSize); LoadPage(); };
        _next.Click += (_, _) => { _offset += PageSize; LoadPage(); };
        _add.Click += (_, _) => AddRow();
        _delete.Click += (_, _) => DeleteRows();
        _save.Click += (_, _) => Save();
        _discard.Click += (_, _) => Discard();
        _grid.CellEditEnded += (_, e) => OnCellEdited(e);

        var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(8) };
        top.Children.Add(_path);
        top.Children.Add(browse);
        top.Children.Add(_secret);
        top.Children.Add(connect);

        var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 8) };
        foreach (var control in new Control[] { _filter, _previous, _next, _page, _add, _delete, _save, _discard })
        {
            bar.Children.Add(control);
        }

        var tableTab = new DockPanel { Margin = new Thickness(8) };
        DockPanel.SetDock(bar, Dock.Top);
        tableTab.Children.Add(bar);
        tableTab.Children.Add(_grid);

        var run = new Button { Content = "Executa" };
        run.Click += (_, _) => RunSql();
        var sqlTab = new DockPanel { Margin = new Thickness(8) };
        var sqlTop = new StackPanel { Spacing = 6 };
        sqlTop.Children.Add(_sql);
        sqlTop.Children.Add(run);
        sqlTop.Children.Add(_sqlInfo);
        DockPanel.SetDock(sqlTop, Dock.Top);
        sqlTab.Children.Add(sqlTop);
        sqlTab.Children.Add(_results);

        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Taula", Content = tableTab });
        tabs.Items.Add(new TabItem { Header = "SQL", Content = sqlTab });

        var left = new DockPanel { Margin = new Thickness(8, 0, 0, 8) };
        left.Children.Add(_tables);

        var statusBar = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 1, 0, 0), Child = _status };
        var root = new DockPanel();
        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(statusBar, Dock.Bottom);
        DockPanel.SetDock(left, Dock.Left);
        root.Children.Add(top);
        root.Children.Add(statusBar);
        root.Children.Add(left);
        root.Children.Add(tabs);
        Content = root;

        SetStatus("Tria un fitxer i connecta. Els canvis no s'apliquen fins que els desis.");
        UpdateButtons();
        if (file is not null)
        {
            _path.Text = file;
        }
    }

    async Task BrowseAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Fitxer de base de dades", AllowMultiple = false });
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } local)
        {
            _path.Text = local;
        }
    }

    void Connect()
    {
        if (_session?.HasPendingChanges == true)
        {
            SetStatus("Hi ha canvis sense desar: desa'ls o descarta'ls abans d'obrir un altre fitxer.");
            return;
        }

        try
        {
            var session = DbSession.Open(_path.Text ?? "", _secret.Text);
            _session?.Dispose();
            _session = session;
            _table = null;
            _data = null;
            _grid.ItemsSource = null;
            _grid.Columns.Clear();
            _tables.ItemsSource = session.Tables().Select(t => t.IsView ? t.Name + "  (vista)" : t.Name).ToList();
            _tablesInfo = session.Tables();
            SetStatus($"Obert: {session.Path} · {session.Mode} · {_tablesInfo.Count} taules");
        }
        catch (Exception e) when (e is OpenException or Microsoft.Data.Sqlite.SqliteException)
        {
            SetStatus(e.Message, error: true);
        }

        UpdateButtons();
    }

    IReadOnlyList<TableInfo> _tablesInfo = [];

    void OnTableChosen()
    {
        if (_tables.SelectedIndex < 0 || _tables.SelectedIndex >= _tablesInfo.Count)
        {
            return;
        }

        _table = _tablesInfo[_tables.SelectedIndex];
        _offset = 0;
        _loading = true;
        _filter.Text = "";
        _loading = false;
        LoadPage();
    }

    void LoadPage()
    {
        if (_session is null || _table is null)
        {
            return;
        }

        try
        {
            var data = _session.Read(_table, _filter.Text?.Trim() ?? "", _offset, PageSize);
            if (_offset > 0 && data.Rows.Count == 0)
            {
                _offset = Math.Max(0, (int)((data.Total - 1) / PageSize) * PageSize);
                data = _session.Read(_table, _filter.Text?.Trim() ?? "", _offset, PageSize);
            }

            _data = data;
            _grid.ItemsSource = null;
            _grid.Columns.Clear();
            for (var i = 0; i < data.Columns.Count; i++)
            {
                _grid.Columns.Add(new DataGridTextColumn
                {
                    Header = data.Columns[i],
                    Binding = new Binding($"[{i}]") { Mode = BindingMode.TwoWay },
                    IsReadOnly = !data.HasRowId,
                });
            }

            var skip = data.HasRowId ? 1 : 0;
            _grid.ItemsSource = data.Rows
                .Select(r => new RowItem(data.HasRowId ? Convert.ToInt64(r[0], System.Globalization.CultureInfo.InvariantCulture) : null,
                    r.Skip(skip).Select(DbSession.Display).ToArray()))
                .ToList();

            var from = data.Total == 0 ? 0 : _offset + 1;
            _page.Text = $"{from}–{_offset + data.Rows.Count} de {data.Total}";
            _previous.IsEnabled = _offset > 0;
            _next.IsEnabled = _offset + PageSize < data.Total;
            if (!data.HasRowId)
            {
                SetStatus("Aquesta taula o vista no té rowid: només es pot consultar. Per canviar-la usa la pestanya SQL.");
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            SetStatus(e.Message, error: true);
        }

        UpdateButtons();
    }

    void OnCellEdited(DataGridCellEditEndedEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit || _session is null || _table is null || _data is null
            || e.Row.DataContext is not RowItem { RowId: { } rowId } item)
        {
            return;
        }

        var index = _grid.Columns.IndexOf(e.Column);
        var original = _data.Rows.FirstOrDefault(r => Convert.ToInt64(r[0], System.Globalization.CultureInfo.InvariantCulture) == rowId);
        var before = original is null ? null : DbSession.Display(original[index + 1]);
        if (before == item[index])
        {
            return;
        }

        try
        {
            _session.Update(_table.Name, rowId, _data.Columns[index], item[index]);
            SetStatus($"Cel·la «{_data.Columns[index]}» canviada (pendent de desar).");
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            if (before is not null)
            {
                item[index] = before;
            }

            SetStatus(ex.Message, error: true);
        }

        UpdateButtons();
    }

    void AddRow()
    {
        if (_session is null || _table is null)
        {
            return;
        }

        try
        {
            _session.Insert(_table.Name);
            _filter.Text = "";
            _offset = Math.Max(0, (int)(((_data?.Total ?? 0) / PageSize) * PageSize)); // the new row is last by rowid
            LoadPage();
            _grid.ScrollIntoView(_grid.ItemsSource?.Cast<object>().LastOrDefault(), null);
            SetStatus("Fila afegida (pendent de desar). Edita'n les cel·les.");
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            SetStatus(e.Message, error: true);
        }

        UpdateButtons();
    }

    void DeleteRows()
    {
        if (_session is null || _table is null)
        {
            return;
        }

        var ids = _grid.SelectedItems.Cast<RowItem>().Where(r => r.RowId.HasValue).Select(r => r.RowId!.Value).ToList();
        if (ids.Count == 0)
        {
            SetStatus("Selecciona primer les files que vols eliminar.");
            return;
        }

        try
        {
            var count = _session.Delete(_table.Name, ids);
            LoadPage();
            SetStatus($"{count} files eliminades (pendent de desar).");
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            SetStatus(e.Message, error: true);
        }

        UpdateButtons();
    }

    void RunSql()
    {
        if (_session is null)
        {
            SetStatus("Connecta primer amb un fitxer.");
            return;
        }

        try
        {
            var result = _session.Run(_sql.Text ?? "");
            _results.ItemsSource = null;
            _results.Columns.Clear();
            for (var i = 0; i < result.Columns.Count; i++)
            {
                _results.Columns.Add(new DataGridTextColumn { Header = result.Columns[i], Binding = new Binding($"[{i}]") });
            }

            _results.ItemsSource = result.Rows.Select(r => new RowItem(null, r.Select(DbSession.Display).ToArray())).ToList();
            _sqlInfo.Text = result.Columns.Count > 0
                ? $"{result.Rows.Count} files"
                : $"{result.Affected} files afectades (pendent de desar)";
            if (result.Columns.Count == 0)
            {
                LoadPage();
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            _sqlInfo.Text = e.Message;
        }

        UpdateButtons();
    }

    void Save()
    {
        try
        {
            _session?.Commit();
            SetStatus($"Canvis desats. Còpia anterior: {_session?.BackupPath}");
        }
        catch (Microsoft.Data.Sqlite.SqliteException e)
        {
            SetStatus(e.Message, error: true);
        }

        UpdateButtons();
    }

    void Discard()
    {
        _session?.Rollback();
        LoadPage();
        SetStatus("Canvis descartats.");
        UpdateButtons();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_session?.HasPendingChanges == true && !_warnedClosing)
        {
            _warnedClosing = true;
            e.Cancel = true;
            SetStatus("Hi ha canvis sense desar. Desa'ls o descarta'ls; si tanques una altra vegada es perdran.", error: true);
            return;
        }

        _session?.Dispose();
    }

    void UpdateButtons()
    {
        var pending = _session?.HasPendingChanges == true;
        var editable = _data?.HasRowId == true;
        _save.IsEnabled = pending;
        _discard.IsEnabled = pending;
        _add.IsEnabled = editable;
        _delete.IsEnabled = editable;
        _warnedClosing &= pending;
    }

    void SetStatus(string text, bool error = false)
    {
        _status.Text = text;
        _status.Foreground = error ? Brushes.Firebrick : Brushes.DimGray;
    }
}
