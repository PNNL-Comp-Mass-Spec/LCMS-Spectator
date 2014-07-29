using System.Windows;
using System.Windows.Forms;
using Ookii.Dialogs;
using MessageBox = System.Windows.MessageBox;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for OpenFile.xaml
    /// </summary>
    public partial class OpenFile
    {
        public string IdFileName { get; set; }
        public string RawFileName { get; set; }
        public OpenFile()
        {
            InitializeComponent();
            IdFileName = "";
            RawFileName = "";
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BrowseIdFile(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaOpenFileDialog { DefaultExt = ".txt", Filter = @"IC ID Files (*.tsv)|*.tsv" };

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                IdFileBox.Text = dialog.FileName;
            }
        }

        private void BrowseRawFile(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaOpenFileDialog { DefaultExt = ".raw", Filter = @"Raw Files (*.raw)|*.raw" };

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                RawFileBox.Text = dialog.FileName;
            }
        }

        private void UpdateFiles(object sender, RoutedEventArgs e)
        {
            if (IdFileBox.Text == "" || RawFileBox.Text == "")
            {
                MessageBox.Show("Missing files.");
                return;
            }
            IdFileName = IdFileBox.Text;
            RawFileName = RawFileBox.Text;
            Close();
        }
    }
}
