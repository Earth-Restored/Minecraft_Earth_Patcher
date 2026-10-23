using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MCEPatcher.UI.Utils;
using MCEPatcher.UI.ViewModels;
using MCEPatcher.UI.Views;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace MCEPatcher.UI;

public sealed partial class App : Application
{
    internal static event Action<string?>? OnLogWritten;

    public override void Initialize()
    {
        StringWriterExt writer = new StringWriterExt()
        {
            NewLine = "\n"
        };
        StringBuilder sb = new StringBuilder();
        writer.OnWrite += (object sender, StringWriterExt.OnWriteEventArgs args) =>
        {
            sb.Append(args.Value);
            string str = sb.ToString();
            int index = str.IndexOf('\n');
            if (index is not -1)
            {
                OnLogWritten?.Invoke(str.Substring(0, index));
                sb = new StringBuilder(str.Substring(index + 1));
            }
        };

        var log = new LoggerConfiguration()
           .WriteTo.TextWriter(writer, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
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

    private static readonly Stack<Control> s_backStack = new();
    private static ContentControl? s_rootContentControl;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            s_rootContentControl = new ContentControl
            {
                Content = new MainView()
            };
            singleViewPlatform.MainView = s_rootContentControl;
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
        else if (s_rootContentControl is not null)
        {
            current = s_rootContentControl.Content as Control;
            ((current as UserControl)?.DataContext as ViewModelBase)?.OnClose();
            s_rootContentControl.Content = view;
        }

        if (rememberInBackStack && current is not null)
        {
            s_backStack.Push(current);
        }
    }

    public static void NavigateBack()
    {
        if (s_backStack.TryPop(out var previousView))
        {
            s_backStack.Clear();
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
