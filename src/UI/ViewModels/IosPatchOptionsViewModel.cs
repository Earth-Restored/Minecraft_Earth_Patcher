using Avalonia.Platform.Storage;
using MCEPatcher.UI.Models;
using MCEPatcher.UI.Services;
using MCEPatcher.UI.Utils;
using ReactiveUI;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace MCEPatcher.UI.ViewModels;

public sealed class IosPatchOptionsViewModel : ViewModelBase
{
    private string? _ipaFile;
    public string? IpaFile
    {
        get => "IPA File: " + (StringUtils.LimitLengthMiddle(_ipaFile, 60) ?? "Not selected");
        set => this.RaiseAndSetIfChanged(ref _ipaFile, value);
    }
    public string? IpaFilePath
    {
        get => _ipaFile;
        set => this.RaiseAndSetIfChanged(ref _ipaFile, value);
    }

    private bool _isSimpleMode;
    public bool IsSimpleMode
    {
        get => _isSimpleMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isSimpleMode, value);
            if (value)
            {
                EnforceSimpleModeDefaults();
            }
        }
    }

    private bool _changeLocatorAddress;
    public bool ChangeLocatorAddress
    {
        get => _changeLocatorAddress;
        set => this.RaiseAndSetIfChanged(ref _changeLocatorAddress, value);
    }

    private bool _disableSunsetTimeCheck;
    public bool DisableSunsetTimeCheck
    {
        get => _disableSunsetTimeCheck;
        set => this.RaiseAndSetIfChanged(ref _disableSunsetTimeCheck, value);
    }

    private bool _disableSpeedLimit;
    public bool DisableSpeedLimit
    {
        get => _disableSpeedLimit;
        set => this.RaiseAndSetIfChanged(ref _disableSpeedLimit, value);
    }

    private bool _changeAppName;
    public bool ChangeAppName
    {
        get => _changeAppName;
        set => this.RaiseAndSetIfChanged(ref _changeAppName, value);
    }

    private bool _removeDrm;
    public bool RemoveDrm
    {
        get => _removeDrm;
        set => this.RaiseAndSetIfChanged(ref _removeDrm, value);
    }

    private int _locatorProtocol;
    public int LocatorProtocol
    {
        get => _locatorProtocol;
        set => this.RaiseAndSetIfChanged(ref _locatorProtocol, value);
    }

    private string _locatorHostname = "";
    public string LocatorHostname
    {
        get => _locatorHostname;
        set => this.RaiseAndSetIfChanged(ref _locatorHostname, value);
    }

    private string _appName = "Solace";
    public string AppName
    {
        get => _appName;
        set => this.RaiseAndSetIfChanged(ref _appName, value);
    }

    public ReactiveCommand<Unit, Unit> PickIpaCommand { get; }

    public IosPatchOptionsViewModel()
    {
        LocatorProtocol = (int)ProtocolEnum.Http;
        IsSimpleMode = true;

        PickIpaCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var path = await FilePickerHelper.PickFileAsync(new FilePickerOpenOptions
            {
                Title = "Minecraft Earth IPA file",
                AllowMultiple = false,
                FileTypeFilter = [ new FilePickerFileType("Ipa")
                {
                    Patterns = ["*.ipa"],
                } ],
            });

            if (path is not null)
            {
                IpaFile = path;
            }
        });
    }

    private void EnforceSimpleModeDefaults()
    {
        ChangeLocatorAddress = true;
        DisableSunsetTimeCheck = true;
        DisableSpeedLimit = true;
        ChangeAppName = true;
        RemoveDrm = true;
    }

    public async Task<Core.IpaProcessor.Options?> BuildOptionsAsync()
    {
        var ipaPath = IpaFilePath;

        if (ipaPath is null)
        {
            await DialogHelper.ShowError("Select the IPA file first");
            return null;
        }

        if (!File.Exists(ipaPath))
        {
            await DialogHelper.ShowError("Selected file doesn't exist");
            return null;
        }

        using (var fs = File.OpenRead(ipaPath))
        {
            if (!Core.IpaProcessor.VerifyHash(fs))
            {
                var result = await DialogHelper.ShowDialogAsync(
                    "Warning",
                    "The .ipa file hash does not match. Patching may fail. Do you want to continue?",
                    MsBox.Avalonia.Enums.Icon.Error,
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo);

                if (result is MsBox.Avalonia.Enums.ButtonResult.No)
                {
                    return null;
                }
            }
        }

        if (ChangeLocatorAddress && string.IsNullOrWhiteSpace(LocatorHostname))
        {
            await DialogHelper.ShowError("Enter a locator hostname/IP first");
            return null;
        }

        var ipaOptions = new Core.IpaProcessor.Options()
        {
            Autonomous = true,
            InIpa = ipaPath,
            OutIpa = PlatformService.Current.GetDefaultOutputPath("Solace.ipa"),
            DecodedDir = "IpaDecoded",
            LocatorProtocol = LocatorProtocol,
            LocatorHostname = LocatorHostname,
            AppName = AppName,
            ChangeLocatorAddress = ChangeLocatorAddress,
            ChangeXalAuthAddress = false,
            ForceInAppWebView = false,
            ForceInteractiveSignIn = false,
            ChangePlayfabApiAddress = false,
            ChangeXboxABAddress = false,
            ChangeXboxLiveAddress = false,
            DisableSunsetTimeCheck = DisableSunsetTimeCheck,
            DisableSpeedLimit = DisableSpeedLimit,
            ChangeAppName = ChangeAppName,
            RemoveDrm = RemoveDrm,
        };

        return ipaOptions;
    }
}
