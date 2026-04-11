using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Core.Models;
using Launcher.Core.Services.Integrations;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Game;

public partial class SkinsViewModel : ObservableObject
{
    private readonly ICharacterManagerService _characterService;
    private readonly IFileDialogService _dialogService;
    private readonly IOverlayService _overlayService;
    private readonly IMojangProfileService _mojangProfileService;

    private readonly SkinsStore _skinsStore;
    private readonly AppStore _appStore;
    private readonly LoginStore _loginStore;
    
    public SkinsStore SkinsStore => _skinsStore;
    public AppStore AppStore => _appStore;
    public LoginStore LoginStore => _loginStore;

    public SkinsViewModel(ICharacterManagerService characterService,
        IFileDialogService dialogService,
        IOverlayService overlayService,
        IMojangProfileService mojangProfileService,
        SkinsStore skinsStore,
        AppStore appStore,
        LoginStore loginStore)
    {
        _characterService = characterService;
        _dialogService = dialogService;
        _overlayService = overlayService;
        _mojangProfileService = mojangProfileService;
        
        _skinsStore = skinsStore;
        _appStore = appStore;
        _loginStore = loginStore;
    }
    
    [RelayCommand]
    private async Task ApplySkin(CharacterItemViewModel skin)
    {
        if (skin != null)
        {
            _skinsStore.SelectedSkin = skin;
            await _skinsStore.ApplyToMojangAsync(skin);
        }
    }
    
    [RelayCommand]
    private async Task DeleteSkin(CharacterItemViewModel skin)
    {
        if (skin != null)
        {
            await _skinsStore.DeleteSkin(skin);
        }
    }
    
    [RelayCommand]
    private void EditSkin(CharacterItemViewModel skin)
    {
        var skinEditorVM = new SkinEditorVM(_characterService, _dialogService, _skinsStore, skin);;
        _overlayService.Show(skinEditorVM);
    }

    [RelayCommand]
    private void AddCharacter()
    {
        var skinEditorVM = new SkinEditorVM(_characterService, _dialogService, _skinsStore);
        _overlayService.Show(skinEditorVM);
    }
}