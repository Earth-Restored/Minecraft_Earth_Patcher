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
    private string? ipaFile;
    public string? IpaFile
    {
        get => "IPA File: " + (StringUtils.LimitLengthMiddle(ipaFile, 60) ?? "Not selected");
        set => this.RaiseAndSetIfChanged(ref ipaFile, value);
    }
    public string? IpaFilePath
    {
        get => ipaFile;
        set => this.RaiseAndSetIfChanged(ref ipaFile, value);
    }

    private bool isSimpleMode;
    public bool IsSimpleMode
    {
        get => isSimpleMode;
        set
        {
            this.RaiseAndSetIfChanged(ref isSimpleMode, value);
            if (value)
            {
                EnforceSimpleModeDefaults();
            }
        }
    }

    private bool changeLocatorAddress;
    public bool ChangeLocatorAddress
    {
        get => changeLocatorAddress;
        set => this.RaiseAndSetIfChanged(ref changeLocatorAddress, value);
    }

    private bool disableSunsetTimeCheck;
    public bool DisableSunsetTimeCheck
    {
        get => disableSunsetTimeCheck;
        set => this.RaiseAndSetIfChanged(ref disableSunsetTimeCheck, value);
    }

    private bool disableSpeedLimit;
    public bool DisableSpeedLimit
    {
        get => disableSpeedLimit;
        set => this.RaiseAndSetIfChanged(ref disableSpeedLimit, value);
    }

    private bool changeAppName;
    public bool ChangeAppName
    {
        get => changeAppName;
        set => this.RaiseAndSetIfChanged(ref changeAppName, value);
    }

    private bool removeDrm;
    public bool RemoveDrm
    {
        get => removeDrm;
        set => this.RaiseAndSetIfChanged(ref removeDrm, value);
    }

    private int locatorProtocol;
    public int LocatorProtocol
    {
        get => locatorProtocol;
        set => this.RaiseAndSetIfChanged(ref locatorProtocol, value);
    }

    private string locatorHostname = "";
    public string LocatorHostname
    {
        get => locatorHostname;
        set => this.RaiseAndSetIfChanged(ref locatorHostname, value);
    }

    private string appName = "Solace";
    public string AppName
    {
        get => appName;
        set => this.RaiseAndSetIfChanged(ref appName, value);
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
        string? ipaPath = IpaFilePath;

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
