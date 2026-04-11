using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Launcher.Core.Models;
using Launcher.UI.WPF.Models;

namespace Launcher.UI.WPF.Resources.Controls;

public partial class SkinCard : UserControl
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
        DependencyProperty.Register("Character", typeof(CharacterItemViewModel), typeof(SkinCard), new PropertyMetadata(null));

    public CharacterItemViewModel Character
    {
        get => (CharacterItemViewModel)GetValue(CharacterProperty);
        set => SetValue(CharacterProperty, value);
    }

    // 2. Команда "Применить"
    public static readonly DependencyProperty ApplyCommandProperty =
        DependencyProperty.Register("ApplyCommand", typeof(ICommand), typeof(SkinCard), new PropertyMetadata(null));

    public ICommand ApplyCommand
    {
        get => (ICommand)GetValue(ApplyCommandProperty);
        set => SetValue(ApplyCommandProperty, value);
    }
    
    public static readonly DependencyProperty EditCommandProperty =
        DependencyProperty.Register("EditCommand", typeof(ICommand), typeof(SkinCard), new PropertyMetadata(null));

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }
    
    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register("DeleteCommand", typeof(ICommand), typeof(SkinCard), new PropertyMetadata(null));

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
}

