using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.UI.WPF.ViewModels.Game.Items;

public partial class CapeItemViewModel(
    string id,
    string alias,
    string? localImagePath,
    string? cape2DPreviewPath,
    bool isActive)
    : ObservableObject
{
    [ObservableProperty] 
    private string _id = id;
    
    [ObservableProperty] 
    private string _alias = alias;
    
    [ObservableProperty] 
    private string? _localImagePath = localImagePath;
    
    [ObservableProperty] 
    private string? _cape2DPreviewPath = cape2DPreviewPath;
    
    [ObservableProperty] 
    private bool _isActive = isActive;
}
