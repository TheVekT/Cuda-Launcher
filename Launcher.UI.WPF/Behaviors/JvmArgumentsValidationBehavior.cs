using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CmlLib.Core;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Game.Validation;
using Launcher.Core.Instances.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.Behaviors;

public static class JvmArgumentsValidationBehavior
{
    private class ValidationState
    {
        public DispatcherTimer Timer { get; set; } = null!;
        public string? LastValidatedText { get; set; }
        public int CurrentValidationId { get; set; }
        public bool? LastValidationResult { get; set; }
    }

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(ValidationState),
            typeof(JvmArgumentsValidationBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(JvmArgumentsValidationBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty IsValidProperty =
        DependencyProperty.RegisterAttached(
            "IsValid",
            typeof(bool),
            typeof(JvmArgumentsValidationBehavior),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static bool GetIsValid(DependencyObject obj) => (bool)obj.GetValue(IsValidProperty);
    public static void SetIsValid(DependencyObject obj, bool value) => obj.SetValue(IsValidProperty, value);

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.RegisterAttached(
            "ErrorMessage",
            typeof(string),
            typeof(JvmArgumentsValidationBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static string? GetErrorMessage(DependencyObject obj) => (string?)obj.GetValue(ErrorMessageProperty);
    public static void SetErrorMessage(DependencyObject obj, string? value) => obj.SetValue(ErrorMessageProperty, value);

    public static readonly DependencyProperty JavaPathProperty =
        DependencyProperty.RegisterAttached(
            "JavaPath",
            typeof(string),
            typeof(JvmArgumentsValidationBehavior),
            new PropertyMetadata(null));

    public static string? GetJavaPath(DependencyObject obj) => (string?)obj.GetValue(JavaPathProperty);
    public static void SetJavaPath(DependencyObject obj, string? value) => obj.SetValue(JavaPathProperty, value);

    public static readonly DependencyProperty DebounceMillisecondsProperty =
        DependencyProperty.RegisterAttached(
            "DebounceMilliseconds",
            typeof(int),
            typeof(JvmArgumentsValidationBehavior),
            new PropertyMetadata(3000));

    public static int GetDebounceMilliseconds(DependencyObject obj) => (int)obj.GetValue(DebounceMillisecondsProperty);
    public static void SetDebounceMilliseconds(DependencyObject obj, int value) => obj.SetValue(DebounceMillisecondsProperty, value);

    public static readonly DependencyProperty DangerBrushKeyProperty =
        DependencyProperty.RegisterAttached(
            "DangerBrushKey",
            typeof(string),
            typeof(JvmArgumentsValidationBehavior),
            new PropertyMetadata("DangerForegroundBrush"));

    public static string GetDangerBrushKey(DependencyObject obj) => (string)obj.GetValue(DangerBrushKeyProperty);
    public static void SetDangerBrushKey(DependencyObject obj, string value) => obj.SetValue(DangerBrushKeyProperty, value);

    public static readonly DependencyProperty SuccessBrushKeyProperty =
        DependencyProperty.RegisterAttached(
            "SuccessBrushKey",
            typeof(string),
            typeof(JvmArgumentsValidationBehavior),
            new PropertyMetadata("SuccessForegroundBrush"));

    public static string GetSuccessBrushKey(DependencyObject obj) => (string)obj.GetValue(SuccessBrushKeyProperty);
    public static void SetSuccessBrushKey(DependencyObject obj, string value) => obj.SetValue(SuccessBrushKeyProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        if ((bool)e.NewValue)
        {
            var state = new ValidationState();
            state.Timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(GetDebounceMilliseconds(textBox))
            };
            state.Timer.Tick += async (_, _) =>
            {
                state.Timer.Stop();
                await ValidateAsync(textBox, state);
            };

            textBox.SetValue(StateProperty, state);
            textBox.TextChanged += OnTextChanged;
            textBox.GotFocus += OnGotFocus;
            textBox.LostFocus += OnLostFocus;
            textBox.Unloaded += OnUnloaded;
        }
        else
        {
            if (textBox.GetValue(StateProperty) is ValidationState state)
            {
                state.Timer.Stop();
                textBox.ClearValue(StateProperty);
            }

            textBox.TextChanged -= OnTextChanged;
            textBox.GotFocus -= OnGotFocus;
            textBox.LostFocus -= OnLostFocus;
            textBox.Unloaded -= OnUnloaded;

            ResetValidation(textBox);
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.GetValue(StateProperty) is not ValidationState state)
            return;

        string currentText = textBox.Text;

        if (string.IsNullOrWhiteSpace(currentText))
        {
            state.Timer.Stop();
            state.LastValidatedText = currentText;
            state.LastValidationResult = null;
            ResetValidation(textBox);
            return;
        }

        if (currentText == state.LastValidatedText)
            return;

        // Reset status while typing
        state.LastValidationResult = null;
        textBox.ClearValue(Control.BorderBrushProperty);
        textBox.ClearValue(FrameworkElement.ToolTipProperty);

        // Restart debounce timer (default 3 seconds)
        state.Timer.Stop();
        state.Timer.Interval = TimeSpan.FromMilliseconds(GetDebounceMilliseconds(textBox));
        state.Timer.Start();
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.GetValue(StateProperty) is not ValidationState state)
            return;

        // If it was already validated and is valid while has text, show success border on focus
        if (state.LastValidationResult == true && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            string successBrushKey = GetSuccessBrushKey(textBox);
            textBox.SetResourceReference(Control.BorderBrushProperty, successBrushKey);
        }
    }

    private static async void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.GetValue(StateProperty) is not ValidationState state)
            return;

        // If timer is running or text is not validated yet, validate immediately
        if (state.Timer.IsEnabled || textBox.Text != state.LastValidatedText)
        {
            state.Timer.Stop();
            await ValidateAsync(textBox, state);
        }
        else if (state.LastValidationResult == true)
        {
            // On lost focus, if validation was successful, revert border to standard style
            textBox.ClearValue(Control.BorderBrushProperty);
        }
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.GetValue(StateProperty) is not ValidationState state)
            return;

        state.Timer.Stop();
    }

