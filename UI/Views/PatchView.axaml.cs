using Avalonia.Controls;
using Avalonia.Interactivity;
using MCEPatcher.Core;
using MCEPatcher.UI.ViewModels;
using System;
using System.Diagnostics;
using System.IO;

namespace MCEPatcher.UI.Views
{
    public partial class PatchView : UserControl
    {
        public PatchView()
        {
            InitializeComponent();
            PatchViewModel model = new PatchViewModel();
            DataContext = model;
        }

        public void Patch(ApkProcessor.Options options)
        {
            (DataContext as PatchViewModel)?.Start(options, scrollViewer, chat, finishedContainer);
        }

        public void Back(object sender, RoutedEventArgs args)
        {
            MainWindow.Instance.OpenMainView();
        }

        public void OpenPatchedAPKLocation(object sender, RoutedEventArgs args)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = "explorer";
                    info.Arguments = $"/e, /select, \"{Path.GetFullPath("Minecraft_Earth_patched.apk")}\"";
                    Process.Start(info);
                }
                else
                {
                    Process.Start(Environment.CurrentDirectory);
                }
            }
            catch { }
        }

        /*
         * TODO: once done show button "show patched apk"
         * on windows:
         * string p = @"C:\tmp\this path contains spaces, and,commas\target.txt";
         * string args = string.Format("/e, /select, \"{0}\"", p);
         *
         * ProcessStartInfo info = new ProcessStartInfo();
         * info.FileName = "explorer";
         * info.Arguments = args;
         * Process.Start(info);
         * on linux:
         * Process.Start(@"c:\temp");
         */
    }
}
