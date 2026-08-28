using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Launcher.UI.WPF.ViewModels.Game.Items;

namespace Launcher.UI.WPF.Views.Game.Components;

public partial class SkinCard
{
    public SkinCard()
    {
        InitializeComponent();
    }
    
    private void OpenContextMenu(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.DataContext = this;
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
            e.Handled = true;
        }
    }
    
    public static readonly DependencyProperty CharacterProperty =
        DependencyProperty.Register(nameof(Character), typeof(CharacterItemViewModel), typeof(SkinCard), new PropertyMetadata(null));

    public CharacterItemViewModel Character
    {
        get => (CharacterItemViewModel)GetValue(CharacterProperty);
        set => SetValue(CharacterProperty, value);
    }

    // 2. Команда "Применить"
    public static readonly DependencyProperty ApplyCommandProperty =
        DependencyProperty.Register(nameof(ApplyCommand), typeof(ICommand), typeof(SkinCard), new PropertyMetadata(null));

    public ICommand ApplyCommand
    {
        get => (ICommand)GetValue(ApplyCommandProperty);
        set => SetValue(ApplyCommandProperty, value);
    }
    
    public static readonly DependencyProperty EditCommandProperty =
        DependencyProperty.Register(nameof(EditCommand), typeof(ICommand), typeof(SkinCard), new PropertyMetadata(null));

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }
    
    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(SkinCard), new PropertyMetadata(null));

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
}

