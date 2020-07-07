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
            if (e.Exception.GetType() == typeof(InvalidOperationException) && e.Exception.Message.Contains("This PlotModel is already in use"))
            {
                // Exception: This PlotModel is already in use by some other PlotView control
                // See https://stackoverflow.com/a/26318147/1179467
                // This can occur if the user loads two datasets at the same time and switches between the two datasets
                // Related to this, when you load a second dataset, the identified proteins get appended to the Protein List, giving a combined list of results from both datasets; this likely contributes to the issue
                try
                {
                    MessageBox.Show(
                        "LCMS Spectator does not properly support having multiple datasets open at the same time. " +
                        "Please run multiple instances of this program if you need to view results from multiple datasets at the same time",
                        "PlotModel is already in use",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception)
                {
                    // Ignore errors here
                }
                Current.Shutdown();
                return;
            }

            if (e.Exception.GetType() == typeof(ReactiveUI.UnhandledErrorException) && e.Exception.InnerException != null)
            {
                ex = e.Exception.InnerException;
            }

            try
            {
                MessageBox.Show(ex.ToString(), "Unhandled Exception Caught", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
                // Ignore errors here
            }
            Current.Shutdown();
        }
    }
}
