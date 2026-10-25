using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MCEPatcher.UI.Utils;
using MCEPatcher.UI.ViewModels;
using MCEPatcher.UI.Views;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace MCEPatcher.UI;

public sealed partial class App : Application
{
    internal static event Action<LogEvent>? OnLogWritten;

    public override void Initialize()
    {
        var uiSink = new UiLogSink();
        uiSink.LogEmitted += logEvent => OnLogWritten?.Invoke(logEvent);

        var log = new LoggerConfiguration()
           .WriteTo.Sink(uiSink)
           .WriteTo.File("logs/debug.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 8338607, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
           .MinimumLevel.Debug()
           .CreateLogger();
        Log.Logger = log;

        AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
        {
            Log.Fatal($"Unhandled exception: {e.ExceptionObject}");
            Log.CloseAndFlush();
            Environment.Exit(1);
        };

        AvaloniaXamlLoader.Load(this);
    }

    private static readonly Stack<Control> backStack = new();
    private static ContentControl? rootContentControl;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            rootContentControl = new ContentControl
            {
                Content = new MainView()
            };
            singleViewPlatform.MainView = rootContentControl;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void NavigateTo(Control view, bool rememberInBackStack = true)
    {
        Control? current = null;

        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            current = desktop.MainWindow.Content as Control;
            ((current as UserControl)?.DataContext as ViewModelBase)?.OnClose();
            desktop.MainWindow.Content = view;
        }
        else if (rootContentControl is not null)
        {
            current = rootContentControl.Content as Control;
            ((current as UserControl)?.DataContext as ViewModelBase)?.OnClose();
            rootContentControl.Content = view;
        }

        if (rememberInBackStack && current is not null)
        {
            backStack.Push(current);
        }
    }

    public static void NavigateBack()
    {
        if (backStack.TryPop(out var previousView))
        {
            backStack.Clear();
            NavigateTo(previousView, rememberInBackStack: false);
        }
        else
        {
            NavigateTo(new MainView(), rememberInBackStack: false);
        }
    }

    public static Avalonia.Platform.Storage.IStorageProvider? GetStorageProvider()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.StorageProvider;
        }

        if (Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            return TopLevel.GetTopLevel(singleView.MainView)?.StorageProvider;
        }

        return null;
    }
}
