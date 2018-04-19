using System.Windows;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Readers;
using LcmsSpectator.ViewModels;
using LcmsSpectator.Views;

namespace LcmsSpectator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Event handler that is triggered on application start up.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            var mainWindowVm = new MainWindowViewModel(new MainDialogService(), new DataReader());
            mainWindow.DataContext = mainWindowVm;
            mainWindow.Show();
        }
    }
}
