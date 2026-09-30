// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Startup;
using Arca.Application.Storage;
using Arca.Testing;
using Arca.UI.Actions;
using Arca.UI.Access;
using Arca.Application.Search;
using Arca.UI.Map;
using Arca.UI.Search;
using Arca.Application.GlobalState;
using Arca.UI.Shell;
using Arca.UI.Confirmation;
using Arca.UI.Lists;
using Arca.UI.Notifications;
using Arca.UI.Info;
using Arca.UI.Startup;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>
/// Not a check: renders the screens to PNG files so the look can be reviewed without opening the application.
/// Runs only with ARCA_SCREENSHOT=&lt;folder&gt; (for example: ARCA_SCREENSHOT=/tmp/arca dotnet test).
/// </summary>
public sealed class ScreenshotTests
{
    static readonly ILocalizer _localizer = new ResxLocalizer();

    internal static void Take(Window window, string name)
    {
        var folder = Environment.GetEnvironmentVariable("ARCA_SCREENSHOT");
        Assert.SkipWhen(folder is null, "Set ARCA_SCREENSHOT=<folder> to render screenshots");
        Directory.CreateDirectory(folder!);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        window.CaptureRenderedFrame()!.Save(Path.Combine(folder!, name + ".png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
    }

    [AvaloniaFact]
    public void Main_window_with_information()
    {
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new InfoView(new InfoViewModel(new AppInfo("0.1.0-dev", "20260924104538_InitialCreate"), _localizer)));
        content.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(24, 0),
            Children =
            {
                new Button { Content = "Acció principal", IsDefault = true },
                new Button { Content = "Secundària" },
                new Button { Content = "Desactivada", IsEnabled = false },
                new TextBox { Text = "Cerca un alumne", Width = 200 },
                new CheckBox { Content = "Inclou baixes", IsChecked = true },
                new ComboBox { ItemsSource = new[] { "1r ESO", "2n ESO" }, SelectedIndex = 0, Width = 120 },
            },
        });
        Take(new Window { Width = 900, Height = 300, Content = content, Title = "ARCA" }, "main");
    }

    [AvaloniaFact]
    public void Splash_loading_and_error()
    {
        var loading = new SplashViewModel(_localizer);
        loading.Show(new StartupProgress("Startup.Stage.Migrating", 4, 4));
        Take(new SplashWindow(loading), "splash-loading");

        var failed = new SplashViewModel(_localizer);
        failed.ShowError(StorageErrors.PathNotAccessible("/dades/arca.db"));
        Take(new SplashWindow(failed), "splash-error");
    }

    [AvaloniaFact]
    public void Confirmation_destructive()
    {
        var request = new ConfirmationRequest(
            "Dona de baixa la taquilla", "La taquilla 15 es donarà de baixa. Aquesta acció no es pot desfer.", "Dona de baixa", true, ["1 taquilla"]);
        Take(new ConfirmationWindow(new ConfirmationViewModel(request, _localizer)), "confirmation");
    }

    [AvaloniaFact]
    public void Notifications_and_empty_state()
    {
        var center = new NotificationCenter(new FakeClock(DateTimeOffset.UtcNow), new ManualDelay());
        center.Publish(NotificationKind.Success, "S'ha condonat 1 càrrec, per un total de 50,00 €.");
        center.Publish(NotificationKind.Warning, "Càrrecs pendents de cursos anteriors: 2, per un total de 70,00 €.");
        center.Publish(NotificationKind.Error, "Error inesperat. Referència 4F2A.", "Referència 4F2A · System.IOException");

        var state = new ListStateViewModel(_localizer);
        state.ShowEmpty("Encara no hi ha zones. Crea'n una per començar.", new EmptyStateAction("Afegeix la primera zona", new NoOpCommand()));
        var grid = new Grid();
        grid.Children.Add(new ListStateView(state));
        grid.Children.Add(new NotificationHostView(center, _localizer));
        Take(new Window { Width = 900, Height = 520, Content = grid, Title = "ARCA" }, "notifications");
    }

    [AvaloniaFact]
    public void Focus_ring_and_actions()
    {
        var registry = new ActionRegistry(_localizer, UiPlatform.MacOS);
        registry[StandardActions.Search].Attach(() => { });
        var release = new AppAction("Release", "Allibera la taquilla");
        release.Attach(() => { }, () => Availability.Unavailable("La taquilla no té cap alumne"));
        var name = new TextBox { Text = "Marta", Width = 200 };
        var search = ActionControls.Button(registry[StandardActions.Search]);
        var disabled = ActionControls.Button(release);
        var save = new Button { Content = "Desa", IsDefault = true };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Margin = new Thickness(24), Children = { name, search, disabled, save } };
        var window = new Window { Width = 700, Height = 120, Content = panel, Title = "ARCA" };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        search.Focus(Avalonia.Input.NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Take(window, "focus");
    }

    sealed record DemoStudent(Guid Id, string Name, string Group, int Locker);

    [AvaloniaFact]
    public void Virtualized_list()
    {
        var model = new ListViewModel<DemoStudent, Guid>(
            [
                new ListColumn<DemoStudent>("name", "Alumne", s => s.Name, Width: 3),
                new ListColumn<DemoStudent>("group", "Grup", s => s.Group, Width: 1),
                new ListColumn<DemoStudent>("locker", "Taquilla", s => s.Locker.ToString(System.Globalization.CultureInfo.InvariantCulture), s => s.Locker, Width: 1),
            ],
            s => s.Id, _localizer);
        model.SetItems(Enumerable.Range(1, 300).Select(i => new DemoStudent(Guid.NewGuid(), $"Alumne {i:000} Garcia", i % 2 == 0 ? "1r A" : "1r B", i)));
        model.SortBy("locker");
        model.SortBy("locker");
        foreach (var row in model.Rows.Take(3))
        {
            model.SetSelected(row, true);
        }

        Take(new Window { Width = 700, Height = 360, Content = new VirtualizedListView<DemoStudent, Guid>(model), Title = "ARCA" }, "list");
    }

    [AvaloniaFact]
    public void Shell_frame()
    {
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>());
        var preferences = new Arca.UI.Preferences.UiPreferencesSession(new EmptyPreferences());
        var navigation = new NavigationViewModel(registry, preferences, s => SectionPlaceholder.Create(s, registry, _localizer));
        navigation.Navigate("Students");
        var shell = new ShellView(navigation, _localizer);
        shell.HeaderSlot.Content = Arca.UI.Common.ThemedText.Title("ARCA");
        Take(new Window { Width = 1024, Height = 640, Content = shell, Title = "ARCA" }, "shell");

        var folded = new NavigationViewModel(registry, preferences, s => SectionPlaceholder.Create(s, registry, _localizer)) { IsSidebarCollapsed = true };
        Take(new Window { Width = 1024, Height = 640, Content = new ShellView(folded, _localizer), Title = "ARCA" }, "shell-collapsed");
    }

    sealed class EmptyPreferences : Arca.Application.Preferences.IUiPreferencesStore
    {
        public Arca.Application.Preferences.UiPreferences Load() => new();

        public void Save(Arca.Application.Preferences.UiPreferences preferences)
        {
        }
    }

    [AvaloniaFact]
    public void Password_with_the_eye_button()
    {
        var password = new FormField("Contrasenya del centre", true) { Text = "gat ratllat" };
        var model = new AccessFormViewModel("Obre les dades", "Escriu la contrasenya del centre.", "Obre", "Cancel·la", "Comprovant…", [password], _ => Task.FromResult(false))
        {
            ShowPasswordLabel = "Mostra la contrasenya",
            HidePasswordLabel = "Amaga la contrasenya",
        };
        Take(new AccessWindow(model), "password");
    }

    [AvaloniaFact]
    public void Shell_with_the_state_of_the_application()
    {
        var registry = SectionRegistry.Compose(
            new Dictionary<string, Func<Control>>(), new Dictionary<string, Func<int>> { [ShellCatalog.Students] = () => 12 });
        var navigation = new NavigationViewModel(registry, new Arca.UI.Preferences.UiPreferencesSession(new EmptyPreferences()), s => SectionPlaceholder.Create(s, registry, _localizer));
        var state = new GlobalStateService(_ => Task.FromResult(Arca.Domain.Common.Result<GlobalState>.Success(new GlobalState(null, 12))),
            new Arca.UI.Notifications.ResultNotifier(new Arca.Testing.RecordingNotifications(), _localizer, new Arca.Testing.RecordingErrorLog()));
        state.RefreshAsync().GetAwaiter().GetResult();
        var shell = new ShellView(navigation, _localizer);
        var header = new StackPanel();
        header.Children.Add(new HeaderView(state, _localizer, "ARCA"));
        header.Children.Add(new NoticeBarView(new GlobalNoticesViewModel(state, navigation, _localizer), _localizer));
        shell.HeaderSlot.Content = header;
        Take(new Window { Width = 1024, Height = 640, Content = shell, Title = "ARCA" }, "shell-state");
    }

    [AvaloniaFact]
    public void Search_box_with_results()
    {
        var registry = SectionRegistry.Compose(new Dictionary<string, Func<Control>>());
        var navigation = new NavigationViewModel(registry, new Arca.UI.Preferences.UiPreferencesSession(new EmptyPreferences()), s => SectionPlaceholder.Create(s, registry, _localizer));
        var result = new GlobalSearchResult(
            [new StudentHit(Guid.NewGuid(), "Marta", "García Puig", "1r ESO", "A", 15, false, true, 70m), new StudentHit(Guid.NewGuid(), "Pau", "García Abad", "2n ESO", "B", null, false, false, 0m)], 2,
            [new LockerHit(Guid.NewGuid(), 15, "Planta 1", LockerStatusView.Occupied, "Marta García Puig")], 1, [], 0, 0);
        var delay = new Arca.Testing.ManualDelay();
        var model = new GlobalSearchViewModel((_, _) => Task.FromResult(Arca.Domain.Common.Result<GlobalSearchResult>.Success(result)), delay,
            new Arca.UI.Notifications.ResultNotifier(new Arca.Testing.RecordingNotifications(), _localizer, new Arca.Testing.RecordingErrorLog()), new SearchNavigator(navigation), _localizer);
        var box = new SearchBoxView(model, _localizer);
        var window = new Window { Width = 800, Height = 420, Content = new StackPanel { Margin = new Thickness(24), Children = { box } }, Title = "ARCA" };
        model.Text = "garcia";
        delay.Elapse(GlobalSearchViewModel.Pause + TimeSpan.FromMilliseconds(1)); // the pause is over: the search runs
        Take(window, "search");
    }

    [AvaloniaFact]
    public void Locker_map()
    {
        var random = new Random(7);
        var zones = new List<Arca.Application.LockerMap.ZoneMap>();
        for (var z = 1; z <= 2; z++)
        {
            var lockers = Enumerable.Range(1, 36).Select(i =>
            {
                var status = random.Next(10) switch { < 5 => LockerStatusView.Occupied, < 8 => LockerStatusView.Free, 8 => LockerStatusView.Reserved, _ => i % 2 == 0 ? LockerStatusView.Broken : LockerStatusView.Maintenance };
                return new Arca.Application.LockerMap.MapLocker(Guid.NewGuid(), z * 100 + i, status, status == LockerStatusView.Occupied ? Guid.NewGuid() : null, status == LockerStatusView.Occupied ? "Marta Puig" : null, status == LockerStatusView.Occupied && random.Next(4) == 0);
            }).ToList();
            zones.Add(new Arca.Application.LockerMap.ZoneMap(Guid.NewGuid(), "Planta " + z, lockers, Arca.Application.LockerMap.GetLockerMapHandler.Count(lockers)));
        }

        var data = new Arca.Application.LockerMap.LockerMapData(zones, Arca.Application.LockerMap.GetLockerMapHandler.Count(zones.SelectMany(z => z.Lockers)));
        var model = new LockerMapViewModel(_ => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.LockerMap.LockerMapData>.Success(data)),
            (_, _) => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.LockerMap.MapLocker?>.Success(null)),
            new Arca.UI.Preferences.UiPreferencesSession(new EmptyPreferences()),
            new Arca.UI.Notifications.ResultNotifier(new Arca.Testing.RecordingNotifications(), _localizer, new Arca.Testing.RecordingErrorLog()), _localizer);
        model.LoadAsync().GetAwaiter().GetResult();
        model.Reveal(zones[0].Lockers[9].LockerId);
        var view = new ScreenView("Mapa de taquilles", [], new LockerMapView(model, _localizer));
        Take(new Window { Width = 1024, Height = 640, Content = view, Title = "ARCA" }, "map");
    }

    [AvaloniaFact]
    public void Start_screen_with_detail_and_students()
    {
        var random = new Random(3);
        var lockers = Enumerable.Range(1, 60).Select(i => new Arca.Application.LockerMap.MapLocker(
            Guid.NewGuid(), i, i % 7 == 0 ? LockerStatusView.Free : i % 11 == 0 ? LockerStatusView.Broken : i % 13 == 0 ? LockerStatusView.Reserved : LockerStatusView.Occupied,
            null, null, false)).Select(l => l.Status == LockerStatusView.Occupied ? l with { StudentId = Guid.NewGuid(), StudentName = "Marta Puig", HasDebt = random.Next(4) == 0 } : l).ToList();
        var zone = new Arca.Application.LockerMap.ZoneMap(Guid.NewGuid(), "Planta baixa", lockers, Arca.Application.LockerMap.GetLockerMapHandler.Count(lockers));
        var data = new Arca.Application.LockerMap.LockerMapData([zone], zone.Counters);
        var students = new Arca.Application.Students.StudentListing(
            [.. new[] { ("Aina", "Abad"), ("Biel", "Bosch"), ("Carla", "Camps"), ("Dani", "Costa") }.Select(n => new Arca.Application.Students.StudentRow(Guid.NewGuid(), n.Item1, n.Item2, "1r ESO", "A", false, null))],
            new Arca.Application.Students.StudentCounters(60, 56, 4));
        var target = lockers.First(l => l.Status == LockerStatusView.Occupied && l.HasDebt);
        var services = new Arca.UI.Map.LockerHomeServices(
            _ => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.LockerMap.LockerMapData>.Success(data)),
            (_, _) => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.LockerMap.MapLocker?>.Success(null)),
            (id, _) => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.LockerMap.LockerDetail?>.Success(new Arca.Application.LockerMap.LockerDetail(id, target.Number, "Planta baixa", target.Status, null, null, target.StudentId, target.StudentName, "2n ESO", "B", true, 70m))),
            _ => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.Students.StudentListing>.Success(students)),
            (_, _, _) => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.Assignments.CheckAssignmentTarget.AssignmentTargetCheck>.Success(new(null, []))),
            (_, _) => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.Assignments.AssignLocker.AssignLockerResult>.Failure(new Arca.Domain.Common.Error("X.Y"))),
            (_, _) => Task.FromResult(Arca.Domain.Common.Result<Arca.Application.Assignments.AssignLocker.AssignLockerResult>.Failure(new Arca.Domain.Common.Error("X.Y"))),
            new Arca.UI.Map.LockerOperations(Op, Op, Op, Op, Op));
        var notifier = new Arca.UI.Notifications.ResultNotifier(new Arca.Testing.RecordingNotifications(), _localizer, new Arca.Testing.RecordingErrorLog());
        var state = new GlobalStateService(_ => Task.FromResult(Arca.Domain.Common.Result<GlobalState>.Success(new(null, 0))), notifier);
        var home = new Arca.UI.Map.LockerHomeModel(services, new Arca.UI.Preferences.UiPreferencesSession(new EmptyPreferences()), notifier, new Arca.Testing.RecordingConfirmations(true), _localizer, new Arca.Testing.RecordingNotifications(), new Arca.Testing.RecordingErrorLog(), new Arca.Testing.ManualDelay(), state);
        home.LoadAsync().GetAwaiter().GetResult();
        home.Map.Select(target.LockerId);
        home.Detail.ShowAsync(target.LockerId).GetAwaiter().GetResult();
        var screen = new Arca.UI.Map.LockerMapHomeScreen(home, new SearchNavigator(), _localizer).Create();
        Take(new Window { Width = 1180, Height = 700, Content = screen, Title = "ARCA" }, "start");
    }

    static Task<Arca.Domain.Common.Result<string>> Op(Guid id, CancellationToken ct) => Task.FromResult(Arca.Domain.Common.Result<string>.Success("fet"));
}
