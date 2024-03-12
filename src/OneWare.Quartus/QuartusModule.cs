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

        containerProvider.Resolve<IWindowService>().RegisterUiExtension("CompileWindow_TopRightExtension", new UiExtension(x => new QuartusCompileWindowExtensionView()
        {
            DataContext = containerProvider.Resolve<QuartusCompileWindowExtensionViewModel>()
        }));
        containerProvider.Resolve<FpgaService>().RegisterToolchain<QuartusToolchain>();
        containerProvider.Resolve<FpgaService>().RegisterLoader<QuartusLoader>();

        settingsService.RegisterTitledPath("Tools", "Quartus", "Quartus_Path", "Quartus Path",
            "Sets the path for Quartus", Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "",
                "intelFPGA_lite", "18.1",
                "quartus"), null, null, IsQuartusPathValid);

        string? environmentPathSetting;

        settingsService.GetSettingObservable<string>("Quartus_Path").Subscribe(x =>
        {
            if (string.IsNullOrEmpty(x)) return;

            if (!IsQuartusPathValid(x))
            {
                containerProvider.Resolve<ILogger>().Warning("Quartus path invalid", null, false);
                return;
            }

            var binPath = Path.Combine(x, "bin64");

            environmentPathSetting = PlatformHelper.Platform switch
            {
                PlatformId.WinX64 or PlatformId.WinArm64 => $";{binPath};",
                _ => $":{binPath}:"
            };

            var currentPath = Environment.GetEnvironmentVariable("PATH");

            Environment.SetEnvironmentVariable("PATH", $"{environmentPathSetting}{currentPath}");
        });
    }

    private static bool IsQuartusPathValid(string path)
    {
        if (!Directory.Exists(path)) return false;
        if (!File.Exists(Path.Combine(path, "bin64", $"quartus_pgm{PlatformHelper.ExecutableExtension}"))) return false;
        return true;
    }
}