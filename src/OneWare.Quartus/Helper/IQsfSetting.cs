using OneWare.Essentials.Models;

namespace OneWare.Quartus.Helper;

public interface IQsfSetting
{
    public TitledSetting GetSettingModel();
    public void Save();
}