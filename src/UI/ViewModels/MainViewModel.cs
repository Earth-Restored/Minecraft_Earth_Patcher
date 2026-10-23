using MCEPatcher.UI.Views;
using ReactiveUI;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;

namespace MCEPatcher.UI.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    public AndroidPatchOptionsViewModel AndroidOptions { get; }
    public IosPatchOptionsViewModel IosOptions { get; }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
            this.RaisePropertyChanged(nameof(IsAndroidSelected));
            this.RaisePropertyChanged(nameof(IsIosSelected));
        }
    }

    public bool IsAndroidSelected => _selectedTabIndex == 0;
    public bool IsIosSelected => _selectedTabIndex == 1;

    public ReactiveCommand<Unit, Unit> SelectAndroidCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectIosCommand { get; }
    public ReactiveCommand<Unit, Unit> PatchCommand { get; }

    public MainViewModel()
    {
        AndroidOptions = new AndroidPatchOptionsViewModel();
        IosOptions = new IosPatchOptionsViewModel();

        SelectAndroidCommand = ReactiveCommand.Create(() => { SelectedTabIndex = 0; });
        SelectIosCommand = ReactiveCommand.Create(() => { SelectedTabIndex = 1; });

        PatchCommand = ReactiveCommand.CreateFromTask(ExecutePatchAsync);
    }

    private async Task ExecutePatchAsync()
    {
        if (IsAndroidSelected)
        {
            var options = await AndroidOptions.BuildOptionsAsync();
            if (options is not null)
            {
                App.NavigateTo(new PatchView(options));
            }
        }
        else if (IsIosSelected)
        {
            var options = await IosOptions.BuildOptionsAsync();
            if (options is not null)
            {
                App.NavigateTo(new PatchView(options));
            }
        }
    }
}
