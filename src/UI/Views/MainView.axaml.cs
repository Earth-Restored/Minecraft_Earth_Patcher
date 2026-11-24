using Avalonia.Controls;
using MCEPatcher.UI.ViewModels;

namespace MCEPatcher.UI.Views;

public sealed partial class MainView : UserControl
{
    public MainView() : this(new MainViewModel())
    {
    }

    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
