using DynamicData;
using OneWare.Essentials.Controls;
using OneWare.Essentials.ViewModels;
using OneWare.Settings;
using OneWare.Settings.ViewModels;
using OneWare.Settings.ViewModels.SettingTypes;
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
        
        _shortTermModeSetting = new ComboBoxSetting("Short Term Mode", "Mode to use for Short Term Programming",
            defaultProperties.GetValueOrDefault("QuartusProgrammer_ShortTerm_Mode") ?? "", ["JTAG", "AS", "PS", "SD"]);
        
        _shortTermOperationSetting = new TitledSetting("Short Term Operation", "Operation to use for Short Term Programming",
            defaultProperties.GetValueOrDefault("QuartusProgrammer_ShortTerm_Operation") ?? "");
        
        _shortTermArgumentsSetting = new TitledSetting("Short Term Additional Arguments", "Additional Arguments to use for Short Term Programming",
            defaultProperties.GetValueOrDefault("QuartusProgrammer_ShortTerm_Arguments") ?? "");
        
        _longTermModeSetting = new ComboBoxSetting("Long Term Mode", "Mode to use for Long Term Programming",
            defaultProperties.GetValueOrDefault("QuartusProgrammer_LongTerm_Mode") ?? "", ["JTAG", "AS", "PS", "SD"]);
            
        _longTermOperationSetting = new TitledSetting("Long Term Operation", "Operation to use for Long Term Programming",
            defaultProperties.GetValueOrDefault("QuartusProgrammer_LongTerm_Operation") ?? "");
        
        _longTermFormatSetting = new ComboBoxSetting("Long Term Format", "Programming Format to use",
            defaultProperties.GetValueOrDefault("QuartusProgrammer_LongTerm_Format") ?? "", ["POF", "JIC"]);
        
        _longTermCpfArgumentsSetting = new TitledSetting("Long Term Cpf Arguments", "If format is different from POF, these arguments will be used to convert .sof to given format",
            defaultProperties.GetValueOrDefault("QuartusProgrammer_LongTerm_CpfArguments") ?? "");
        
        _longTermArgumentsSetting = new TitledSetting("Long Term Additional Arguments", "Additional Arguments to use for Long Term Programming",
            defaultProperties.GetValueOrDefault("QuartusProgrammer_LongTerm_Arguments") ?? "");
        
        if (_settings.TryGetValue("QuartusProgrammer_ShortTerm_Mode", out var qPstMode))
            _shortTermModeSetting.Value = qPstMode;
        
        if (_settings.TryGetValue("QuartusProgrammer_ShortTerm_Operation", out var qPstOperation))
            _shortTermOperationSetting.Value = qPstOperation;
        
        if (_settings.TryGetValue("QuartusProgrammer_ShortTerm_Arguments", out var qPstArguments))
            _shortTermArgumentsSetting.Value = qPstArguments;
        
        if (_settings.TryGetValue("QuartusProgrammer_LongTerm_Mode", out var qPltMode))
            _longTermModeSetting.Value = qPltMode;
        
        if (_settings.TryGetValue("QuartusProgrammer_LongTerm_Operation", out var qPltOperation))
            _longTermOperationSetting.Value = qPltOperation;
        
        if (_settings.TryGetValue("QuartusProgrammer_LongTerm_Format", out var qPltFormat))
            _longTermFormatSetting.Value = qPltFormat;
        
        if (_settings.TryGetValue("QuartusProgrammer_LongTerm_CpfArguments", out var qPltCpfArguments))
            _longTermCpfArgumentsSetting.Value = qPltCpfArguments;
        
        if (_settings.TryGetValue("QuartusProgrammer_LongTerm_Arguments", out var qPltArguments))
            _longTermArgumentsSetting.Value = qPltArguments;
        
        SettingsCollection.SettingModels.Add(new ComboBoxSettingViewModel(_shortTermModeSetting));
        SettingsCollection.SettingModels.Add(new TextBoxSettingViewModel(_shortTermOperationSetting));
        SettingsCollection.SettingModels.Add(new TextBoxSettingViewModel(_shortTermArgumentsSetting));
        SettingsCollection.SettingModels.Add(new ComboBoxSettingViewModel(_longTermModeSetting));
        SettingsCollection.SettingModels.Add(new TextBoxSettingViewModel(_longTermOperationSetting));
        SettingsCollection.SettingModels.Add(new ComboBoxSettingViewModel(_longTermFormatSetting));
        SettingsCollection.SettingModels.Add(new TextBoxSettingViewModel(_longTermCpfArgumentsSetting));
        SettingsCollection.SettingModels.Add(new TextBoxSettingViewModel(_longTermArgumentsSetting));
    }
    
    public void Save(FlexibleWindow flexibleWindow)
    {
        _settings["QuartusProgrammer_ShortTerm_Mode"] = _shortTermModeSetting.Value.ToString()!;
        _settings["QuartusProgrammer_ShortTerm_Operation"] = _shortTermOperationSetting.Value.ToString()!;
        _settings["QuartusProgrammer_ShortTerm_Arguments"] = _shortTermArgumentsSetting.Value.ToString()!;
        _settings["QuartusProgrammer_LongTerm_Mode"] = _longTermModeSetting.Value.ToString()!;
        _settings["QuartusProgrammer_LongTerm_Operation"] = _longTermOperationSetting.Value.ToString()!;
        _settings["QuartusProgrammer_LongTerm_Format"] = _longTermFormatSetting.Value.ToString()!;
        _settings["QuartusProgrammer_LongTerm_CpfArguments"] = _longTermCpfArgumentsSetting.Value.ToString()!;
        _settings["QuartusProgrammer_LongTerm_Arguments"] = _longTermArgumentsSetting.Value.ToString()!;

        FpgaSettingsParser.SaveSettings(_projectRoot, _fpga.Name, _settings);
        
        Close(flexibleWindow);
    }
    
    public void Reset()
    {
        foreach (var setting in SettingsCollection.SettingModels)
        {
            setting.Setting.Value = setting.Setting.DefaultValue;
        }
    }
}