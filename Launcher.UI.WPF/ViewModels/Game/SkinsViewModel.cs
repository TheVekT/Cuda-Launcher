using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Common.Enums;
using Launcher.Core.Identity.Abstractions;
using Launcher.Core.System.Abstractions;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Assets.Models;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Services.Abstractions;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Game;

public partial class SkinsViewModel : ObservableObject, IRecipient<AccountLoggedMessage>
{
    private readonly ICharacterManagerService _characterService;
    private readonly IFileDialogService _dialogService;
    private readonly IOverlayService _overlayService;
    private readonly IMojangProfileService _mojangProfileService;
    private readonly IMojangAssetCacheService _assetCacheService;
    private readonly IPreviewGeneratorService _previewGeneratorService;
    private readonly INotificationService _notificationService;

    private readonly SkinsStore _skinsStore;
    private readonly AppStore _appStore;
    private readonly IdentityStore _identityStore;
    
    public SkinsStore SkinsStore => _skinsStore;
    public AppStore AppStore => _appStore;
    public IdentityStore IdentityStore => _identityStore;

    public SkinsViewModel(ICharacterManagerService characterService,
        IFileDialogService dialogService,
        IOverlayService overlayService,
        IMojangProfileService mojangProfileService,
        IMojangAssetCacheService assetCacheService,
        IPreviewGeneratorService previewGeneratorService,
        INotificationService notificationService,
        SkinsStore skinsStore,
        AppStore appStore,
        IdentityStore identityStore)
    {
        _characterService = characterService;
        _dialogService = dialogService;
        _overlayService = overlayService;
        _mojangProfileService = mojangProfileService;
        _assetCacheService = assetCacheService;
        _previewGeneratorService = previewGeneratorService;
        _notificationService = notificationService;

        _skinsStore = skinsStore;
        _appStore = appStore;
        _identityStore = identityStore;
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    public async Task SyncWithMojangAsync(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return;
        try
        {
            // Get mojang profile
            var profile = await _mojangProfileService.GetProfileAsync(accessToken);
            if (profile == null) return;
            
            var loadedCapes = new List<CapeItemModel>();
            foreach (var cape in profile.Capes)
            {
                string localPath = await _assetCacheService.GetOrDownloadAssetAsync(cape.Url, cape.Id);
                
                if (localPath != null)
                {
                    // Create a new CapeItemModel and add it to the loadedCapes list and generate a preview for it
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
            // Update the AvailableCapes collection with the loaded capes from Mojang profile
            SkinsStore.AvailableCapes.ReplaceRange(loadedCapes);
            
            // Update the FullCapePath for each skin in SkinsStore based on the available capes
            foreach (var skin in SkinsStore.Skins)
            {
                if (!string.IsNullOrEmpty(skin.CoreModel.CapeId))
                {
                    var matchingCape = SkinsStore.AvailableCapes.FirstOrDefault(c => c.Id == skin.CoreModel.CapeId);
                    skin.FullCapePath = matchingCape?.LocalImagePath;
                }
            }
            
            // Find the active skin from Mojang profile and set it as the selected skin in SkinsStore
            var activeMojangSkin = profile.Skins.FirstOrDefault(s => s.State == "ACTIVE");
            if (activeMojangSkin != null)
            {
                // Download the active skin and cape and create a temporary CharacterModel for it
                string localSkinPath = await _assetCacheService.GetOrDownloadAssetAsync(activeMojangSkin.Url, activeMojangSkin.Id);
                var activeCapeId = profile.Capes.FirstOrDefault(c => c.State == "ACTIVE")?.Id;
                
                // Create a temporary CharacterModel for the active skin
                var coreTempSkin = new CharacterModel
                (
                    id : profile.Id,
                    name : profile.Name,
                    skinFileName : Path.GetFileName(localSkinPath),
                    capeId : activeCapeId,
                    skinVariant : activeMojangSkin.Variant?.ToLower() ?? "classic"
                );
                
                // Create a temporary CharacterItemViewModel for the active skin and set it as the selected skin in SkinsStore
                var tempSkinVM = new CharacterItemViewModel(coreTempSkin)
                {
                    FullSkinPath = localSkinPath,
                    FullCapePath = SkinsStore.AvailableCapes.FirstOrDefault(c => c.Id == activeCapeId)?.LocalImagePath,
                };
                Console.WriteLine($"[SkinsStore] Active skin set: {tempSkinVM.FullSkinPath}, Cape: {tempSkinVM.FullCapePath}");
                SkinsStore.SelectedSkin = tempSkinVM;
            }
            
            OnPropertyChanged(nameof(SkinsStore.SelectedSkin));
            OnPropertyChanged(nameof(SkinsStore.Skins));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SkinsStore] Error sync with mojang: {ex.Message}");
        }
    }
    
    public async Task<SkinApplyResult> ApplyToMojangAsync(CharacterItemViewModel skinVM)
    {
        var token = _identityStore.CurrentAccount?.AccessToken;
        if (string.IsNullOrEmpty(token))
            return new SkinApplyResult(false, false, "Access token is missing.", "Access token is missing.");
        // 1. Request to Mojang API to apply skin and cape
        var skinStatus = await _mojangProfileService.UploadSkinAsync(
            token, skinVM.FullSkinPath, skinVM.CoreModel.SkinVariant);
        var capeStatus = string.IsNullOrEmpty(skinVM.CoreModel.CapeId)
            ? await _mojangProfileService.HideCapeAsync(token)
            : await _mojangProfileService.ApplyCapeAsync(token, skinVM.CoreModel.CapeId);
        // 2. Define a local function to get error messages based on the status
        string? GetErrorMessage(NetworkRequestStatus status, string target) => status switch
        {
            NetworkRequestStatus.Success => null,
            NetworkRequestStatus.RateLimited => "Too many requests. Please wait a moment.",
            _ => $"Unknown error occurred while applying {target}."
        };
        var skinSuccess = skinStatus == NetworkRequestStatus.Success;
        var capeSuccess = capeStatus == NetworkRequestStatus.Success;
        var result = new SkinApplyResult(
            skinSuccess, capeSuccess, 
            GetErrorMessage(skinStatus, "skin"), 
            GetErrorMessage(capeStatus, "cape"));
        // 3. Show notification if there was an error
        if (!result.IsFullySuccessful)
        {
            var titleKey = (skinSuccess, capeSuccess) switch
            {
                (false, false) => LocKey.Warnings_ApplyMojangCharacter_Title,
                (false, true)  => LocKey.Warnings_ApplyMojangSkin_Title,
                _              => LocKey.Warnings_ApplyMojangCape_Title
            };
            var isRateLimited = skinStatus == NetworkRequestStatus.RateLimited || 
                                capeStatus == NetworkRequestStatus.RateLimited;
            var descKey = isRateLimited
                ? LocKey.Warnings_ApplyMojangCharacter_RateLimit
                : LocKey.Warnings_ApplyMojangCharacter_Unknown;
            _notificationService.ShowWarning(
                LocalizationService.Instance[titleKey], 
                LocalizationService.Instance[descKey]);
        }
        return result;
    }

    
    [RelayCommand]
    private async Task ApplySkinAsync(CharacterItemViewModel skin)
    {
        var applied = await ApplyToMojangAsync(skin);
        if (applied.IsCapeApplied || applied.IsSkinApplied)
        {
            await SyncWithMojangAsync(_identityStore.CurrentAccount?.AccessToken);
        }
        await Task.Delay(1000);
    }
    
    [RelayCommand]
    private async Task DeleteSkin(CharacterItemViewModel skin)
    {
        await _skinsStore.DeleteSkin(skin);
    }
    
    [RelayCommand]
    private void EditSkin(CharacterItemViewModel skin)
    {
        var skinEditorVM = new SkinEditorViewModel(_characterService, _dialogService, _skinsStore, skin);;
        _overlayService.Show(skinEditorVM);
    }

    [RelayCommand]
    private void AddCharacter()
    {
        var skinEditorVM = new SkinEditorViewModel(_characterService, _dialogService, _skinsStore);
        _overlayService.Show(skinEditorVM);
    }
    
    public async void Receive(AccountLoggedMessage message)
    {
        if (!message.User.IsOffline)
        {
            await SyncWithMojangAsync(message.User.AccessToken);
        }
    }
}