using System.IO;
using System.Windows;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Models;
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
            //Loaded += (o, e) => { ScanView.ContextMenu.DataContext = DataContext;  };
            //Loaded += Window_OnLoaded;
        }

        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("layout.xml"))
            {
                var serializer = new XmlLayoutSerializer(AvDock);
                using (var stream = new StreamReader("layout.xml"))
                {
                    serializer.LayoutSerializationCallback += (s, args) =>
                    {
                        args.Content = Application.Current.MainWindow.FindName(args.Model.ContentId);
                    };
                    serializer.Deserialize(stream);
                }   
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
