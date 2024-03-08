using OneWare.Settings.ViewModels.SettingTypes;

namespace OneWare.Quartus.Helper;

public interface IQsfSetting
{
    public SettingViewModel GetViewModel();
    public void Save();
}