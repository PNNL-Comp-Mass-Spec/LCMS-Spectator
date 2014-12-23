using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for DataSetView.xaml
    /// </summary>
    public partial class DataSetView : UserControl
    {
        public DataSetView()
        {
            InitializeComponent();
        }

        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            var fileName = "layoutdoc.xml";
            if (File.Exists(fileName)) LoadLayout(fileName);
            else
            {
                var result = MessageBox.Show("Cannot find layoutdoc.xml. Would you like to search for it?", "", MessageBoxButton.YesNo,
                    MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.Yes)
                {
                    var dialog = new OpenFileDialog { DefaultExt = ".xml", Filter = @"XML Layout Files (*.xml)|*.xml" };

                    var openResult = dialog.ShowDialog();
                    if (openResult == true)
                    {
                        fileName = dialog.FileName;
                        LoadLayout(fileName);
                    }
                    else
                    {
                        MessageBox.Show("Without layout file, LcMsSpectator may not display correctly.", "",
    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                else
                {
                    MessageBox.Show("Without layout file, LcMsSpectator may not display correctly.", "",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void LoadLayout(string layoutFile)
        {
            try
            {
                var serializer = new XmlLayoutSerializer(AvDock);
                using (var stream = new StreamReader(layoutFile))
                {
                    serializer.LayoutSerializationCallback += (s, args) =>
                    {
                        args.Content = this.FindName(args.Model.ContentId);
                    };
                    serializer.Deserialize(stream);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Could not load layout file.", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DockingManager_OnUnloaded(object sender, RoutedEventArgs e)
        {
            /*using (var fs = new StreamWriter("layoutdoc1.xml"))
            {
                var xmlLayout = new XmlLayoutSerializer(AvDock);
                xmlLayout.Serialize(fs);
            }*/
        }
    }
}
