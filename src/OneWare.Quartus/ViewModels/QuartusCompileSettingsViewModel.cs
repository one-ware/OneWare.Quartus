using DynamicData;
using ImTools;
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

    private List<IQsfSetting> _settings = [];
    
    public QuartusCompileSettingsViewModel(UniversalFpgaProjectRoot fpgaProjectRoot)
    {
        Title = "Quartus Compile Settings";
        Id = "QuartusCompileSettings";
        
        _qsfPath = QsfHelper.GetQsfPath(fpgaProjectRoot);
        _qsfFile = QsfHelper.ReadQsf(_qsfPath);
        
        _settings.Add(new QsfSettingSlider(_qsfFile, "NUM_PARALLEL_PROCESSORS", "Parallel CPU Count", "Max processors Quartus will use to compile", 
            Environment.ProcessorCount, 1, Environment.ProcessorCount, 1));

        _settings.Add(new QsfSettingComboBox(_qsfFile, "INTERNAL_FLASH_UPDATE_MODE", "Configuration Mode", "Quartus Configuration Mode", 
            new Dictionary<string, string>()
            {
                { "DEFAULT", "Default" },
                { "DUAL IMAGES", "Dual Compressed Images (256Kbits UFM)" },
                { "SINGLE COMP IMAGE", "Single Compressed Image (1376Kbits UFM)" },
                { "SINGLE COMP IMAGE WITH ERAM", "Single Compressed Image with Memory Initialization (256Kbits UFM)" },
                { "SINGLE IMAGE", "Single Uncompressed Image (912Kbits UFM)" },
                { "SINGLE IMAGE WITH ERAM", "Single Uncompressed Image with Memory Initialization (256Kbits UFM)" }
            }, "Default"));
        
        _settings.Add(new QsfSettingComboBox(_qsfFile, "OPTIMIZATION_MODE", "Optimization Mode", "Quartus Optimization Mode", 
            new Dictionary<string, string>()
            {
                { "BALANCED", "Balanced" },
                { "HIGH PERFORMANCE EFFORT", "Performance (High effort - increases runtime)" },
                { "AGGRESSIVE PERFORMANCE", "Performance (Aggressive - increases runtime and area)" },
                { "HIGH POWER EFFORT", "Power (High effort - increases runtime)" },
                { "AGGRESSIVE POWER", "Power (Aggressive - increases runtime, reduces performance)" },
                { "AGGRESSIVE AREA", "Area (Aggressive - reduces performance)" }
            }, "Balanced"));

        foreach (var setting in _settings)
        {
            SettingsCollection.SettingModels.Add(setting.GetViewModel());
        }
    }
    
    public void Save(FlexibleWindow flexibleWindow)
    {
        foreach (var setting in _settings)
        {
            setting.Save();
        }
        
        QsfHelper.WriteQsf(_qsfPath, _qsfFile);
        Close(flexibleWindow);
    }
}