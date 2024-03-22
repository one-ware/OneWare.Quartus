using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using OneWare.Essentials.Services;
using OneWare.Quartus.Views;
using OneWare.UniversalFpgaProjectSystem.Fpga;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using Prism.Ioc;

namespace OneWare.Quartus.ViewModels;

public class QuartusLoaderWindowExtensionViewModel : ObservableObject
{
    private readonly IWindowService _windowService;
    private readonly UniversalFpgaProjectRoot _projectRoot;
    private readonly IFpga? _fpga;
    
    public bool IsVisible { get; }
    
    public bool IsEnabled { get; }

    public QuartusLoaderWindowExtensionViewModel(UniversalFpgaProjectRoot projectRoot, IWindowService windowService, FpgaService fpgaService)
    {
        _windowService = windowService;
        _projectRoot = projectRoot;
        
        _fpga = fpgaService.Fpgas.FirstOrDefault(x => x.Name == projectRoot.GetProjectProperty("Fpga"));

        IsVisible = projectRoot.Loader is QuartusLoader;
        IsEnabled = _fpga != null;
    }

    public async Task OpenSettingsAsync(Control control)
    {
        if (_fpga == null) return;

        var ownerWindow = TopLevel.GetTopLevel(control) as Window;
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _windowService.ShowDialogAsync(new QuartusLoaderSettingsView()
                    { DataContext = new QuartusLoaderSettingsViewModel(_projectRoot, _fpga) }, ownerWindow);
            }
            catch (Exception e)
            {
                ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e);
            }
        });
    }
}