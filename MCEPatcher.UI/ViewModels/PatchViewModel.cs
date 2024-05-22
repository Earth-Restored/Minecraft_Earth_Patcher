using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MCEPatcher.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MCEPatcher.UI.ViewModels
{
    public class PatchViewModel : ViewModelBase
    {
        private ScrollViewer scrollViewer;
        private StackPanel panel;
        private bool patchResult;

        public void Start(ApkProcessor.Options options, ScrollViewer _scrollViewer, StackPanel _panel)
        {
            scrollViewer = _scrollViewer;
            panel = _panel;

            App.OnLogWritten += onLogWritten;

            Task task = Task.Run(() =>
            {
                patchResult = ApkProcessor.Run(options);
            });
            task.ContinueWith(t =>
            {
                Dispatcher.UIThread.Invoke(() => scrollViewer.ScrollToEnd());
            });
        }

        private void onLogWritten(string? text)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                SelectableTextBlock block = new SelectableTextBlock()
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(0, 0, 0, 2)
                };
                panel.Children.Add(block);
                scrollViewer.ScrollToEnd();
            });
        }

        public override void OnClose()
        {
            App.OnLogWritten -= onLogWritten;
        }
    }
}