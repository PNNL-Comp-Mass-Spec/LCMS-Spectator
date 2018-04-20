// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for MsPathViewer.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for MsPathViewer.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            ProgressRow.Height = new GridLength(0, GridUnitType.Pixel);
            FileLoadProgress.IsVisibleChanged += (s, e) =>
                {
                    ProgressRow.Height = FileLoadProgress.IsVisible ?
                                                    new GridLength(30, GridUnitType.Pixel) :
                                                    new GridLength(0, GridUnitType.Pixel);
                };
        }

        /// <summary>
        /// Event handler for when the DockingManager is unloaded.
        /// Reads layout serialization file.
        /// </summary>
        /// <param name="sender">The sender DockingManager.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            /*
            var fileName = "layout.xml";
            if (File.Exists(fileName))
            {
                LoadLayout(fileName);
            }
            else
            {
                var result = MessageBox.Show(
                    "Cannot find layout.xml. Would you like to search for it?",
                    string.Empty,
                    MessageBoxButton.YesNo,
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
                        MessageBox.Show(
                            "Without layout file, LcMsSpectator may not display correctly.",
                            string.Empty,
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Without layout file, LcMsSpectator may not display correctly.",
                        string.Empty,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                }
            }
            */
        }

        /// <summary>
        /// Load layout from serialized layout string.
        /// </summary>
        /// <param name="layout">Serialized layout string.</param>
        private void LoadLayout(string layout)
        {
            try
            {
                var serializer = new XmlLayoutSerializer(AvDock);
                using (var stream = new StreamReader(layout))
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
                MessageBox.Show("Could not load layout file.", string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for when the DockingManager is unloaded.
        /// Writes layout serialization file.
        /// </summary>
        /// <param name="sender">The sender DockingManager.</param>
        /// <param name="e">The event arguments.</param>
        private void DockingManager_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ////using (var fs = new StreamWriter("layout1.xml"))
            ////{
            ////    var xmlLayout = new XmlLayoutSerializer(AvDock);
            ////    xmlLayout.Serialize(fs);
            ////}
        }
    }
}
