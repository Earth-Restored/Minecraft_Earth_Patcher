using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace MCEPatcher.UI.Utils;

public static class DialogHelper
{
    public static async Task ShowError(string message)
    {
        await ShowDialogAsync("Error", message, Icon.Error);
    }

    public static async Task<ButtonResult> ShowDialogAsync(string title, string message, Icon icon = Icon.None, ButtonEnum buttonEnum = ButtonEnum.Ok)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, @enum: buttonEnum, icon: icon, windowStartupLocation: WindowStartupLocation.CenterOwner);
        if (Views.MainWindow.Instance is not null)
        {
            return await box.ShowWindowDialogAsync(Views.MainWindow.Instance);
        }
        return await box.ShowAsync();
    }
}
