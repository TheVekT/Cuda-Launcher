using Launcher.Core.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Launcher.UI.WPF.Helpers
{


    
    
    public static class ButtonHelper
    {
        public static readonly DependencyProperty BalanceTextProperty =
            DependencyProperty.RegisterAttached(
                "BalanceText",
                typeof(bool),
                typeof(ButtonHelper),
                new PropertyMetadata(false));

        [AttachedPropertyBrowsableForType(typeof(Button))]
        public static bool GetBalanceText(DependencyObject obj)
        {
            return (bool)obj.GetValue(BalanceTextProperty);
        }

        public static void SetBalanceText(DependencyObject obj, bool value)
        {
            obj.SetValue(BalanceTextProperty, value);
        }
        
        
        
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.RegisterAttached(
                "IconSize", 
                typeof(double), 
                typeof(ButtonHelper), 
                new PropertyMetadata(16.0));

        public static void SetIconSize(DependencyObject element, double value)
        {
            element.SetValue(IconSizeProperty, value);
        }

        public static double GetIconSize(DependencyObject element)
        {
            return (double)element.GetValue(IconSizeProperty);
        }
        
    }
    public class ComboBoxHelper
    {
        public static readonly DependencyProperty PopupPlacementProperty =
            DependencyProperty.RegisterAttached(
                "PopupPlacement",
                typeof(PlacementMode),
                typeof(ComboBoxHelper),
                new PropertyMetadata(PlacementMode.Bottom));

        public static void SetPopupPlacement(DependencyObject element, PlacementMode value)
        {
            element.SetValue(PopupPlacementProperty, value);
        }

        public static PlacementMode GetPopupPlacement(DependencyObject element)
        {
            return (PlacementMode)element.GetValue(PopupPlacementProperty);
        }
    }
    
    public class ScaleHelper
    {
        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.RegisterAttached(
                "ScaleFactor",
                typeof(double),
                typeof(ScaleHelper),
                new PropertyMetadata(1.0));

        public static void SetScaleFactor(DependencyObject element, double value)
        {
            element.SetValue(ScaleFactorProperty, value);
        }

        public static double GetScaleFactor(DependencyObject element)
        {
            return (double)element.GetValue(ScaleFactorProperty);
        }
    }
    
    public class ScrollViewerHelper
    {
        public static readonly DependencyProperty EnableHorizontalScrollingProperty =
            DependencyProperty.RegisterAttached(
                "EnableHorizontalScrolling",
                typeof(bool),
                typeof(ScrollViewerHelper),
                new PropertyMetadata(false, OnEnableHorizontalScrollingChanged));

        public static void SetEnableHorizontalScrolling(DependencyObject element, bool value) 
            => element.SetValue(EnableHorizontalScrollingProperty, value);

        public static bool GetEnableHorizontalScrolling(DependencyObject element) 
            => (bool)element.GetValue(EnableHorizontalScrollingProperty);

        private static void OnEnableHorizontalScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer && (bool)e.NewValue)
            {
                scrollViewer.PreviewMouseWheel += (s, args) =>
                {
                    if (args.Delta > 0)
                        scrollViewer.LineLeft(); 
                    else
                        scrollViewer.LineRight();
                    
                    args.Handled = true;
                };
            }
        }
    }
    public class VersionElementHelper
    {
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.RegisterAttached("DeleteCommand", typeof(ICommand), typeof(VersionElementHelper), new PropertyMetadata(null));

        public static void SetDeleteCommand(DependencyObject element, ICommand value) => element.SetValue(DeleteCommandProperty, value);
        public static ICommand GetDeleteCommand(DependencyObject element) => (ICommand)element.GetValue(DeleteCommandProperty);
        
        public static readonly DependencyProperty InstanceProperty =
            DependencyProperty.RegisterAttached("Instance", typeof(MinecraftInstance), typeof(VersionElementHelper), new PropertyMetadata(null));

        public static void SetInstance(DependencyObject element, MinecraftInstance value) => element.SetValue(InstanceProperty, value);
        public static MinecraftInstance GetInstance(DependencyObject element) => (MinecraftInstance)element.GetValue(InstanceProperty);
        
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(string),
                typeof(VersionElementHelper),
                new PropertyMetadata(null));

        public static string GetIcon(DependencyObject element)
        {
            return (string)element.GetValue(IconProperty);
        }

        public static void SetIcon(DependencyObject element, string value)
        {
            element.SetValue(IconProperty, value);
        }
        
        public static readonly DependencyProperty VersionNameProperty =
            DependencyProperty.RegisterAttached(
                "VersionName",
                typeof(string),
                typeof(VersionElementHelper),
                new PropertyMetadata(""));

        public static string GetVersionName(DependencyObject element)
        {
            return (string)element.GetValue(VersionNameProperty);
        }

        public static void SetVersionName(DependencyObject element, string value)
        {
            element.SetValue(VersionNameProperty, value);
        }
        
        public static readonly DependencyProperty ModLoaderProperty =
            DependencyProperty.RegisterAttached(
                "ModLoader",
                typeof(string),
                typeof(VersionElementHelper),
                new PropertyMetadata(""));

        public static string GetModLoader(DependencyObject element)
        {
            return (string)element.GetValue(ModLoaderProperty);
        }

        public static void SetModLoader(DependencyObject element, string value)
        {
            element.SetValue(ModLoaderProperty, value);
        }
        
        public static readonly DependencyProperty GameVersionProperty =
            DependencyProperty.RegisterAttached(
                "GameVersion",
                typeof(string),
                typeof(VersionElementHelper),
                new PropertyMetadata(""));

        public static string GetGameVersion(DependencyObject element)
        {
            return (string)element.GetValue(GameVersionProperty);
        }

        public static void SetGameVersion(DependencyObject element, string value)
        {
            element.SetValue(GameVersionProperty, value);
        }
        
        public static readonly DependencyProperty LastPlayedProperty =
            DependencyProperty.RegisterAttached(
                "LastPlayed",
                typeof(string),
                typeof(VersionElementHelper),
                new PropertyMetadata(""));

        public static string GetLastPlayed(DependencyObject element)
        {
            return (string)element.GetValue(LastPlayedProperty);
        }

        public static void SetLastPlayed(DependencyObject element, string value)
        {
            element.SetValue(LastPlayedProperty, value);
        }
    }

    public static class SkinCardHelper
    {
        public static readonly DependencyProperty SkinNameProperty =
            DependencyProperty.RegisterAttached(
                "SkinName",
                typeof(string),
                typeof(SkinCardHelper),
                new PropertyMetadata("Unknown"));

        public static string GetSkinName(DependencyObject element) =>
            (string)element.GetValue(SkinNameProperty);

        public static void SetSkinName(DependencyObject element, string value) =>
            element.SetValue(SkinNameProperty, value);
        
        public static readonly DependencyProperty SkinModelProperty =
            DependencyProperty.RegisterAttached(
                "SkinModel",
                typeof(string),
                typeof(SkinCardHelper),
                new PropertyMetadata("Classic"));

        public static string GetSkinModel(DependencyObject element) =>
            (string)element.GetValue(SkinModelProperty);

        public static void SetSkinModel(DependencyObject element, string value) =>
            element.SetValue(SkinModelProperty, value);
        
        public static readonly DependencyProperty SkinPreviewProperty =
            DependencyProperty.RegisterAttached(
                "SkinPreview",
                typeof(ImageSource),
                typeof(SkinCardHelper),
                new PropertyMetadata(default(ImageSource)));

        public static ImageSource GetSkinPreview(DependencyObject element) =>
            (ImageSource)element.GetValue(SkinPreviewProperty);

        public static void SetSkinPreview(DependencyObject element, ImageSource value) =>
            element.SetValue(SkinPreviewProperty, value);
    }
    
    public static class AccountCardHelper
    {
        public static readonly DependencyProperty AccountNameProperty =
            DependencyProperty.RegisterAttached(
                "AccountName",
                typeof(string),
                typeof(AccountCardHelper),
                new PropertyMetadata("Unknown"));

        public static string GetAccountName(DependencyObject element) =>
            (string)element.GetValue(AccountNameProperty);

        public static void SetAccountName(DependencyObject element, string value) =>
            element.SetValue(AccountNameProperty, value);
        
        public static readonly DependencyProperty AccountTypeProperty =
            DependencyProperty.RegisterAttached(
                "AccountType",
                typeof(AccountType),
                typeof(AccountCardHelper),
                new PropertyMetadata(AccountType.Microsoft));

        public static AccountType GetAccountType(DependencyObject element) =>
            (AccountType)element.GetValue(AccountTypeProperty);

        public static void SetAccountType(DependencyObject element, AccountType value) =>
            element.SetValue(AccountTypeProperty, value);
    }
    
}