using System.Windows;
using System.Windows.Input;

namespace Launcher.UI.WPF.Behaviors;

public static class FileDragDropBehavior
{
    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.RegisterAttached("DropCommand", typeof(ICommand), typeof(FileDragDropBehavior), new UIPropertyMetadata(null, OnDropCommandChanged));

    public static ICommand GetDropCommand(DependencyObject obj) => (ICommand)obj.GetValue(DropCommandProperty);
    public static void SetDropCommand(DependencyObject obj, ICommand value) => obj.SetValue(DropCommandProperty, value);
    
    public static readonly DependencyProperty DragEnterCommandProperty =
        DependencyProperty.RegisterAttached("DragEnterCommand", typeof(ICommand), typeof(FileDragDropBehavior), new UIPropertyMetadata(null));

    public static ICommand GetDragEnterCommand(DependencyObject obj) => (ICommand)obj.GetValue(DragEnterCommandProperty);
    public static void SetDragEnterCommand(DependencyObject obj, ICommand value) => obj.SetValue(DragEnterCommandProperty, value);
    
    public static readonly DependencyProperty DragLeaveCommandProperty =
        DependencyProperty.RegisterAttached("DragLeaveCommand", typeof(ICommand), typeof(FileDragDropBehavior), new UIPropertyMetadata(null));

    public static ICommand GetDragLeaveCommand(DependencyObject obj) => (ICommand)obj.GetValue(DragLeaveCommandProperty);
    public static void SetDragLeaveCommand(DependencyObject obj, ICommand value) => obj.SetValue(DragLeaveCommandProperty, value);
    
    private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.AllowDrop = true;
            element.PreviewDragEnter -= Element_DragEnter;
            element.PreviewDragLeave -= Element_DragLeave;
            element.PreviewDrop -= Element_Drop;

            if (e.NewValue is ICommand)
            {
                element.PreviewDragEnter += Element_DragEnter;
                element.PreviewDragLeave += Element_DragLeave;
                element.PreviewDrop += Element_Drop;
            }
        }
    }

    private static void Element_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            var command = GetDragEnterCommand((DependencyObject)sender);
            if (command?.CanExecute(null) == true) command.Execute(null);
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private static void Element_DragLeave(object sender, DragEventArgs e)
    {
        var command = GetDragLeaveCommand((DependencyObject)sender);
        if (command?.CanExecute(null) == true) command.Execute(null);
        e.Handled = true;
    }

    private static void Element_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var command = GetDropCommand((DependencyObject)sender);
            
            if (command?.CanExecute(files) == true) command.Execute(files);
        }
        e.Handled = true;
    }
}