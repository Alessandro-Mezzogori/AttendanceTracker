using AttendanceTracker.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DocumentFormat.OpenXml.ExtendedProperties;
using System;
using System.Linq;

namespace AttendanceTracker.Views
{
    public partial class MainWindow : Window
    {
        private readonly NotificationService _notificationService;

        public MainWindow()
        {
            _notificationService = new NotificationService(this);

            InitializeComponent();
        }

        private bool _calendarInitialLoad = true;

        private void FilterCalendar_Attached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_calendarInitialLoad == false)
                return;

            if (sender is not Calendar calendar || DataContext is not MainWindowViewModel vm || vm.FilterStartDate is null || vm.FilterEndDate is null)
                return;

            calendar.SelectedDates.Clear();
            for (DateOnly date = (DateOnly)vm.FilterStartDate; date <= vm.FilterEndDate; date = date.AddDays(1))
            {
                calendar.SelectedDates.Add(date.ToDateTime(TimeOnly.MinValue));
            }

            _calendarInitialLoad = false;
        }

        private async void ClearDates_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            this.FilterCalendar.SelectedDates.Clear();
            vm.FilterStartDate = null;
            vm.FilterEndDate = null;

            try
            {
                await vm.LoadAsync();
            }
            catch (Exception ex)
            {
                _notificationService.Show("Load error", $"{ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }

        private async void FilterCalendar_SelectedDatesChanged(object? sender, EventArgs e)
        {
            if (_calendarInitialLoad == true)
                return;

            if (DataContext is not MainWindowViewModel vm)
                return;

            if (this.FilterCalendar.SelectedDates.Count > 0)
            {
                vm.FilterStartDate = DateOnly.FromDateTime(this.FilterCalendar.SelectedDates.Min());
                vm.FilterEndDate = DateOnly.FromDateTime(this.FilterCalendar.SelectedDates.Max());
            }
            else
            {
                vm.FilterStartDate = null;
                vm.FilterEndDate = null;
            }

            try
            {
                await vm.LoadAsync();
            }
            catch (Exception ex)
            {
                _notificationService.Show("Load error", $"{ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }

        private async void ImportExcel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Import excel",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                var file = files[0];

                if (DataContext is MainWindowViewModel vm)
                {
                    try
                    {
                        await vm.ImportExcel(file.Path);
                        await vm.LoadAsync();

                        _notificationService.Show("Import completed", "Import completed successfully", Avalonia.Controls.Notifications.NotificationType.Success);
                    }
                    catch (Exception ex)
                    {
                        _notificationService.Show("Import error", $"Imported ended with error: {Environment.NewLine} {ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
                    }
                }
            }
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (DataContext is MainWindowViewModel vm)
                await vm.LoadAsync();
        }

        private async void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (MainTabs.SelectedItem is TabItem selectedTab && selectedTab.Header?.ToString() == "Dashboard")
            {
                // Call your loading logic here
                try
                {
                    await vm.LoadDashboard();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    _notificationService.Show("Load dashbaord error", $"{ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
                }
            }
            else
            {
                vm.AttendedTime.Clear();
            }
        }

        private async void Backup_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            try
            {
                await vm.BackupDatabase();
                _notificationService.Show("Backup success", $"backup avvenuto con successo", Avalonia.Controls.Notifications.NotificationType.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _notificationService.Show("Backup error", $"{ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }

        private async void Restore_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            try
            {
                var startingStorage = await this.StorageProvider.TryGetFolderFromPathAsync(vm.AppDataFolder());

                var files = await this.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Select backup",
                    AllowMultiple = false,
                    SuggestedStartLocation = startingStorage,
                    FileTypeFilter = [
                        new FilePickerFileType("Attendance tracker backup"){
                            Patterns = ["*.db"]
                        }
                    ]
                });


                if (files.Count >= 1)
                {
                    var file = files[0];

                    await vm.RestoreDatabase(file.Path);
                    _notificationService.Show("restore success", $"backup avvenuto con successo", Avalonia.Controls.Notifications.NotificationType.Success);
                    await vm.LoadAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _notificationService.Show("restore error", $"{ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }

        private async void AppDataFolder_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            try
            {
                var startingStorage = await this.StorageProvider.TryGetFolderFromPathAsync(vm.AppDataFolder());
                await this.Launcher.LaunchFileAsync(startingStorage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _notificationService.Show("appdatafolder open error", $"{ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }
    }
}