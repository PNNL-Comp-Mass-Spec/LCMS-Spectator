using System.Windows;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Models;
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
            Loaded += (o, e) => { ScanView.ContextMenu.DataContext = DataContext;  };
        }
    }
}
