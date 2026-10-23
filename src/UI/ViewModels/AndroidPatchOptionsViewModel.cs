using Avalonia.Platform.Storage;
using MCEPatcher.UI.Models;
using MCEPatcher.UI.Services;
using MCEPatcher.UI.Utils;
using ReactiveUI;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;

namespace MCEPatcher.UI.ViewModels;

public sealed class AndroidPatchOptionsViewModel : ViewModelBase
{
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
        get => "APK File: " + (StringUtils.LimitLengthMiddle(apkFile, 60) ?? "Not selected");
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
        get => "Resource Pack File (Optional): " + (StringUtils.LimitLengthMiddle(resourcePackFile, 60) ?? "Not selected");
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
    private int? androidVersion;
    public int? AndroidVersion
    {
        get => androidVersion;
        set => this.RaiseAndSetIfChanged(ref androidVersion, value);
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
        set => this.RaiseAndSetIfChanged(ref changeMSALoginServiceAddress, value);
    }
    private int msaLoginServiceProtocol;
    public int MSALoginServiceProtocol
    {
        get => msaLoginServiceProtocol;
        set => this.RaiseAndSetIfChanged(ref msaLoginServiceProtocol, value);
    }
    private string msaLoginServiceHostname;
    public string MSALoginServiceHostname
    {
        get => msaLoginServiceHostname;
        set => this.RaiseAndSetIfChanged(ref msaLoginServiceHostname, value);
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

    public ReactiveCommand<Unit, Unit> PickApkCommand { get; }
    public ReactiveCommand<Unit, Unit> PickResourcePackCommand { get; }

    public AndroidPatchOptionsViewModel()
    {
        AndroidVersion = PlatformService.Current.GetDefaultAndroidVersion() ?? 14;
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

        PickApkCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var path = await FilePickerHelper.PickFileAsync(new FilePickerOpenOptions
            {
                Title = "Minecraft earth APK file",
                AllowMultiple = false,
                FileTypeFilter = [ new FilePickerFileType("Apk")
                {
                    Patterns = ["*.apk"],
                    MimeTypes = ["application/vnd.android.package-archive"]
                } ],
            });
            if (path is not null)
            {
                ApkFile = path;
            }
        });

        PickResourcePackCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var path = await FilePickerHelper.PickFileAsync(new FilePickerOpenOptions
            {
                Title = "Minecraft earth resource pack file",
                AllowMultiple = false,
                FileTypeFilter = [ new FilePickerFileType("Zip")
                {
                    Patterns = ["*.zip"],
                    MimeTypes = ["application/zip"]
                } ],
            });
            if (path is not null)
            {
                ResourcePackFile = path;
            }
        });
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
        yield return "add-arcore-apikey"; // make sure it runs after change-package-name
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

    public async Task<Core.ApkProcessor.Options?> BuildOptionsAsync()
    {
        string? apkPath = ApkFilePath;

        if (apkPath is null)
        {
            await DialogHelper.ShowError("Select the apk file first");
            return null;
        }

        if (!File.Exists(apkPath))
        {
            await DialogHelper.ShowError("Selected apk file doesn't exist");
            return null;
        }

        using (var fs = File.OpenRead(apkPath))
        {
            if (!Core.ApkProcessor.VerifyApkHash(fs))
            {
                var result = await DialogHelper.ShowDialogAsync(
                    "Warning",
                    "The .apk file hash does not match. Patching may fail. Do you want to continue?",
                    MsBox.Avalonia.Enums.Icon.Error,
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo);

                if (result is MsBox.Avalonia.Enums.ButtonResult.No)
                {
                    return null;
                }
            }
        }

        string? resourcePackPath = ResourcePackFilePath;

        if (!string.IsNullOrEmpty(resourcePackPath))
        {
            if (!File.Exists(resourcePackPath))
            {
                await DialogHelper.ShowError("Selected resource pack file doesn't exist");
                return null;
            }

            using (var fs = File.OpenRead(resourcePackPath))
            {
                if (!Core.ApkProcessor.VerifyResourcePackHash(fs))
                {
                    var result = await DialogHelper.ShowDialogAsync(
                        "Warning",
                        "The resource pack file hash does not match. Game may not function correctly. Do you want to continue?",
                        MsBox.Avalonia.Enums.Icon.Error,
                        MsBox.Avalonia.Enums.ButtonEnum.YesNo);

                    if (result is MsBox.Avalonia.Enums.ButtonResult.No)
                    {
                        return null;
                    }
                }
            }
        }

        if (AndroidVersion is null or < 7)
        {
            await DialogHelper.ShowError("Android version must be selected");
            return null;
        }

        if (ChangeMSALoginServiceAddress && IPAddress.TryParse(MSALoginServiceHostname.Split(':')[0], out _))
        {
            await DialogHelper.ShowError("MSA login service address cannot be an IP");
            return null;
        }

        if (ChangePlayfabApiAddress && IPAddress.TryParse(PlayfabApiHostname.Split(':')[0], out _))
        {
            await DialogHelper.ShowError("Playfab api address cannot be an IP");
            return null;
        }

        if (ChangeXboxABAddress && IPAddress.TryParse(XboxABHostname.Split(':')[0], out _))
        {
            await DialogHelper.ShowError("XboxAB address cannot be an IP");
            return null;
        }

        if (ChangeXboxLiveAddress && IPAddress.TryParse(XboxLiveHostname.Split(':')[0], out _))
        {
            await DialogHelper.ShowError("Xbox live address cannot be an IP");
            return null;
        }

        if (string.IsNullOrEmpty(resourcePackPath))
        {
            var result = await DialogHelper.ShowDialogAsync(
                "Info",
                "Resource Pack file not selected. Do you want to continue?",
                MsBox.Avalonia.Enums.Icon.Info,
                MsBox.Avalonia.Enums.ButtonEnum.YesNo);

            if (result is MsBox.Avalonia.Enums.ButtonResult.No)
            {
                return null;
            }
        }

        return new Core.ApkProcessor.Options()
        {
            NonInteractive = true,
            InApk = apkPath,
            OutApk = PlatformService.Current.GetDefaultOutputPath("Solace.apk"),
            ResourcePack = resourcePackPath,
            DecodedDir = Path.Combine("tmp", "apk"),
            AndroidOSVersion = AndroidVersion.Value,
            Patches = GetPatches(),
            Variables = GetVariables(),
        };
    }
}
