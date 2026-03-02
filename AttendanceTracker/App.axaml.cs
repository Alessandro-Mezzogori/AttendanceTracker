using AttendanceTracker;
using AttendanceTracker.Data.Services;
using AttendanceTracker.ViewModels;
using AttendanceTracker.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace AttendanceTracker;

public partial class App : Application
{
    public static ILogger<Application> Logger = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var loggerFactory = LoggerFactory.Create(b => b
            .SetMinimumLevel(LogLevel.Information)
            .ClearProviders()
            .AddConsole());

        this.AttachDeveloperTools(o =>
        {
            o.AddMicrosoftLoggerObservable(loggerFactory);
        });

        Logger = loggerFactory.CreateLogger<Application>();
    }

    private IServiceProvider SetupDependencyInjection()
    {

        var collection = new ServiceCollection();

        // ViewModels
        collection.AddTransient<MainWindowViewModel>();
        collection.AddDataServices();


        var services = collection.BuildServiceProvider();

        return services;
    }

    private void SetupDatabase(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        using var ctx = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        ctx.Database.Migrate();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var _provider = SetupDependencyInjection();

        SetupDatabase(_provider);

        var vm = _provider.GetRequiredService<MainWindowViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}

public class NotificationService
{
    private Window _window;
    private WindowNotificationManager _manager;

    public NotificationService(Window window)
    {
        _window = window;
        _manager = new WindowNotificationManager(window)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3
        }; ;
    }

    public void Show(string title, string message, NotificationType type = NotificationType.Information, TimeSpan? expiration = null)
    {
        expiration ??= TimeSpan.FromSeconds(5);

        var notification = type switch {
            NotificationType.Error => new Notification(title, message, type, expiration, onClick: async () =>
            {
                if(_window.Clipboard is not null)
                {
                    await _window.Clipboard.SetTextAsync(message);
                    this.Show("Copied!", string.Empty);
                }
            }),
            _ => new Notification(title, message, type, expiration),
        };


        _manager.Show(notification);
    }
}