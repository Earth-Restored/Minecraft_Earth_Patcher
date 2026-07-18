using MCEPatcher.UI.Models;
using MCEPatcher.UI.Utils;
using ReactiveUI;
using System.Collections.Generic;
using System.Reflection;

namespace MCEPatcher.UI.ViewModels;

/*
 * color definitions from: https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Themes.Fluent/Accents/FluentControlResources.xaml
 */

public sealed class MainViewModel : ViewModelBase
{
    public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

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

    private string? apkFile;
    public string? ApkFile
    {
        get => "APK File: " + (U.LimitLengthMiddle(apkFile, 60) ?? "Not selected");
        set => this.RaiseAndSetIfChanged(ref apkFile, value);
    }
    public string? ApkFilePath
    {
        get => apkFile;
        set => this.RaiseAndSetIfChanged(ref apkFile, value);
    }

    private string? resourcePackFile;
    public string? ResourcePackFile
    {
        get => "Resource Pack File: " + (U.LimitLengthMiddle(resourcePackFile, 60) ?? "Not selected");
        set => this.RaiseAndSetIfChanged(ref resourcePackFile, value);
    }
    public string? ResourcePackFilePath
    {
        get => resourcePackFile;
        set => this.RaiseAndSetIfChanged(ref resourcePackFile, value);
    }

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
    private bool loginServerSingleDomainMode;
    public bool LoginServerSingleDomainMode
    {
        get => loginServerSingleDomainMode;
        set => this.RaiseAndSetIfChanged(ref loginServerSingleDomainMode, value);
    }
    private int loginServerProtocol;
    public int LoginServerProtocol
    {
        get => loginServerProtocol;
        set => this.RaiseAndSetIfChanged(ref loginServerProtocol, value);
    }
    private string loginServerHostname;
    public string LoginServerHostname
    {
        get => loginServerHostname;
        set => this.RaiseAndSetIfChanged(ref loginServerHostname, value);
    }
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

    public MainViewModel()
    {
        ChangeLocatorAddress = true;
        LocatorProtocol = (int)ProtocolEnum.Http;
        LocatorHostname = "";
        DisableSunsetTimeCheck = true;
        DisableLicenseCheck = true;
        DisableTelemetry = true;
        DisableMsaLoginSignatureValidation = true;
        ChangeAppName = true;
        AppName = "Solace";
        AppNameShort = "Solace";
        ChangePackageName = true;
        PackageName = "com.github.earth_restored";
        LoginServerSingleDomainMode = true;
        LoginServerProtocol = (int)ProtocolEnum.Http;
        LoginServerHostname = "";
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

        IsSimpleMode = true;
    }

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
            yield return $"locatorprotocol={((ProtocolEnum)LocatorProtocol).ToProtocolString()}://";
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
        if (LoginServerSingleDomainMode)
        {
            if (ChangeMSALoginServiceAddress)
            {
                yield return $"liveprotocol={((ProtocolEnum)LoginServerProtocol).ToProtocolString()}://{LoginServerHostname}/";
                yield return "livehostname=live.com";
            }
            if (ChangePlayfabApiAddress)
            {
                yield return $"playfabprotocol={((ProtocolEnum)LoginServerProtocol).ToProtocolString()}://{LoginServerHostname}/";
                yield return "playfabhostname=playfabapi.com";
            }
            if (ChangeXboxABAddress)
            {
                yield return $"xboxabprotocol={((ProtocolEnum)LoginServerProtocol).ToProtocolString()}://{LoginServerHostname}/";
                yield return "xboxabhostname=xboxab.com";
            }
            if (ChangeXboxLiveAddress)
            {
                yield return $"xboxliveprotocol={((ProtocolEnum)LoginServerProtocol).ToProtocolString()}://{LoginServerHostname}/";
                yield return "xboxlivehostname=xboxlive.com";
            }
        }
        else
        {
            if (ChangeMSALoginServiceAddress)
            {
                yield return $"liveprotocol={((ProtocolEnum)MSALoginServiceProtocol).ToProtocolString()}://";
                yield return $"livehostname={MSALoginServiceHostname}";
            }
            if (ChangePlayfabApiAddress)
            {
                yield return $"playfabprotocol={((ProtocolEnum)PlayfabApiProtocol).ToProtocolString()}://";
                yield return $"playfabhostname={PlayfabApiHostname}";
            }
            if (ChangeXboxABAddress)
            {
                yield return $"xboxabprotocol={((ProtocolEnum)XboxABProtocol).ToProtocolString()}://";
                yield return $"xboxabhostname={XboxABHostname}";
            }
            if (ChangeXboxLiveAddress)
            {
                yield return $"xboxliveprotocol={((ProtocolEnum)XboxLiveProtocol).ToProtocolString()}://";
                yield return $"xboxlivehostname={XboxLiveHostname}";
            }
        }
    }

    private void EnforceSimpleModeDefaults()
    {
        ChangeLocatorAddress = true;
        DisableSunsetTimeCheck = true;
        DisableLicenseCheck = true;
        DisableTelemetry = true;
        DisableMsaLoginSignatureValidation = true;
        ChangeAppName = true;
        ChangePackageName = true;
        LoginServerSingleDomainMode = true;

        ChangeMSALoginServiceAddress = true;
        ChangePlayfabApiAddress = true;
        ChangeXboxABAddress = true;
        ChangeXboxLiveAddress = true;
    }
}