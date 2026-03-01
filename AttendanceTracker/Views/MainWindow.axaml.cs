using AttendanceTracker.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;

namespace AttendanceTracker.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ImportExcel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Import excel",
                AllowMultiple = false
            });

            if(files.Count >= 1)
            {
                var file = files.First();
                
                if(DataContext is MainWindowViewModel vm)
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