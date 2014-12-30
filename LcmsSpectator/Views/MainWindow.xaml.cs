using System;
using System.IO;
using System.Windows;
using LcmsSpectator.DialogServices;
using Microsoft.Win32;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

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
            //using (var fs = new StreamWriter("layout1.xml"))
            //{
            //    var xmlLayout = new XmlLayoutSerializer(AvDock);
            //    xmlLayout.Serialize(fs);
            //}
        }

        //private void LoadLayout_MenuItem_OnClick(object sender, RoutedEventArgs e)
        //{
        //    var dialogService = new DialogService();
        //    var fileName = dialogService.OpenFile(".xml", @"XML Layout Files (*.xml)|*.xml");
        //    if (!String.IsNullOrEmpty(fileName))
        //    {
        //        try
        //        {
        //            LoadLayout(fileName);
        //        }
        //        catch (Exception)
        //        {
        //            MessageBox.Show("Could not load layout.");
        //            throw;
        //        }
        //    }
        //}

        //private void SaveLayout_MenuItem_OnClick(object sender, RoutedEventArgs e)
        //{
        //    var dialogService = new DialogService();
        //    var fileName = dialogService.SaveFile(".xml", @"XML Layout Files (*.xml)|*.xml");
        //    if (!String.IsNullOrEmpty(fileName))
        //    {
        //        using (var fs = new StreamWriter(fileName))
        //        {
        //            var xmlLayout = new XmlLayoutSerializer(AvDock);
        //            xmlLayout.Serialize(fs);
        //        }

        //    }
        //}
    }
}
