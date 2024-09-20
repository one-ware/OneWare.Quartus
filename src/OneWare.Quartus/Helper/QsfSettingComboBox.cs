using OneWare.Essentials.Models;
using OneWare.Settings;
using OneWare.Settings.ViewModels.SettingTypes;

namespace OneWare.Quartus.Helper;

public class QsfSettingComboBox : IQsfSetting
{
    private readonly string _name;
    private readonly QsfFile _qsfFile;
    private readonly Dictionary<string, string> _options;
    private readonly ComboBoxSetting _setting;
    
    public QsfSettingComboBox(QsfFile file, string name, string title, string description, Dictionary<string, string> options, string defaultValue)
    {
        _qsfFile = file;
        _name = name;
        _options = options;
        
        _setting = new ComboBoxSetting(title, description, defaultValue, options.Values);
        
        var setting = file.GetGlobalAssignment(name);
        
        if (!string.IsNullOrWhiteSpace(setting))
        {
            options.TryAdd(setting, setting);
            _setting.Value = options[setting];
        }
    }

    public SettingViewModel GetViewModel()
    {
        return new ComboBoxSettingViewModel(_setting);
    }

    public void Save()
    {
        if(_setting.Value.ToString() != "Default")
            _qsfFile.SetGlobalAssignment(_name, _options.FirstOrDefault(x => x.Value == (string)_setting.Value).Key);
        else 
            _qsfFile.RemoveGlobalAssignment(_name);
    }
}