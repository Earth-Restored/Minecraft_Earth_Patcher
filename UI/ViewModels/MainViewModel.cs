using MCEPatcher.UI.Models;
using MCEPatcher.UI.Utils;
using ReactiveUI;
using System.Collections.Generic;

namespace MCEPatcher.UI.ViewModels;

/*
 * color definitions from: https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Themes.Fluent/Accents/FluentControlResources.xaml
 */

public class MainViewModel : ViewModelBase
{
    private string? apkFile;
    public string? ApkFile
    {
        get => "APK File: " + (U.LimitLengthMiddle(apkFile, 60) ?? "Not selected");
        set => this.RaiseAndSetIfChanged(ref apkFile, value);
    }

    #region locator
    private bool changeLocatorAddress;
    public bool ChangeLocatorAddress
    {
        get => changeLocatorAddress;
        set => this.RaiseAndSetIfChanged(ref changeLocatorAddress, value);
    }
    private int locatorProtocol;
    public int LocatorProtocol
    {
        get => locatorProtocol;
        set => this.RaiseAndSetIfChanged(ref locatorProtocol, value);
    }
    private string locatorHostname;
    public string LocatorHostname
    {
        get => locatorHostname;
        set => this.RaiseAndSetIfChanged(ref locatorHostname, value);
    }
    #endregion

    private bool disableSunsetTimeCheck;
    public bool DisableSunsetTimeCheck
    {
        get => disableSunsetTimeCheck;
        set => this.RaiseAndSetIfChanged(ref disableSunsetTimeCheck, value);
    }

    private bool disableLicenseCheck;
    public bool DisableLicenseCheck
    {
        get => disableLicenseCheck;
        set => this.RaiseAndSetIfChanged(ref disableLicenseCheck, value);
    }

    private bool disableTelemetry;
    public bool DisableTelemetry
    {
        get => disableTelemetry;
        set => this.RaiseAndSetIfChanged(ref disableTelemetry, value);
    }

    private bool disableMsaLoginSignatureValidation;
    public bool DisableMsaLoginSignatureValidation
    {
        get => disableMsaLoginSignatureValidation;
        set
        {
            this.RaiseAndSetIfChanged(ref disableMsaLoginSignatureValidation, value);
            if (!value) ChangeMSALoginServiceAddress = false;
        }
    }

    #region app_name
    private bool changeAppName;
    public bool ChangeAppName
    {
        get => changeAppName;
        set => this.RaiseAndSetIfChanged(ref changeAppName, value);
    }
    private string appName;
    public string AppName
    {
        get => appName;
        set => this.RaiseAndSetIfChanged(ref appName, value);
    }
    private string appNameShort;
    public string AppNameShort
    {
        get => appNameShort;
        set => this.RaiseAndSetIfChanged(ref appNameShort, value);
    }
    #endregion
    #region package_name
    private bool changePackageName;
    public bool ChangePackageName
    {
        get => changePackageName;
        set => this.RaiseAndSetIfChanged(ref changePackageName, value);
    }
    private string packageName;
    public string PackageName
    {
        get => packageName;
        set => this.RaiseAndSetIfChanged(ref packageName, value);
    }
    #endregion
    #region MSA_login_service
    private bool changeMSALoginServiceAddress;
    public bool ChangeMSALoginServiceAddress
    {
        get => changeMSALoginServiceAddress;
        set
        {
            this.RaiseAndSetIfChanged(ref changeMSALoginServiceAddress, value);
            if (value) DisableMsaLoginSignatureValidation = true;
        }
    }
    private int _MSALoginServiceProtocol;
    public int MSALoginServiceProtocol
    {
        get => _MSALoginServiceProtocol;
        set => this.RaiseAndSetIfChanged(ref _MSALoginServiceProtocol, value);
    }
    private string _MSALoginServiceHostname;
    public string MSALoginServiceHostname
    {
        get => _MSALoginServiceHostname;
        set => this.RaiseAndSetIfChanged(ref _MSALoginServiceHostname, value);
    }
    #endregion
    #region playfab_api
    private bool changePlayfabApiAddress;
    public bool ChangePlayfabApiAddress
    {
        get => changePlayfabApiAddress;
        set => this.RaiseAndSetIfChanged(ref changePlayfabApiAddress, value);
    }
    private int playfabApiProtocol;
    public int PlayfabApiProtocol
    {
        get => playfabApiProtocol;
        set => this.RaiseAndSetIfChanged(ref playfabApiProtocol, value);
    }
    private string playfabApiHostname;
    public string PlayfabApiHostname
    {
        get => playfabApiHostname;
        set => this.RaiseAndSetIfChanged(ref playfabApiHostname, value);
    }
    #endregion
    #region xboxab
    private bool changeXboxABAddress;
    public bool ChangeXboxABAddress
    {
        get => changeXboxABAddress;
        set => this.RaiseAndSetIfChanged(ref changeXboxABAddress, value);
    }
    private int xboxABProtocol;
    public int XboxABProtocol
    {
        get => xboxABProtocol;
        set => this.RaiseAndSetIfChanged(ref xboxABProtocol, value);
    }
    private string xboxABHostname;
    public string XboxABHostname
    {
        get => xboxABHostname;
        set => this.RaiseAndSetIfChanged(ref xboxABHostname, value);
    }
    #endregion
    #region xboxlive
    private bool changeXboxLiveAddress;
    public bool ChangeXboxLiveAddress
    {
        get => changeXboxLiveAddress;
        set => this.RaiseAndSetIfChanged(ref changeXboxLiveAddress, value);
    }
    private int xboxLiveProtocol;
    public int XboxLiveProtocol
    {
        get => xboxLiveProtocol;
        set => this.RaiseAndSetIfChanged(ref xboxLiveProtocol, value);
    }
    private string xboxLiveHostname;
    public string XboxLiveHostname
    {
        get => xboxLiveHostname;
        set => this.RaiseAndSetIfChanged(ref xboxLiveHostname, value);
    }
    #endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MainViewModel()
    {
        ChangeLocatorAddress = true;
        LocatorProtocol = (int)ProtocolEnum.Http;
        LocatorHostname = "192.168.1.x";

        DisableSunsetTimeCheck = true;
        DisableLicenseCheck = true;
        DisableTelemetry = true;
        DisableMsaLoginSignatureValidation = true;

        ChangeAppName = true;
        AppName = "Minecraft Earth Patched";
        AppNameShort = "MCE Patched";

        ChangePackageName = true;
        PackageName = "com.github.bitcodercz";

        ChangeMSALoginServiceAddress = false;
        MSALoginServiceProtocol = (int)ProtocolEnum.Https;
        MSALoginServiceHostname = "live.com";

        ChangePlayfabApiAddress = false;
        PlayfabApiProtocol = (int)ProtocolEnum.Https;
        PlayfabApiHostname = "playfabapi.com";

        ChangeXboxABAddress = false;
        XboxABProtocol = (int)ProtocolEnum.Https;
        XboxABHostname = "xboxab.com";

        ChangeXboxLiveAddress = false;
        XboxLiveProtocol = (int)ProtocolEnum.Https;
        XboxLiveHostname = "xboxlive.com";
    }
#pragma warning restore CS8618

