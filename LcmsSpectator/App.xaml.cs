using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Composition;

namespace LcmsSpectator
{
    using System.Windows;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Readers;
    using LcmsSpectator.ViewModels;
    using LcmsSpectator.Views;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Event handler that is triggered on application start up.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            // Warm up InformedProteomics Averagine using arbitrary mass
            Task.Run(() => Averagine.GetIsotopomerEnvelopeFromNominalMass(50000));

            var mainWindow = new MainWindow();
            var mainWindowVm = new MainWindowViewModel(new MainDialogService());
            mainWindow.DataContext = mainWindowVm;
            mainWindow.Show();
        }
    }
}
