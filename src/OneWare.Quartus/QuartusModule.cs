using System.Reactive.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis;
using OneWare.Essentials.Helpers;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;
using OneWare.Quartus.Services;
using OneWare.Quartus.ViewModels;
using OneWare.Quartus.Views;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace OneWare.Quartus;

public class QuartusModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<QuartusService>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var settingsService = containerProvider.Resolve<ISettingsService>();
        var quartusService = containerProvider.Resolve<QuartusService>();

        containerProvider.Resolve<IWindowService>().RegisterUiExtension<QuartusCompileWindowExtensionView>("CompileWindow_TopRightExtension", containerProvider.Resolve<QuartusCompileWindowExtensionViewModel>());
        containerProvider.Resolve<FpgaService>().RegisterToolchain<QuartusToolchain>();
        containerProvider.Resolve<FpgaService>().RegisterLoader<QuartusLoader>();

        var defaultQuartusPath = PlatformHelper.Platform switch
        {
            PlatformId.LinuxX64 or PlatformId.LinuxArm64 => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "intelFPGA_lite", "18.1",
                "quartus"),
            _ => Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "",
                "intelFPGA_lite", "18.1",
                "quartus")
        };
        
        settingsService.RegisterTitledPath("Tools", "Quartus", "Quartus_Path", "Quartus Path",
            "Sets the path for Quartus", defaultQuartusPath, null, null, IsQuartusPathValid);

        string? environmentPathSetting;

        settingsService.GetSettingObservable<string>("Quartus_Path").Skip(1).Subscribe(x =>
        {
            if (string.IsNullOrEmpty(x)) return;

            if (!IsQuartusPathValid(x))
            {
                containerProvider.Resolve<ILogger>().Warning("Quartus path invalid", null, false);
                return;
            }

            var binPath = Path.Combine(x, "bin");
            var bin64Path = Path.Combine(x, "bin64");

            environmentPathSetting = PlatformHelper.Platform switch
            {
                PlatformId.WinX64 or PlatformId.WinArm64 => $";{bin64Path};",
                _ => $":{binPath}:"
            };

            var currentPath = Environment.GetEnvironmentVariable("PATH");

            Environment.SetEnvironmentVariable("PATH", $"{environmentPathSetting}{currentPath}");
        });
    }

    private static bool IsQuartusPathValid(string path)
    {
        if (!Directory.Exists(path)) return false;
        if (!File.Exists(Path.Combine(path, "bin64", $"quartus_pgm{PlatformHelper.ExecutableExtension}")) 
            && !File.Exists(Path.Combine(path, "bin", $"quartus_pgm{PlatformHelper.ExecutableExtension}"))) return false;
        return true;
    }
}