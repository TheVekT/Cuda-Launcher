using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;

namespace Launcher.UI.WPF.Stores;

public class InstancesStore: INotifyPropertyChanged
{
    //Services
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    private readonly IInstanceService _instanceService;
    
    //Attributes
    private MinecraftInstance _selectedInstance;
    
    //Collections
    public ObservableCollection<MinecraftInstance> Instances { get; set; } = new();
    
    //Events
    public event Action AddedInstance;

    
    public InstancesStore(IInstanceFileSystemService instanceFileSystemService, IInstanceService instanceService)
    {
        _instanceFileSystemService = instanceFileSystemService;
        _instanceService = instanceService;
        
        LoadSavedInstances();
    }
    
    public void InvokeAddedInstance() => AddedInstance?.Invoke();
    
    private void LoadSavedInstances()
    {
        var loaded = _instanceService.LoadInstances();
        Instances.Clear();
        foreach (var inst in loaded)
        {
            Instances.Add(inst);
        }
        
        if (Instances.Count > 0) SelectedInstance = Instances[0];
    }
    
    public void DeleteInstance(MinecraftInstance instance)
    {
        bool wasSelected = (SelectedInstance == instance);
        
        _instanceService.DeleteInstance(instance.Id);
        
        Instances.Remove(instance);
        
        _instanceFileSystemService.DeleteInstance(instance);
        
        if (wasSelected)
        {
            SelectedInstance = Instances.FirstOrDefault() ;
        }
    }
    
    //Getters and Setters
    public MinecraftInstance SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            if (_selectedInstance != value)
            {
                _selectedInstance = value;
                OnPropertyChanged(nameof(SelectedInstance));
            }
        }
    }
   
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}