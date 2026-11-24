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

    private bool _loginServerSingleDomainMode;
    public bool LoginServerSingleDomainMode
    {
        get => _loginServerSingleDomainMode;
        set => this.RaiseAndSetIfChanged(ref _loginServerSingleDomainMode, value);
    }

    private int _loginServerProtocol;
    public int LoginServerProtocol
    {
        get => _loginServerProtocol;
        set => this.RaiseAndSetIfChanged(ref _loginServerProtocol, value);
    }

    private string _loginServerHostname = "";
    public string LoginServerHostname
    {
        get => _loginServerHostname;
        set => this.RaiseAndSetIfChanged(ref _loginServerHostname, value);
    }

    private bool _changeXalAuthAddress;
    public bool ChangeXalAuthAddress
    {
        get => _changeXalAuthAddress;
        set => this.RaiseAndSetIfChanged(ref _changeXalAuthAddress, value);
    }

    private int _xalProtocol;
    public int XalProtocol
    {
        get => _xalProtocol;
        set => this.RaiseAndSetIfChanged(ref _xalProtocol, value);
    }

    private string _xalHostname = "";
    public string XalHostname
    {
        get => _xalHostname;
        set => this.RaiseAndSetIfChanged(ref _xalHostname, value);
    }

    private bool _forceInAppWebView;
    public bool ForceInAppWebView
    {
        get => _forceInAppWebView;
        set => this.RaiseAndSetIfChanged(ref _forceInAppWebView, value);
    }

    private bool _forceInteractiveSignIn;
    public bool ForceInteractiveSignIn
    {
        get => _forceInteractiveSignIn;
        set => this.RaiseAndSetIfChanged(ref _forceInteractiveSignIn, value);
    }

    private bool _changePlayfabApiAddress;
    public bool ChangePlayfabApiAddress
    {
        get => _changePlayfabApiAddress;
        set => this.RaiseAndSetIfChanged(ref _changePlayfabApiAddress, value);
    }

    private int _playfabApiProtocol;
    public int PlayfabApiProtocol
    {
        get => _playfabApiProtocol;
        set => this.RaiseAndSetIfChanged(ref _playfabApiProtocol, value);
    }

    private string _playfabApiHostname = "";
    public string PlayfabApiHostname
    {
        get => _playfabApiHostname;
        set => this.RaiseAndSetIfChanged(ref _playfabApiHostname, value);
    }

    private bool _changeXboxABAddress;
    public bool ChangeXboxABAddress
    {
        get => _changeXboxABAddress;
        set => this.RaiseAndSetIfChanged(ref _changeXboxABAddress, value);
    }

    private int _xboxABProtocol;
    public int XboxABProtocol
    {
        get => _xboxABProtocol;
        set => this.RaiseAndSetIfChanged(ref _xboxABProtocol, value);
    }

    private string _xboxABHostname = "";
    public string XboxABHostname
    {
        get => _xboxABHostname;
        set => this.RaiseAndSetIfChanged(ref _xboxABHostname, value);
    }

    private bool _changeXboxLiveAddress;
    public bool ChangeXboxLiveAddress
    {
        get => _changeXboxLiveAddress;
        set => this.RaiseAndSetIfChanged(ref _changeXboxLiveAddress, value);
    }

    private int _xboxLiveProtocol;
    public int XboxLiveProtocol
    {
        get => _xboxLiveProtocol;
        set => this.RaiseAndSetIfChanged(ref _xboxLiveProtocol, value);
    }

    private string _xboxLiveHostname = "";
    public string XboxLiveHostname
    {
        get => _xboxLiveHostname;
        set => this.RaiseAndSetIfChanged(ref _xboxLiveHostname, value);
    }

    public ReactiveCommand<Unit, Unit> PickIpaCommand { get; }

    public IosPatchOptionsViewModel()
    {
        LocatorProtocol = (int)ProtocolEnum.Http;
        LoginServerProtocol = (int)ProtocolEnum.Http;
        XalProtocol = (int)ProtocolEnum.Http;
        PlayfabApiProtocol = (int)ProtocolEnum.Http;
        XboxABProtocol = (int)ProtocolEnum.Http;
        XboxLiveProtocol = (int)ProtocolEnum.Http;
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
        LoginServerSingleDomainMode = true;

        ChangeXalAuthAddress = true;
        ForceInAppWebView = true;
        ForceInteractiveSignIn = true;
        ChangePlayfabApiAddress = true;
        ChangeXboxABAddress = true;
        ChangeXboxLiveAddress = true;
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

        if (LoginServerSingleDomainMode)
        {
            if ((ChangeXalAuthAddress || ChangePlayfabApiAddress || ChangeXboxABAddress || ChangeXboxLiveAddress)
                && string.IsNullOrWhiteSpace(LoginServerHostname))
            {
                await DialogHelper.ShowError("Enter an auth server hostname/IP first");
                return null;
            }
        }
        else
        {
            if (ChangeXalAuthAddress && string.IsNullOrWhiteSpace(XalHostname))
            {
                await DialogHelper.ShowError("Enter a Xal auth hostname/IP first");
                return null;
            }

            if (ChangePlayfabApiAddress && string.IsNullOrWhiteSpace(PlayfabApiHostname))
            {
                await DialogHelper.ShowError("Enter a PlayFab API hostname/IP first");
                return null;
            }

            if (ChangeXboxABAddress && string.IsNullOrWhiteSpace(XboxABHostname))
            {
                await DialogHelper.ShowError("Enter an XboxAB hostname/IP first");
                return null;
            }

            if (ChangeXboxLiveAddress && string.IsNullOrWhiteSpace(XboxLiveHostname))
            {
                await DialogHelper.ShowError("Enter an Xbox live hostname/IP first");
                return null;
            }
        }

        var ipaOptions = new Core.IpaProcessor.Options()
        {
            Autonomous = true,
            InIpa = ipaPath,
            OutIpa = PlatformService.Current.GetDefaultOutputPath("Solace.ipa"),
            DecodedDir = "IpaDecoded",
            Protocol = LoginServerProtocol,
            Hostname = LoginServerHostname,
            LocatorProtocol = LocatorProtocol,
            LocatorHostname = LocatorHostname,
            AppName = AppName,
            ChangeLocatorAddress = ChangeLocatorAddress,
            ChangeXalAuthAddress = ChangeXalAuthAddress,
            ForceInAppWebView = ForceInAppWebView,
            ForceInteractiveSignIn = ForceInteractiveSignIn,
            ChangePlayfabApiAddress = ChangePlayfabApiAddress,
            ChangeXboxABAddress = ChangeXboxABAddress,
            ChangeXboxLiveAddress = ChangeXboxLiveAddress,
            DisableSunsetTimeCheck = DisableSunsetTimeCheck,
            DisableSpeedLimit = DisableSpeedLimit,
            ChangeAppName = ChangeAppName,
            RemoveDrm = RemoveDrm,
        };

        if (!LoginServerSingleDomainMode)
        {
            ipaOptions.XalProtocol = XalProtocol;
            ipaOptions.XalHostname = XalHostname;
            ipaOptions.PlayfabApiProtocol = PlayfabApiProtocol;
            ipaOptions.PlayfabApiHostname = PlayfabApiHostname;
            ipaOptions.XboxABProtocol = XboxABProtocol;
            ipaOptions.XboxABHostname = XboxABHostname;
            ipaOptions.XboxLiveProtocol = XboxLiveProtocol;
            ipaOptions.XboxLiveHostname = XboxLiveHostname;
        }

        return ipaOptions;
    }
}
