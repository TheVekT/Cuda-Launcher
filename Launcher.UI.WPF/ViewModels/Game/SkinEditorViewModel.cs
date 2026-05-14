using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Assets.Abstractions;
using Launcher.Core.Assets.Validators;
using Launcher.Core.Common.Models;
using Launcher.Core.System.Abstractions;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

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
    
    private bool _isSkinReplaced = false;


    [ObservableProperty] 
    private string _menuTitle;

    public ObservableRangeCollection<CapeItemModel> AvailableCapes { get; } = new();

    [ObservableProperty] 
    private string _characterName = string.Empty;
    [ObservableProperty] 
    private string _suggestedName = "Unnamed";
    [ObservableProperty] 
    private SkinModelType _selectedModelType = SkinModelType.Classic;
    [ObservableProperty] 
    private CapeItemModel? _selectedCape;
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


        MenuTitle = IsEditMode ? LocalizationService.Instance["SkinEditorMenu.Settings.Title"] : LocalizationService.Instance["SkinEditorMenu.Title"];
        
        var noCapeOption = new CapeItemModel 
        { 
            Id = null!,
            Alias = LocalizationService.Instance["SkinEditorMenu.NoCapeOption"], 
            LocalImagePath = null! 
        };
        
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
        var validationResult = SkinValidator.ValidateSkinFile(TempSkinFilePath, null);
        if (validationResult != ValidationResult.Success)
        {
            string? errorKey = validationResult!.ErrorMessage;
            var errorMessage = LocalizationService.Instance[errorKey];
            SkinValidationResult = errorMessage;
            return false;
        }
        return true;
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
            string currentSkinFileName = IsEditMode ? _editingSkin!.CoreModel.SkinFileName : string.Empty;
            
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
                
                _editingSkin.FullSkinPath = _characterService.GetFullSkinPath(_editingSkin.CoreModel.SkinFileName);
                _editingSkin.FullCapePath = SelectedCape?.LocalImagePath;
                
                _editingSkin.RefreshCoreUI();
                if (needsPreviewUpdate)
                {
                    _ = _skinsStore.RegeneratePreviewAsync(_editingSkin);
                }
            }
            else
            {
                var coreCharacter = new CharacterModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = finalName,
                    SkinFileName = currentSkinFileName, 
                    SkinVariant = SelectedModelType.ToString().ToLower(),
                    CapeId = SelectedCape?.Id
                };
                
                var newCharacterVM = new CharacterItemViewModel(coreCharacter)
                {
                    FullSkinPath = _characterService.GetFullSkinPath(coreCharacter.SkinFileName),
                    FullCapePath = SelectedCape?.LocalImagePath
                };
                
                _skinsStore.Skins.Add(newCharacterVM);
                
                _ = _skinsStore.RegeneratePreviewAsync(newCharacterVM);
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