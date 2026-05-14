using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Launcher.UI.WPF.Helpers;

namespace Launcher.UI.WPF.Views.Instances.Components;

public partial class VersionElement : UserControl
{
    private static readonly DispatcherTimer _sharedTimer;
    
    private static readonly HashSet<VersionElement> _activeElements = new();
    
    static VersionElement()
    {
        _sharedTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        
        _sharedTimer.Tick += SharedTimer_Tick;
        _sharedTimer.Start();
    }

    private static void SharedTimer_Tick(object? sender, EventArgs e)
    {
        foreach (var element in _activeElements.ToList())
        {
            element.UpdateLastPlayedBinding();
        }
    }

    public VersionElement()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _activeElements.Add(this);
        UpdateLastPlayedBinding();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _activeElements.Remove(this);
    }

    private void UpdateLastPlayedBinding()
    {
        BindingExpression binding = this.GetBindingExpression(VersionElementHelper.LastPlayedProperty);
        binding?.UpdateTarget();
    }
}

