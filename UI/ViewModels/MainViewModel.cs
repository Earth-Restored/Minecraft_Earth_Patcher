using Avalonia.Interactivity;
using MCEPatcher.UI.Models;
using MCEPatcher.UI.Utils;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

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
        set => this.RaiseAndSetIfChanged(ref disableMsaLoginSignatureValidation, value);
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
    }
}
