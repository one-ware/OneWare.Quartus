using OneWare.Essentials.Models;
using OneWare.Settings.ViewModels.SettingTypes;

namespace OneWare.Quartus.Helper;

public interface IQsfSetting
{
    public TitledSetting GetSettingModel();
    public void Save();
}