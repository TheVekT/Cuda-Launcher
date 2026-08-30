using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Config.Abstractions;
using Launcher.Infrastructure.Config.Models;
using Launcher.UI.WPF.Helpers.Collections;
using Launcher.UI.WPF.Services.Rendering.Abstractions;
using Launcher.UI.WPF.ViewModels.Game.Items;
using CapeItemViewModel = Launcher.UI.WPF.ViewModels.Game.Items.CapeItemViewModel;

namespace Launcher.UI.WPF.Stores;

public partial class SkinsStore : ObservableObject
{
    private readonly ICharacterManagerService _characterService;
    private readonly ILauncherPathsService _pathsService;
    private readonly IPreviewGeneratorService _previewGeneratorService;
    
    public ObservableRangeCollection<CharacterItemViewModel> Skins { get; } = new();
    public ObservableRangeCollection<CapeItemViewModel> AvailableCapes { get; } = new();

    [ObservableProperty]
    [property: SettingProperty]
    private CharacterItemViewModel? _selectedSkin;

    public SkinsStore(
        ISettingsService settingsService, 
        ICharacterManagerService characterService,
        IPreviewGeneratorService previewGeneratorService,
        ILauncherPathsService pathsService)
    {
        _characterService = characterService;
        _pathsService = pathsService;
        _previewGeneratorService = previewGeneratorService;
        
        settingsService.Initialize(this);
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    public async Task InitializeAsync()
    {
        var coreSkins = await _characterService.LoadCharactersAsync();
        var uiSkins = new List<CharacterItemViewModel>();
    
        foreach (var coreModel in coreSkins)
        {
            if (coreModel.SkinFileName != null)
            {
                var itemVm = new CharacterItemViewModel(coreModel)
                {
                    FullSkinPath = _characterService.GetFullSkinPath(coreModel.SkinFileName)
                };

                if (!string.IsNullOrEmpty(coreModel.CapeId))
                {
                    itemVm.FullCapePath = Path.Combine(_pathsService.CacheDirectory, "MojangAssets", $"{coreModel.CapeId}.png");
                }
                uiSkins.Add(itemVm);
            }
        }
    
        Skins.ReplaceRange(uiSkins);
        _ = Generate3DPreviewsBackgroundAsync();
    }
    
    public async Task DeleteSkin(CharacterItemViewModel skinVm)
    {
        bool wasSelected = SelectedSkin == skinVm;
            
        Skins.Remove(skinVm);
        
        var coreModelsToSave = Skins.Select(s => s.CoreModel).ToList();
        await _characterService.SaveCharactersAsync(coreModelsToSave);
        
        if (wasSelected)
        {
            SelectedSkin = Skins.FirstOrDefault();
        }
    }
    
    private async Task Generate3DPreviewsBackgroundAsync()
    {
        foreach (var skinVm in Skins)
        {
            if (!string.IsNullOrEmpty(skinVm.Skin3DPreviewPath)) continue;

            try
            {
                if (skinVm.FullSkinPath != null)
                {
                    string? previewPath = await _previewGeneratorService.Generate3DSkinSnapshotAsync(
                        skinVm.FullSkinPath, 
                        skinVm.FullCapePath, 
                        skinVm.CoreModel.Id, 
                        skinVm.CoreModel.SkinVariant == "slim");
                
                    skinVm.Skin3DPreviewPath = previewPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkinsStore] Ошибка генерации превью для {skinVm.CoreModel.Name}: {ex.Message}");
            }
        }
    }
    
    public async Task RegeneratePreviewAsync(CharacterItemViewModel skinVm)
    {
        skinVm.Skin3DPreviewPath = null!;

        try
        {
            if (skinVm.FullSkinPath != null)
            {
                string? previewPath = await _previewGeneratorService.Generate3DSkinSnapshotAsync(
                    skinVm.FullSkinPath, 
                    skinVm.FullCapePath, 
                    skinVm.CoreModel.Id, 
                    skinVm.CoreModel.SkinVariant == "slim",
                    forceRegenerate: true);
            
                skinVm.Skin3DPreviewPath = previewPath;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SkinsStore] Ошибка перерисовки превью: {ex.Message}");
        }
    }
}