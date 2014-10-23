using System;
using System.IO;
using System.Windows;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Models;
using Microsoft.Win32;
using Xceed.Wpf.AvalonDock.Layout.Serialization;
using TreeView = System.Windows.Controls.TreeView;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for MsPathViewer.xaml
    /// </summary>
    public partial class MainWindow: Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            var fileName = "layout.xml";
            if (File.Exists(fileName)) LoadLayout(fileName);
            else
            {
                var result = MessageBox.Show("Cannot find layout.xml. Would you like to search for it?", "", MessageBoxButton.YesNo,
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
                        args.Content = Application.Current.MainWindow.FindName(args.Model.ContentId);
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
            /*using (var fs = new StreamWriter("layout.xml"))
            {
                var xmlLayout = new XmlLayoutSerializer(AvDock);
                xmlLayout.Serialize(fs);
            }*/
        }
    }
}
