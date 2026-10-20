using Avalonia.Controls;
using MCEPatcher.UI.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace MCEPatcher.UI.Utils;

internal static class U
{
    public static string? LimitLengthMiddle(string? str, int maxLength, string ender = "...")
    {
        if (str is null || str.Length <= maxLength) return str;

        int strLen = (maxLength - ender.Length) / 2;

        if (strLen <= 0) return ender;

        return str.Substring(0, strLen) + ender + str.Substring(str.Length - strLen, strLen);
    }

    public static async Task ShowError(string message)
    {
        await ShowDialogAsync("Error", message, Icon.Error);
    }

    public static async Task<ButtonResult> ShowDialogAsync(string title, string message, Icon icon = Icon.None, ButtonEnum buttonEnum = ButtonEnum.Ok)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, @enum: buttonEnum, icon: icon, windowStartupLocation: WindowStartupLocation.CenterOwner);
        if (MainWindow.Instance is not null)
        {
            return await box.ShowWindowDialogAsync(MainWindow.Instance);
        }
        return await box.ShowAsync();
    }
}
