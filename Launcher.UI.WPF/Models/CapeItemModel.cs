using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.UI.WPF.Models;

public partial class CapeItemModel: ObservableObject
{
    [ObservableProperty] 
    public string _id;
    [ObservableProperty] 
    public string _alias;
    [ObservableProperty] 
    public string _localImagePath;
    [ObservableProperty] 
    public string _cape2DPreviewPath;
    [ObservableProperty] 
    public bool _isActive;
}