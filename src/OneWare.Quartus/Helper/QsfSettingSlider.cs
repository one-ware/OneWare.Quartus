using Avalonia.Controls;
using OneWare.Essentials.Models;
using OneWare.Settings;
using OneWare.Settings.ViewModels.SettingTypes;

namespace OneWare.Quartus.Helper;

public class QsfSettingSlider : IQsfSetting
{
    private readonly string _name;
    private readonly QsfFile _qsfFile;
    private readonly SliderSetting _setting;
    
    public QsfSettingSlider(QsfFile file, string name, string title, string description, int defaultValue, int min, int max, int step)
    {
        _qsfFile = file;
        _name = name;
        
        _setting = new SliderSetting(title, defaultValue, min, max, step)
        {
            HoverDescription = description
        };
        
        var setting = file.GetGlobalAssignment(name);
        
        if (!string.IsNullOrWhiteSpace(setting) && int.TryParse(setting, out var val))
        {
            _setting.Value = val;
        }
    }

    public TitledSetting GetSettingModel()
    {
        return _setting;
    }

    public void Save()
    {
        if(!string.IsNullOrWhiteSpace(_setting.Value.ToString()))
            _qsfFile.SetGlobalAssignment(_name, _setting.Value.ToString()!);
        else 
            _qsfFile.RemoveGlobalAssignment(_name);
    }
}