// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for MsPathViewer.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views
{
    using System.IO;
    using System.Windows;

    using LcmsSpectator.ViewModels;

    using Xceed.Wpf.AvalonDock.Layout.Serialization;

    /// <summary>
    /// Interaction logic for MsPathViewer.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The path to the default layout file.
        /// </summary>
        private const string DefaultLayout = "layout.xml";

        /// <summary>
        /// Lock for accessing layout file.
        /// </summary>
        private readonly object layoutFileLock;

        /// <summary>
        /// The view model for this dataset view.
        /// </summary>
        private MainWindowViewModel viewModel;

        /// <summary>
        /// A value that indicates whether this view has been loaded yet.
        /// </summary>
        private bool isLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            this.layoutFileLock = new object();

            this.ProgressRow.Height = new GridLength(0, GridUnitType.Pixel);
            this.FileLoadProgress.IsVisibleChanged += (s, e) =>
            {
                this.ProgressRow.Height = this.FileLoadProgress.IsVisible ? 
                                                new GridLength(30, GridUnitType.Pixel) : 
                                                new GridLength(0, GridUnitType.Pixel);
            };

            // Update layout when datacontext changes.
            this.DataContextChanged += (o, e) =>
            {
                this.viewModel = this.DataContext as MainWindowViewModel;
                if (viewModel != null)
                {
                    if (this.isLoaded)
                    {
                        this.LoadLayout();
                    }
                }
            };

            // Load layout when docking manager has loaded.
            this.AvDock.Loaded += (o, e) =>
            {
                this.viewModel = this.DataContext as MainWindowViewModel;
                if (viewModel != null)
                {
                    this.isLoaded = true;
                    this.LoadLayout();
                }
            };

            // Save layout when the docking manager has been destroyed.
            this.AvDock.Unloaded += (o, e) => this.SaveLayout();
        }

        /// <summary>
        /// Load layout from layout file.
        /// </summary>
        private void LoadLayout()
        {
            string layoutPath = DefaultLayout;
            if (this.viewModel != null && !string.IsNullOrEmpty(this.viewModel.ProjectManager.ProjectInfo.LayoutFilePath) &&
                File.Exists(this.viewModel.ProjectManager.ProjectInfo.LayoutFilePath))
            {
                layoutPath = this.viewModel.ProjectManager.ProjectInfo.LayoutFilePath;
            }

            this.LoadLayout(layoutPath);
        }

        /// <summary>
        /// Load layout from layout file.
        /// </summary>
        /// <param name="layout">The path to the layout file.</param>
        private void LoadLayout(string layout)
        {
            var serializer = new XmlLayoutSerializer(AvDock);

            lock (this.layoutFileLock)
            {
                using (var stream = new StreamReader(layout))
                {
                    serializer.LayoutSerializationCallback += (s, args) =>
                    {
                        args.Content = this.FindName(args.Model.ContentId);
                    };

                    serializer.Deserialize(stream);
                }
            }
        }

        /// <summary>
        /// Save layout to layout file.
        /// </summary>
        private void SaveLayout()
        {
            if (this.viewModel == null || string.IsNullOrEmpty(this.viewModel.ProjectManager.ProjectInfo.LayoutFilePath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(this.viewModel.ProjectManager.ProjectInfo.LayoutFilePath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            lock (this.layoutFileLock)
            {
                Directory.CreateDirectory(directory);
                using (var fs = new StreamWriter(this.viewModel.ProjectManager.ProjectInfo.LayoutFilePath))
                {
                    var xmlLayout = new XmlLayoutSerializer(AvDock);
                    xmlLayout.Serialize(fs);
                }
            }
        }
    }
}