    private static async Task ValidateAsync(TextBox textBox, ValidationState state)
    {
        string textToValidate = textBox.Text;

        if (string.IsNullOrWhiteSpace(textToValidate))
        {
            state.LastValidatedText = textToValidate;
            state.LastValidationResult = null;
            ResetValidation(textBox);
            return;
        }

        var validator = App.Services?.GetService<JvmArgumentsValidator>();
        if (validator == null)
            return;

        string javaExecutable = ResolveJavaBinary(textBox);
        int validationId = ++state.CurrentValidationId;

        var result = await validator.ValidateAsync(javaExecutable, textToValidate);

        // Discard result if newer validation has started or text changed in the meantime
        if (validationId != state.CurrentValidationId || textBox.Text != textToValidate)
            return;

        state.LastValidatedText = textToValidate;
        state.LastValidationResult = result.IsValid;

        if (result.IsValid)
        {
            SetIsValid(textBox, true);
            SetErrorMessage(textBox, null);
            textBox.ClearValue(FrameworkElement.ToolTipProperty);

            if (textBox.IsKeyboardFocused || textBox.IsFocused)
            {
                string successBrushKey = GetSuccessBrushKey(textBox);
                textBox.SetResourceReference(Control.BorderBrushProperty, successBrushKey);
            }
            else
            {
                textBox.ClearValue(Control.BorderBrushProperty);
            }
        }
        else
        {
            string errorMessage = !string.IsNullOrWhiteSpace(result.RejectedArgument)
                ? $"Invalid argument: {result.RejectedArgument}"
                : (result.ErrorMessage ?? "Invalid JVM arguments");

            SetIsValid(textBox, false);
            SetErrorMessage(textBox, errorMessage);

            string dangerBrushKey = GetDangerBrushKey(textBox);
            textBox.SetResourceReference(Control.BorderBrushProperty, dangerBrushKey);
            textBox.ToolTip = errorMessage;
        }
    }

    private static void ResetValidation(TextBox textBox)
    {
        SetIsValid(textBox, true);
        SetErrorMessage(textBox, null);
        textBox.ClearValue(Control.BorderBrushProperty);
        textBox.ClearValue(FrameworkElement.ToolTipProperty);
    }

    private static string ResolveJavaBinary(TextBox textBox)
    {
        string? explicitJava = GetJavaPath(textBox);
        if (!string.IsNullOrWhiteSpace(explicitJava))
            return explicitJava;

        var fileService = App.Services?.GetService<IInstanceFileSystemService>();
        var javaResolver = App.Services?.GetService<IJavaPathResolver>();

        if (fileService != null && javaResolver != null)
        {
            var globalMcPath = new MinecraftPath(fileService.GetSharedGameDataPath());
            string? resolved = javaResolver.ResolveJavaPath(globalMcPath, "1.20.1");
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;
        }

        return "javaw.exe";
    }
}
