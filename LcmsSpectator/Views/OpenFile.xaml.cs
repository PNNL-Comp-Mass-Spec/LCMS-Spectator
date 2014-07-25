using System.Windows;
using System.Windows.Forms;
using Ookii.Dialogs;
using MessageBox = System.Windows.MessageBox;

namespace MsPathViewer.Views
{
    /// <summary>
    /// Interaction logic for OpenFile.xaml
    /// </summary>
    public partial class OpenFile
    {
        public string ParamFileName { get; set; }
        public string IdFileName { get; set; }
        public string RawFileName { get; set; }
        public OpenFile()
        {
            InitializeComponent();
            ParamFileName = "";
            IdFileName = "";
            RawFileName = "";
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BrowseParamFile(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaOpenFileDialog { DefaultExt = ".txt", Filter = @"Param Files (*.param)|*.param" };

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ParamFileBox.Text = dialog.FileName;
            }
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
            if (ParamFileBox.Text == "" || IdFileBox.Text == "" || RawFileBox.Text == "")
            {
                MessageBox.Show("Missing files.");
                return;
            }
            ParamFileName = ParamFileBox.Text;
            IdFileName = IdFileBox.Text;
            RawFileName = RawFileBox.Text;
            Close();
        }
    }
}
