using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Identity.Abstractions;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Config.Abstractions;
using Launcher.Infrastructure.Config.Models;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services.Abstractions;

namespace Launcher.UI.WPF.Stores;

public partial class SkinsStore : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ICharacterManagerService _characterService;
    private readonly IMojangProfileService _mojangProfileService;
    private readonly ILauncherPathsService _pathsService;
    private readonly IPreviewGeneratorService _previewGeneratorService;
    
    public ObservableRangeCollection<CharacterItemViewModel> Skins { get; } = new();
    public ObservableRangeCollection<CapeItemModel> AvailableCapes { get; } = new();
    
    private string _accessToken = string.Empty;

    [ObservableProperty]
    [property: SettingProperty]
    private CharacterItemViewModel _selectedSkin;

    public SkinsStore(
        ISettingsService settingsService, 
        ICharacterManagerService characterService,
        IMojangProfileService mojangProfileService,
        IPreviewGeneratorService previewGeneratorService,
        ILauncherPathsService pathsService)
    {
        _settingsService = settingsService;
        _characterService = characterService;
        _mojangProfileService = mojangProfileService;
        _pathsService = pathsService;
        _previewGeneratorService = previewGeneratorService;
        
        _settingsService.Initialize(this);
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    public async Task InitializeAsync()
    {
        var coreSkins = await _characterService.LoadCharactersAsync();
        var uiSkins = new List<CharacterItemViewModel>();
    
        foreach (var coreModel in coreSkins)
        {
            var itemVM = new CharacterItemViewModel(coreModel)
            {
                FullSkinPath = _characterService.GetFullSkinPath(coreModel.SkinFileName)
            };

            if (!string.IsNullOrEmpty(coreModel.CapeId))
            {
                itemVM.FullCapePath = Path.Combine(_pathsService.CacheDirectory, "MojangAssets", $"{coreModel.CapeId}.png");
            }
            uiSkins.Add(itemVM);
        }
    
        Skins.ReplaceRange(uiSkins);
        _ = Generate3DPreviewsBackgroundAsync();
    }
    
    public async Task DeleteSkin(CharacterItemViewModel skinVM)
    {
        if (skinVM == null) return;

        bool wasSelected = SelectedSkin == skinVM;
            
        Skins.Remove(skinVM);
        
        var coreModelsToSave = Skins.Select(s => s.CoreModel).ToList();
        await _characterService.SaveCharactersAsync(coreModelsToSave);
        
        if (wasSelected)
        {
            SelectedSkin = Skins.FirstOrDefault();
        }
    }
    
    private async Task Generate3DPreviewsBackgroundAsync()
    {
        foreach (var skinVM in Skins)
        {
            if (!string.IsNullOrEmpty(skinVM.Skin3DPreviewPath)) continue;

            try
            {
                string previewPath = await _previewGeneratorService.Generate3DSkinSnapshotAsync(
                    skinVM.FullSkinPath, 
                    skinVM.FullCapePath, 
                    skinVM.CoreModel.Id, 
                    skinVM.CoreModel.SkinVariant == "slim");
                
                skinVM.Skin3DPreviewPath = previewPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkinsStore] Ошибка генерации превью для {skinVM.CoreModel.Name}: {ex.Message}");
            }
        }
    }
    
    public async Task RegeneratePreviewAsync(CharacterItemViewModel skinVM)
    {
        skinVM.Skin3DPreviewPath = null!;

        try
        {
            string previewPath = await _previewGeneratorService.Generate3DSkinSnapshotAsync(
                skinVM.FullSkinPath, 
                skinVM.FullCapePath, 
                skinVM.CoreModel.Id, 
                skinVM.CoreModel.SkinVariant == "slim",
                forceRegenerate: true);
            
            skinVM.Skin3DPreviewPath = previewPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SkinsStore] Ошибка перерисовки превью: {ex.Message}");
        }
    }
}