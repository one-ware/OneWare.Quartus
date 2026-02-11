using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using OneWare.Essentials.Services;
using OneWare.Quartus.Views;
using OneWare.UniversalFpgaProjectSystem.Models;

namespace OneWare.Quartus.ViewModels;

public class QuartusCompileWindowExtensionViewModel : ObservableObject
{
    private readonly IWindowService _windowService;
    private readonly IProjectExplorerService _projectExplorerService;
    
    private bool _isVisible = false;
    
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public QuartusCompileWindowExtensionViewModel(IProjectExplorerService projectExplorerService, IWindowService windowService)
    {
        _windowService = windowService;
        _projectExplorerService = projectExplorerService;
        
        IDisposable? disposable = null;
        projectExplorerService.WhenValueChanged(x => x.ActiveProject).Subscribe(x =>
        {
            disposable?.Dispose();
            if (x is not UniversalFpgaProjectRoot fpgaProjectRoot) return;
            disposable = fpgaProjectRoot.WhenValueChanged(y => y.Toolchain).Subscribe(z =>
            {
                IsVisible = z is QuartusToolchain;
            });
        });
    }

    public async Task OpenCompileSettingsAsync(Control owner)
    {
        var ownerWindow = TopLevel.GetTopLevel(owner) as Window;
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                if (_projectExplorerService.ActiveProject is UniversalFpgaProjectRoot fpgaProjectRoot)
                {
                    await _windowService.ShowDialogAsync(new QuartusCompileSettingsView()
                        { DataContext = new QuartusCompileSettingsViewModel(fpgaProjectRoot) }, ownerWindow);
                }
            }
            catch (Exception e)
            {
                ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e);
            }
        });
    }
}