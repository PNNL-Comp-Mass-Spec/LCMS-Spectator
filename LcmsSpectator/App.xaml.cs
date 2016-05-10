namespace LcmsSpectator
{
    using System.Windows;

    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Readers;
    using LcmsSpectator.ViewModels;
    using LcmsSpectator.ViewModels.Data;
    using LcmsSpectator.ViewModels.SequenceViewer;
    using LcmsSpectator.Views;
    using LcmsSpectator.Views.SequenceViewer;

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