    public IEnumerable<string> GetPatches()
    {
        yield return "fix-official-msa-login-after-signature-change";

        if (DisableSunsetTimeCheck) yield return "disable-sunset-time-check";
        if (DisableLicenseCheck) yield return "disable-license-check";
        if (DisableTelemetry) yield return "disable-telemetry";
        if (DisableMsaLoginSignatureValidation) yield return "disable-msa-login-signature-validation";

        if (ChangeLocatorAddress) yield return "change-locator-address";

        if (ChangeAppName) yield return "change-app-name";
        if (ChangePackageName) yield return "change-package-name";

        if (ChangeMSALoginServiceAddress) yield return "change-msa-login-address";
        if (ChangePlayfabApiAddress) yield return "change-playfab-address";
        if (ChangeXboxABAddress) yield return "change-xboxab-address";
        if (ChangeXboxLiveAddress)
        {
            yield return "change-xboxlive-address-base";
            yield return "change-xboxlive-address-extra";
        }
    }

    public IEnumerable<string> GetVariables()
    {
        if (ChangeLocatorAddress)
        {
            yield return $"locatorprotocol={(ProtocolEnum)LocatorProtocol}";
            yield return $"locatorhostname={LocatorHostname}";
        }

        if (ChangeAppName)
        {
            yield return $"app_name={AppName}";
            yield return $"app_name_short={AppNameShort}";
        }
        if (ChangePackageName)
        {
            yield return $"package_name={PackageName}";
        }

        if (ChangeMSALoginServiceAddress)
        {
            yield return $"liveprotocol={(ProtocolEnum)MSALoginServiceProtocol}";
            yield return $"livehostname={MSALoginServiceHostname}";
        }
        if (ChangePlayfabApiAddress)
        {
            yield return $"playfabprotocol={(ProtocolEnum)PlayfabApiProtocol}";
            yield return $"playfabhostname={PlayfabApiHostname}";
        }
        if (ChangeXboxABAddress)
        {
            yield return $"xboxabprotocol={(ProtocolEnum)XboxABProtocol}";
            yield return $"xboxabhostname={XboxABHostname}";
        }
        if (ChangeXboxLiveAddress)
        {
            yield return $"xboxliveprotocol={(ProtocolEnum)XboxLiveProtocol}";
            yield return $"xboxlivehostname={XboxLiveHostname}";
        }
    }
}
