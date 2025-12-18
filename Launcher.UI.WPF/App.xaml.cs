using System.Configuration;
using System.Data;
using System.Windows;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.ViewModels;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Создаем сервисы (если они нужны)
        // var gameService = new LaunchService(); 
        // ...
        var themeService = new ThemeService();
        // 2. Создаем MainViewModel
        // (Если у нее есть зависимости в конструкторе, передай их сюда)
        
        var mainViewModel = new MainViewModel(themeService);
        
        // 3. Создаем Главное Окно
        var mainWindow = new MainWindow();

        // 🔥 4. ГЛАВНЫЙ МОМЕНТ: ПРИВЯЗКА 🔥
        // Мы говорим окну: "Твои данные - это вот этот класс mainViewModel"
        mainWindow.DataContext = mainViewModel;
        
        

        // 5. Показываем окно
        mainWindow.Show();
    }
}