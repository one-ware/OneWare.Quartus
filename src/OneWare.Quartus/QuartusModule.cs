using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneWare.Essentials.Helpers;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.Quartus.Services;
using OneWare.Quartus.ViewModels;
using OneWare.Quartus.Views;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.Quartus;

public class QuartusModule : OneWareModuleBase
{
    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<QuartusService>();
    }

    public override void Initialize(IServiceProvider serviceProvider)
    {
        var settingsService = serviceProvider.Resolve<ISettingsService>();
        var quartusService = serviceProvider.Resolve<QuartusService>();

        serviceProvider.Resolve<IWindowService>().RegisterUiExtension("CompileWindow_TopRightExtension", new OneWareUiExtension(x => new QuartusCompileWindowExtensionView()
        {
            DataContext = serviceProvider.Resolve<QuartusCompileWindowExtensionViewModel>()
        }));
        serviceProvider.Resolve<IWindowService>().RegisterUiExtension("UniversalFpgaToolBar_DownloaderConfigurationExtension", new OneWareUiExtension(x =>
        {
            if (x is not UniversalFpgaProjectRoot cm) return null;
            return new QuartusLoaderWindowExtensionView()
            {
                DataContext = serviceProvider.Resolve<QuartusLoaderWindowExtensionViewModel>((typeof(UniversalFpgaProjectRoot), cm))
            };
        }));
        serviceProvider.Resolve<FpgaService>().RegisterToolchain<QuartusToolchain>();
        serviceProvider.Resolve<FpgaService>().RegisterLoader<QuartusLoader>();

        var defaultQuartusPath = PlatformHelper.Platform switch
        {
            PlatformId.LinuxX64 or PlatformId.LinuxArm64 => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "intelFPGA_lite", "18.1",
                "quartus"),
            _ => Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "",
                "intelFPGA_lite", "18.1",
                "quartus")
        };
        
        settingsService.RegisterTitledFolderPath("Tools", "Quartus", "Quartus_Path", "Quartus Path",
            "Sets the path for Quartus", defaultQuartusPath, null, null, IsQuartusPathValid);

        settingsService.GetSettingObservable<string>("Quartus_Path").Subscribe(x =>
        {
            if (string.IsNullOrEmpty(x)) return;

            if (!IsQuartusPathValid(x))
            {
                serviceProvider.Resolve<ILogger>().Warning("Quartus path invalid", null, false);
                return;
            }
            
            var binPath = Path.Combine(x, "bin");
            var bin64Path = Path.Combine(x, "bin64");

            ContainerLocator.Container.Resolve<IEnvironmentService>().SetPath("Quartus_Bin64", bin64Path);
            ContainerLocator.Container.Resolve<IEnvironmentService>().SetPath("Quartus_Bin", binPath);
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