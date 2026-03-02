using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace Launcher.UI.WPF.Helpers;

[ContentProperty("Command")]
public class DragDropBinding : MarkupExtension
{
    public ICommand Command { get; set; }
    public string Event { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}

public static class DragDropBehavior
{
    public static readonly DependencyProperty DragEnterCommandProperty =
        DependencyProperty.RegisterAttached(
            "DragEnterCommand",
            typeof(ICommand),
            typeof(DragDropBehavior),
            new PropertyMetadata(null, OnDragEnterCommandChanged));

    public static readonly DependencyProperty DragLeaveCommandProperty =
        DependencyProperty.RegisterAttached(
            "DragLeaveCommand",
            typeof(ICommand),
            typeof(DragDropBehavior),
            new PropertyMetadata(null, OnDragLeaveCommandChanged));

    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.RegisterAttached(
            "DropCommand",
            typeof(ICommand),
            typeof(DragDropBehavior),
            new PropertyMetadata(null, OnDropCommandChanged));

    public static void SetDragEnterCommand(UIElement element, ICommand value)
    {
        element.SetValue(DragEnterCommandProperty, value);
    }

    public static ICommand GetDragEnterCommand(UIElement element)
    {
        return (ICommand)element.GetValue(DragEnterCommandProperty);
    }

    public static void SetDragLeaveCommand(UIElement element, ICommand value)
    {
        element.SetValue(DragLeaveCommandProperty, value);
    }

    public static ICommand GetDragLeaveCommand(UIElement element)
    {
        return (ICommand)element.GetValue(DragLeaveCommandProperty);
    }

    public static void SetDropCommand(UIElement element, ICommand value)
    {
        element.SetValue(DropCommandProperty, value);
    }

    public static ICommand GetDropCommand(UIElement element)
    {
        return (ICommand)element.GetValue(DropCommandProperty);
    }

    private static void OnDragEnterCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if (e.OldValue != null)
            {
                element.DragEnter -= OnDragEnter;
            }
            if (e.NewValue != null)
            {
                element.DragEnter += OnDragEnter;
            }
        }
    }

    private static void OnDragLeaveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if (e.OldValue != null)
            {
                element.DragLeave -= OnDragLeave;
            }
            if (e.NewValue != null)
            {
                element.DragLeave += OnDragLeave;
            }
        }
    }

    private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if (e.OldValue != null)
            {
                element.Drop -= OnDrop;
            }
            if (e.NewValue != null)
            {
                element.Drop += OnDrop;
            }
        }
    }

    private static void OnDragEnter(object sender, DragEventArgs e)
    {
        if (sender is UIElement element)
        {
            var command = GetDragEnterCommand(element);
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }
    }

    private static void OnDragLeave(object sender, DragEventArgs e)
    {
        if (sender is UIElement element)
        {
            var command = GetDragLeaveCommand(element);
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is UIElement element)
        {
            var command = GetDropCommand(element);
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }
    }
}
