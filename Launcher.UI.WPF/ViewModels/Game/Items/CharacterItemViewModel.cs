using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Infrastructure.Assets.Models;

namespace Launcher.UI.WPF.ViewModels.Game.Items;

public partial class CharacterItemViewModel(
    CharacterModel coreModel,
    string? fullSkinPath = null,
    string? fullCapePath = null,
    string? cape2DPreviewPath = null,
    string? skin3DPreviewPath = null)
    : ObservableObject
{
    public CharacterModel CoreModel { get; } = coreModel;

    [ObservableProperty]
    private string? _fullSkinPath = fullSkinPath;

    [ObservableProperty]
    private string? _fullCapePath = fullCapePath;

    [ObservableProperty]
    private string? _cape2DPreviewPath = cape2DPreviewPath;

    [ObservableProperty]
    private string? _skin3DPreviewPath = skin3DPreviewPath;

    public void RefreshCoreUi()
    {
        OnPropertyChanged(nameof(CoreModel));
    }
}
