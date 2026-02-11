using OneWare.Essentials.Controls;
using OneWare.Essentials.Models;
using OneWare.Essentials.ViewModels;
using OneWare.Settings.ViewModels;
using OneWare.UniversalFpgaProjectSystem.Fpga;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Parser;

namespace OneWare.Quartus.ViewModels;

public class QuartusLoaderSettingsViewModel : FlexibleWindowViewModelBase
{
    private readonly UniversalFpgaProjectRoot _projectRoot;
    private readonly Dictionary<string, string> _settings;
    private readonly IFpga _fpga;
    private readonly ComboBoxSetting _shortTermModeSetting;
    private readonly TitledSetting _shortTermOperationSetting;
    private readonly TitledSetting _shortTermArgumentsSetting;
    private readonly ComboBoxSetting _longTermModeSetting;
    private readonly TitledSetting _longTermOperationSetting;
    private readonly ComboBoxSetting _longTermFormatSetting;
    private readonly TitledSetting _longTermCpfArgumentsSetting;
    private readonly TitledSetting _longTermArgumentsSetting;
    
    public SettingsCollectionViewModel SettingsCollection { get; } = new("Quartus Loader Settings")
    {
        ShowTitle = false
    };

    public QuartusLoaderSettingsViewModel(UniversalFpgaProjectRoot projectRoot, IFpga fpga)
    {
        _projectRoot = projectRoot;
        _fpga = fpga;
        
        Title = "Quartus Loader Settings";
        Id = "Quartus Loader Settings";
        
        var defaultProperties = fpga.Properties;
        _settings = FpgaSettingsParser.LoadSettings(projectRoot, fpga.Name);
        
        _shortTermModeSetting = new ComboBoxSetting("Short Term Mode",
            defaultProperties.GetValueOrDefault("quartusProgrammerShortTermMode") ?? "", ["JTAG", "AS", "PS", "SD"]);
        
        _shortTermOperationSetting = new TextBoxSetting("Short Term Operation",
            defaultProperties.GetValueOrDefault("quartusProgrammerShortTermOperation") ?? "",
            "Operation to use for Short Term Programming");
        
        _shortTermArgumentsSetting = new TextBoxSetting("Short Term Additional Arguments",
            defaultProperties.GetValueOrDefault("quartusProgrammerShortTermArguments") ?? "",
            "Additional Arguments to use for Short Term Programming");
        
        _longTermModeSetting = new ComboBoxSetting("Long Term Mode",
            defaultProperties.GetValueOrDefault("quartusProgrammerLongTermMode") ?? "", ["JTAG", "AS", "PS", "SD"]);
            
        _longTermOperationSetting = new TextBoxSetting("Long Term Operation",
            defaultProperties.GetValueOrDefault("quartusProgrammerLongTermOperation") ?? "",
            "Operation to use for Long Term Programming");
        
        _longTermFormatSetting = new ComboBoxSetting("Long Term Format",
            defaultProperties.GetValueOrDefault("quartusProgrammerLongTermFormat") ?? "", ["POF", "JIC"]);
        
        _longTermCpfArgumentsSetting = new TextBoxSetting("Long Term Cpf Arguments",
            defaultProperties.GetValueOrDefault("quartusProgrammerLongTermCpfArguments") ?? "",
            "If format is different from POF, these arguments will be used to convert .sof to given format");
        
        _longTermArgumentsSetting = new TextBoxSetting("Long Term Additional Arguments",
            defaultProperties.GetValueOrDefault("quartusProgrammerLongTermArguments") ?? "",
            "Additional Arguments to use for Long Term Programming");
        
        if (_settings.TryGetValue("quartusProgrammerShortTermMode", out var qPstMode))
            _shortTermModeSetting.Value = qPstMode;
        
        if (_settings.TryGetValue("quartusProgrammerShortTermOperation", out var qPstOperation))
            _shortTermOperationSetting.Value = qPstOperation;
        
        if (_settings.TryGetValue("quartusProgrammerShortTermArguments", out var qPstArguments))
            _shortTermArgumentsSetting.Value = qPstArguments;
        
        if (_settings.TryGetValue("quartusProgrammerLongTermMode", out var qPltMode))
            _longTermModeSetting.Value = qPltMode;
        
        if (_settings.TryGetValue("quartusProgrammerLongTermOperation", out var qPltOperation))
            _longTermOperationSetting.Value = qPltOperation;
        
        if (_settings.TryGetValue("quartusProgrammerLongTermFormat", out var qPltFormat))
            _longTermFormatSetting.Value = qPltFormat;
        
        if (_settings.TryGetValue("quartusProgrammerLongTermCpfArguments", out var qPltCpfArguments))
            _longTermCpfArgumentsSetting.Value = qPltCpfArguments;
        
        if (_settings.TryGetValue("quartusProgrammerLongTermArguments", out var qPltArguments))
            _longTermArgumentsSetting.Value = qPltArguments;
        
        SettingsCollection.SettingModels.Add(_shortTermModeSetting);
        SettingsCollection.SettingModels.Add(_shortTermOperationSetting);
        SettingsCollection.SettingModels.Add(_shortTermArgumentsSetting);
        SettingsCollection.SettingModels.Add(_longTermModeSetting);
        SettingsCollection.SettingModels.Add(_longTermOperationSetting);
        SettingsCollection.SettingModels.Add(_longTermFormatSetting);
        SettingsCollection.SettingModels.Add(_longTermCpfArgumentsSetting);
        SettingsCollection.SettingModels.Add(_longTermArgumentsSetting);
    }
    
    public void Save(FlexibleWindow flexibleWindow)
    {
        _settings["quartusProgrammerShortTermMode"] = _shortTermModeSetting.Value.ToString()!;
        _settings["quartusProgrammerShortTermOperation"] = _shortTermOperationSetting.Value.ToString()!;
        _settings["quartusProgrammerShortTermArguments"] = _shortTermArgumentsSetting.Value.ToString()!;
        _settings["quartusProgrammerLongTermMode"] = _longTermModeSetting.Value.ToString()!;
        _settings["quartusProgrammerLongTermOperation"] = _longTermOperationSetting.Value.ToString()!;
        _settings["quartusProgrammerLongTermFormat"] = _longTermFormatSetting.Value.ToString()!;
        _settings["quartusProgrammerLongTermCpfArguments"] = _longTermCpfArgumentsSetting.Value.ToString()!;
        _settings["quartusProgrammerLongTermArguments"] = _longTermArgumentsSetting.Value.ToString()!;

        FpgaSettingsParser.SaveSettings(_projectRoot, _fpga.Name, _settings);
        
        Close(flexibleWindow);
    }
    
    public void Reset()
    {
        foreach (var setting in SettingsCollection.SettingModels)
        {
            setting.Value = setting.DefaultValue;
        }
    }
}
