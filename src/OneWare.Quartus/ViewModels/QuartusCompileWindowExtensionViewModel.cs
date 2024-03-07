using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;

namespace OneWare.Quartus.ViewModels;

public class QuartusCompileWindowExtensionViewModel : ObservableObject
{
    private bool _isVisible = false;
    
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public QuartusCompileWindowExtensionViewModel(IProjectExplorerService projectExplorerService)
    {
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
}