using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace STranslate.Plugin.Ocr.SimpleTex.ViewModel;

public partial class SettingsViewModel(IPluginContext context, Settings settings) : ObservableObject
{
    [ObservableProperty]
    private string _appId = settings.AppId;

    [ObservableProperty]
    private string _appSecret = settings.AppSecret;

    partial void OnAppIdChanged(string value)
    {
        settings.AppId = value;
        context.SaveSettingStorage<Settings>();
    }

    partial void OnAppSecretChanged(string value)
    {
        settings.AppSecret = value;
        context.SaveSettingStorage<Settings>();
    }
}