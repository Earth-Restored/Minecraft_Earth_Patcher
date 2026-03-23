using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MCEPatcher.Core;
using Serilog;
using System;
using System.Threading.Tasks;

namespace MCEPatcher.UI.ViewModels
{
    public class PatchViewModel : ViewModelBase
    {
        private ScrollViewer scrollViewer;
        private StackPanel panel;
        private bool patchResult;

        public void Start(ApkProcessor.Options options, ScrollViewer _scrollViewer, StackPanel _panel, Grid finishedContainer)
        {
            scrollViewer = _scrollViewer;
            panel = _panel;

            App.OnLogWritten += onLogWritten;

            Task task = Task.Run(async () =>
            {
                try
                {
                    patchResult = await ApkProcessor.Run(options);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    patchResult = false;
                }
            });
            task.ContinueWith(t =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    finishedContainer.IsVisible = true;
                    scrollViewer.ScrollToEnd();
                    if (!patchResult) finishedContainer.Children[1].IsVisible = false;
                });
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