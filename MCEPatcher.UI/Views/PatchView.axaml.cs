using Avalonia.Controls;
using MCEPatcher.Core;
using MCEPatcher.UI.ViewModels;

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
            (DataContext as PatchViewModel)?.Start(options, scrollViewer, chat);
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
