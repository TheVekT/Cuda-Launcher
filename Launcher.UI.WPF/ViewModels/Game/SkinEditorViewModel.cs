using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Assets.Models;
using Launcher.Infrastructure.Assets.Validators;
using Launcher.UI.WPF.Helpers.Collections;
using Launcher.UI.WPF.Helpers.Localization;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Services.Windows.Abstractions;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels.Game.Items;

namespace Launcher.UI.WPF.ViewModels.Game;

public enum SkinModelType
{
    Classic,
    Slim
}

public partial class SkinEditorViewModel : ObservableValidator
{
    private readonly ICharacterManagerService _characterService;
    private readonly SkinsStore _skinsStore;
    private readonly IFileDialogService _dialogService; 
    
    private readonly CharacterItemViewModel? _editingSkin;
    
    [ObservableProperty]
    private bool _isEditMode;
    
    private bool _isSkinReplaced;


    [ObservableProperty] 
    private string _menuTitle;

    public ObservableRangeCollection<CapeItemViewModel> AvailableCapes { get; } = new();

    [ObservableProperty] 
    private string _characterName = string.Empty;
    [ObservableProperty] 
    private string _suggestedName = "Unnamed";
    [ObservableProperty] 
    private SkinModelType _selectedModelType = SkinModelType.Classic;
    [ObservableProperty] 
    private CapeItemViewModel? _selectedCape;
    [ObservableProperty] 
    private string? _tempSkinFilePath;
    [ObservableProperty] 
    private string? _skinValidationResult;


    public SkinEditorViewModel(
        ICharacterManagerService characterService, 
        IFileDialogService dialogService,
        SkinsStore skinsStore,
        CharacterItemViewModel? editingSkin = null) 
    {
        _characterService = characterService;
        _dialogService = dialogService;
        _skinsStore = skinsStore;
        
        _editingSkin = editingSkin;
        IsEditMode = _editingSkin != null;


        MenuTitle = IsEditMode ? LocalizationService.Instance[LocKey.SkinEditorMenu_Settings_Title] : LocalizationService.Instance[LocKey.SkinEditorMenu_Title];
        
        var noCapeOption = new CapeItemViewModel
        (
            id : null!,
            alias : LocalizationService.Instance[LocKey.SkinEditorMenu_NoCapeOption], 
            localImagePath : null!,
            cape2DPreviewPath : null!,
            isActive : false
        );
        
        AvailableCapes.Add(noCapeOption);
        AvailableCapes.AddRange(_skinsStore.AvailableCapes);
        
        if (IsEditMode)
        {
            CharacterName = _editingSkin!.CoreModel.Name;
            SuggestedName = _editingSkin.CoreModel.Name;
            SelectedModelType = _editingSkin.CoreModel.SkinVariant == "slim" ? SkinModelType.Slim : SkinModelType.Classic;
            SelectedCape = AvailableCapes.FirstOrDefault(c => c.Id == _editingSkin.CoreModel.CapeId) ?? noCapeOption;
            TempSkinFilePath = _editingSkin.FullSkinPath;
        }
        else
        {
            SelectedCape = noCapeOption;
        }
    }

    private bool ValidateSkin()
    {
        var validationResult = SkinValidator.ValidateSkinFile(TempSkinFilePath);
        if (validationResult != ValidationResult.Success)
        {
            LocKey errorKey = MapValidationErrorToKey(validationResult?.ErrorMessage);
            var errorMessage = LocalizationService.Instance[errorKey];
            SkinValidationResult = errorMessage;
            return false;
        }
        return true;
    }
    
    private LocKey MapValidationErrorToKey(string? errorMessage)
    {
        return errorMessage switch
        {
            "No file selected." => LocKey.Errors_SkinValidation_NoFileSelected,
            "File not found." => LocKey.Errors_SkinValidation_FileNotFound,
            "Invalid file format. Only PNG files are allowed." => LocKey.Errors_SkinValidation_NotPng,
            "Invalid skin dimensions. Only 64x32 and 64x64 skins are allowed." => LocKey.Errors_SkinValidation_WrongSize,
            "Invalid skin file. Please select a valid PNG file." => LocKey.Errors_SkinValidation_InvalidPng,
            _ => LocKey.None
        };
    }

    [RelayCommand]
    private void ImportSkinFile() 
    {
        string? selectedFilePath = _dialogService.OpenFile("PNG Files|*.png", "Select Skin File"); 

        if (!string.IsNullOrEmpty(selectedFilePath))
        {
            TempSkinFilePath = selectedFilePath;
            if (!ValidateSkin()) 
                return;
            SkinValidationResult = null;
            _isSkinReplaced = true;
            
            if (!IsEditMode || string.IsNullOrWhiteSpace(CharacterName))
                SuggestedName = Path.GetFileNameWithoutExtension(selectedFilePath);
        }
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!IsEditMode && string.IsNullOrEmpty(TempSkinFilePath)) return;
        if (!ValidateSkin()) 
            return;
        try
        {
            var finalName = string.IsNullOrWhiteSpace(CharacterName) ? SuggestedName : CharacterName;
            string? currentSkinFileName = IsEditMode ? _editingSkin!.CoreModel.SkinFileName : string.Empty;
            
            if (_isSkinReplaced && !string.IsNullOrEmpty(TempSkinFilePath))
            {
                currentSkinFileName = await _characterService.ImportSkinFileAsync(TempSkinFilePath);
            }

            if (IsEditMode)
            {
                bool needsPreviewUpdate = _isSkinReplaced || 
                                          _editingSkin!.CoreModel.SkinVariant != SelectedModelType.ToString().ToLower() ||
                                          _editingSkin.CoreModel.CapeId != SelectedCape?.Id;
                
                _editingSkin!.CoreModel.Name = finalName;
                _editingSkin.CoreModel.SkinFileName = currentSkinFileName;
                _editingSkin.CoreModel.SkinVariant = SelectedModelType.ToString().ToLower();
                _editingSkin.CoreModel.CapeId = SelectedCape?.Id;
                
                _editingSkin.FullSkinPath = _characterService.GetFullSkinPath(_editingSkin.CoreModel.SkinFileName!);
                _editingSkin.FullCapePath = SelectedCape?.LocalImagePath;
                
                _editingSkin.RefreshCoreUi();
                if (needsPreviewUpdate)
                {
                    _ = _skinsStore.RegeneratePreviewAsync(_editingSkin);
                }
            }
            else
            {
                var coreCharacter = new CharacterModel
                (
                    id: Guid.NewGuid().ToString(),
                    name: finalName,
                    skinFileName: currentSkinFileName,
                    capeId: SelectedCape?.Id,
                    skinVariant: SelectedModelType.ToString().ToLower()
                );

                if (coreCharacter.SkinFileName != null)
                {
                    var newCharacterVm = new CharacterItemViewModel
                    (
                        coreModel: coreCharacter,
                        fullSkinPath: _characterService.GetFullSkinPath(coreCharacter.SkinFileName),
                        fullCapePath: SelectedCape?.LocalImagePath
                    );
                
                    _skinsStore.Skins.Add(newCharacterVm);
                
                    _ = _skinsStore.RegeneratePreviewAsync(newCharacterVm);
                }
            }

            var coreModelsToSave = _skinsStore.Skins.Select(s => s.CoreModel).ToList();
            await _characterService.SaveCharactersAsync(coreModelsToSave);

            CloseSelf();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SkinEditorVM] Error saving character: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void CloseSelf() =>
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
}