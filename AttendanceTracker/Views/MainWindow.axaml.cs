using AttendanceTracker.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;

namespace AttendanceTracker.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
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

            await vm.LoadAsync();
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
                var file = files.First();

                if (DataContext is MainWindowViewModel vm)
                {
                    var result = await vm.ImportExcel(file.Path);
                }
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            (DataContext as MainWindowViewModel)!.LoadAsync();
        }
    }
}