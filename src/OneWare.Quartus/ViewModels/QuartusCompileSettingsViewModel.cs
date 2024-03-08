using OneWare.Essentials.Controls;
using OneWare.Essentials.ViewModels;
using OneWare.ProjectSystem.Models;
using OneWare.Quartus.Helper;
using OneWare.Settings;
using OneWare.Settings.ViewModels;
using OneWare.Settings.ViewModels.SettingTypes;
using OneWare.UniversalFpgaProjectSystem.Models;

namespace OneWare.Quartus.ViewModels;

public class QuartusCompileSettingsViewModel : FlexibleWindowViewModelBase
{
    private string _qsfPath;
    private QsfFile _qsfFile;
    
    public SettingsCollectionViewModel SettingsCollection { get; } = new("Quartus Settings")
    {
        ShowTitle = false
    };

    private ComboBoxSetting _processorCountSetting;
    private ComboBoxSetting _configurationModeSetting;
    private ComboBoxSetting _optimizationModeSetting;
    
    private Dictionary<string, string> AvailableConfigurationModes { get; } = new()
    {
        { "DEFAULT", "Default" },
        { "DUAL IMAGES", "Dual Compressed Images (256Kbits UFM)" },
        { "SINGLE COMP IMAGE", "Single Compressed Image (1376Kbits UFM)" },
        { "SINGLE COMP IMAGE WITH ERAM", "Single Compressed Image with Memory Initialization (256Kbits UFM)" },
        { "SINGLE IMAGE", "Single Uncompressed Image (912Kbits UFM)" },
        { "SINGLE IMAGE WITH ERAM", "Single Uncompressed Image with Memory Initialization (256Kbits UFM)" }
    };

    private Dictionary<string, string> AvailableOptimizationModes { get; } = new()
    {
        { "BALANCED", "Balanced" },
        { "HIGH PERFORMANCE EFFORT", "Performance (High effort - increases runtime)" },
        { "AGGRESSIVE PERFORMANCE", "Performance (Aggressive - increases runtime and area)" },
        { "HIGH POWER EFFORT", "Power (High effort - increases runtime)" },
        { "AGGRESSIVE POWER", "Power (Aggressive - increases runtime, reduces performance)" },
        { "AGGRESSIVE AREA", "Area (Aggressive - reduces performance)" }
    };

    
    public QuartusCompileSettingsViewModel(UniversalFpgaProjectRoot fpgaProjectRoot)
    {
        Title = "Quartus Compile Settings";
        Id = "QuartusCompileSettings";
        
        _qsfPath = QsfHelper.GetQsfPath(fpgaProjectRoot);
        _qsfFile = QsfHelper.ReadQsf(_qsfPath);

        _processorCountSetting = new ComboBoxSetting("Parallel Compilation Processors",
            "Max processors Quartus will use to compile",
            Environment.ProcessorCount, Enumerable.Range(1, Environment.ProcessorCount).Cast<object>());

        _configurationModeSetting = new ComboBoxSetting("Configuration Mode", "Quartus Configuration Mode",
            AvailableConfigurationModes["DEFAULT"], AvailableConfigurationModes.Values);

        _optimizationModeSetting = new ComboBoxSetting("Optimization Mode", "Quartus Optimization Mode",
            AvailableOptimizationModes["BALANCED"], AvailableOptimizationModes.Values);
        
        SettingsCollection.SettingModels.Add(new ComboBoxSettingViewModel(_processorCountSetting));
        SettingsCollection.SettingModels.Add(new ComboBoxSettingViewModel(_configurationModeSetting));
        SettingsCollection.SettingModels.Add(new ComboBoxSettingViewModel(_optimizationModeSetting));

        var processorCount = _qsfFile.GetGlobalAssignment("NUM_PARALLEL_PROCESSORS");
        if(!string.IsNullOrWhiteSpace(processorCount) && int.TryParse(processorCount, out var processorCountInt)) _processorCountSetting.Value = processorCountInt;
        
        var configurationMode = _qsfFile.GetGlobalAssignment("INTERNAL_FLASH_UPDATE_MODE");
        if(!string.IsNullOrWhiteSpace(configurationMode)) _configurationModeSetting.Value = configurationMode;
        
        var optimizationMode = _qsfFile.GetGlobalAssignment("OPTIMIZATION_MODE");
        if(!string.IsNullOrWhiteSpace(optimizationMode)) _optimizationModeSetting.Value = optimizationMode;
    }
    
    public void Save(FlexibleWindow flexibleWindow)
    {
        _qsfFile.SetGlobalAssignment("NUM_PARALLEL_PROCESSORS", _processorCountSetting.Value.ToString()!);
        _qsfFile.SetGlobalAssignment("INTERNAL_FLASH_UPDATE_MODE", _configurationModeSetting.Value.ToString()!);
        _qsfFile.SetGlobalAssignment("OPTIMIZATION_MODE", _optimizationModeSetting.Value.ToString()!);
        
        QsfHelper.WriteQsf(_qsfPath, _qsfFile);
        Close(flexibleWindow);
    }
}