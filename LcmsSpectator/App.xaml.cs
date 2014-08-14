using System.Windows;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels;
using LcmsSpectator.Views;

namespace LcmsSpectator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            var mainWindowVm = new MainWindowViewModel(new MainDialogService());
            mainWindow.DataContext = mainWindowVm;
            mainWindow.Show();
        }
    }
}
