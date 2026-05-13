using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Assets;
using Launcher.Core.Assets.Abstractions;
using Launcher.Core.Common.Messaging;
using Launcher.Core.Common.Models;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Config.Models;
using Launcher.Core.Identity.Abstractions;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Stores;

public partial class SkinsStore : ObservableObject, IRecipient<MicrosoftLoggedMessage>
{
    private readonly ISettingsService _settingsService;
    private readonly ICharacterManagerService _characterService;
    private readonly IMojangProfileService _mojangProfileService;
    private readonly IMojangAssetCacheService _assetCache;
    private readonly ILauncherPathsService _pathsService;
    private readonly PreviewGeneratorService _previewGeneratorService;
    
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
        IMojangAssetCacheService assetCache,
        PreviewGeneratorService previewGeneratorService,
        ILauncherPathsService pathsService)
    {
        _settingsService = settingsService;
        _characterService = characterService;
        _mojangProfileService = mojangProfileService;
        _assetCache = assetCache;
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
    
    public async Task SyncWithMojangAsync(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return;
        try
        {
            var profile = await _mojangProfileService.GetProfileAsync(accessToken);
            if (profile == null) return;
            _accessToken = accessToken;
            
            var loadedCapes = new List<CapeItemModel>();
            foreach (var cape in profile.Capes)
            {
                string localPath = await _assetCache.GetOrDownloadAssetAsync(cape.Url, cape.Id);
                
                if (localPath != null)
                {
                    loadedCapes.Add(new CapeItemModel
                    {
                        Id = cape.Id,
                        Alias = cape.Alias,
                        LocalImagePath = localPath,
                        Cape2DPreviewPath = _previewGeneratorService.GenerateCapePreview(localPath, cape.Id),
                        IsActive = cape.State == "ACTIVE"
                    });
                }
            }
            AvailableCapes.ReplaceRange(loadedCapes);
            
            foreach (var skin in Skins)
            {
                if (!string.IsNullOrEmpty(skin.CoreModel.CapeId))
                {
                    var matchingCape = AvailableCapes.FirstOrDefault(c => c.Id == skin.CoreModel.CapeId);
                    skin.FullCapePath = matchingCape?.LocalImagePath;
                }
            }
            
            var activeMojangSkin = profile.Skins.FirstOrDefault(s => s.State == "ACTIVE");
            if (activeMojangSkin != null)
            {
                string localSkinPath = await _assetCache.GetOrDownloadAssetAsync(activeMojangSkin.Url, activeMojangSkin.Id);
                var activeCapeId = profile.Capes.FirstOrDefault(c => c.State == "ACTIVE")?.Id;
                
                var coreTempSkin = new CharacterModel
                {
                    Id = profile.Id,
                    Name = profile.Name,
                    SkinFileName = Path.GetFileName(localSkinPath),
                    CapeId = activeCapeId,
                    SkinVariant = activeMojangSkin.Variant?.ToLower() ?? "classic"
                };
                
                var tempSkinVM = new CharacterItemViewModel(coreTempSkin)
                {
                    FullSkinPath = localSkinPath,
                    FullCapePath = AvailableCapes.FirstOrDefault(c => c.Id == activeCapeId)?.LocalImagePath,
                };

                SelectedSkin = tempSkinVM;
            }
            
            OnPropertyChanged(nameof(SelectedSkin));
            OnPropertyChanged(nameof(Skins));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SkinsStore] Error sync with mojang: {ex.Message}");
        }
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

    public async Task ApplyToMojangAsync(CharacterItemViewModel skinVM)
    {
        if (string.IsNullOrEmpty(_accessToken)) return;

        try
        {
            await _mojangProfileService.UploadSkinAsync(_accessToken, skinVM.FullSkinPath, skinVM.CoreModel.SkinVariant);
            
            if (string.IsNullOrEmpty(skinVM.CoreModel.CapeId))
            {
                await _mojangProfileService.HideCapeAsync(_accessToken);
            }
            else
            {
                await _mojangProfileService.ApplyCapeAsync(_accessToken, skinVM.CoreModel.CapeId);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SkinCreationVM] Error applying skin and cape: {ex.Message}");
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

    public async void Receive(MicrosoftLoggedMessage message)
    {
        await SyncWithMojangAsync(message.User.AccessToken);
    }
    
}