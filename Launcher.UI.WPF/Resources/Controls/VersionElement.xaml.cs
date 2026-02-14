using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Launcher.UI.WPF.Helpers;

namespace Launcher.UI.WPF.Resources.Controls;

public partial class VersionElement : UserControl
{
    private DispatcherTimer _timer;

    public VersionElement()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_timer == null)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += Timer_Tick;
        }
        _timer.Start();
        UpdateLastPlayedBinding();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer?.Stop();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        UpdateLastPlayedBinding();
    }

    private void UpdateLastPlayedBinding()
    {
        BindingExpression binding = this.GetBindingExpression(VersionElementHelper.LastPlayedProperty);
        binding?.UpdateTarget();
    }
}

