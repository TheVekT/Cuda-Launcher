using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Core.Models;

namespace Launcher.UI.WPF.Models;

public partial class CharacterItemViewModel : ObservableObject
{
    public CharacterModel CoreModel { get; }
    
    [ObservableProperty]
    private string _fullSkinPath;

    [ObservableProperty]
    private string? _fullCapePath;

    [ObservableProperty]
    private string _cape2DPreviewPath;

    [ObservableProperty]
    private string _skin3DPreviewPath;
    
    public CharacterItemViewModel(CharacterModel coreModel)
    {
        CoreModel = coreModel;
    }
    
    public void RefreshCoreUI()
    {
        OnPropertyChanged(nameof(CoreModel));
    }
}