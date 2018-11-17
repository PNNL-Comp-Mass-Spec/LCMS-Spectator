using System;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
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
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            var mainWindow = new MainWindow();
            var mainWindowVm = new MainWindowViewModel(new MainDialogService(), new DataReader());
            mainWindow.DataContext = mainWindowVm;
            mainWindow.Show();
        }

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            // TODO: This is the best place to notify about exceptions, but catches others we don't want to worry about. It also doesn't let us say that it has been handled.
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            if (e.Exception.GetType() == typeof(ReactiveUI.UnhandledErrorException) && e.Exception.InnerException != null)
            {
                ex = e.Exception.InnerException;
            }
            MessageBox.Show(ex.ToString(), "Unhandled Exception Caught", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
