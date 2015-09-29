// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSetView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for DataSetView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using LcmsSpectator.ViewModels.Data;
using LcmsSpectator.ViewModels.Dataset;

namespace LcmsSpectator.Views
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using InformedProteomics.Backend.Data.Sequence;
    using Microsoft.Win32;
    using Xceed.Wpf.AvalonDock.Layout.Serialization;
    
    /// <summary>
    /// Interaction logic for DataSetView.xaml
    /// </summary>
    public partial class DataSetView : UserControl
    {
        /// <summary>
        /// The path to the default layout file.
        /// </summary>
        private const string DefaultLayout = "layoutdoc.xml";

        /// <summary>
        /// Lock for accessing layout file.
        /// </summary>
        private readonly object layoutFileLock;

        /// <summary>
        /// The selected item in the ScanDataGrid.
        /// </summary>
        private object selectedItem;

        /// <summary>
        /// The view model for this dataset view.
        /// </summary>
        private DatasetViewModel viewModel;

        /// <summary>
        /// A value that indicates whether this view has been loaded yet.
        /// </summary>
        private bool isLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSetView"/> class.
        /// </summary>
        public DataSetView()
        {
            this.InitializeComponent();

            this.layoutFileLock = new object();

            // Scroll to the selected item in datagrid when an item is selected from outside the GUI.
            ScanDataGrid.SelectionChanged += (o, e) =>
            {
                object item = ScanDataGrid.SelectedItem;
                if (ScanDataGrid.SelectedItem == null && selectedItem != null)
                {
                    item = selectedItem;
                }

                selectedItem = item;
                ScanDataGrid.ScrollIntoView(item);
                ScanDataGrid.UpdateLayout();
            };

            // Update layout when datacontext changes.
            this.DataContextChanged += (o, e) =>
            {
                this.viewModel = this.DataContext as DatasetViewModel;
                if (viewModel != null)
                {
                    if (this.isLoaded)
                    {
                        this.LoadLayout();
                    }

                    this.SpectrumView.StartMsPfSearch.DataContext = this.DataContext;  
                }  
            };

            // Load layout when docking manager has loaded.
            this.AvDock.Loaded += (o, e) =>
            {
                this.viewModel = this.DataContext as DatasetViewModel;
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
            if (this.viewModel != null && !string.IsNullOrEmpty(this.viewModel.DatasetInfo.LayoutFile) &&
                File.Exists(this.viewModel.DatasetInfo.LayoutFile))
            {
                layoutPath = this.viewModel.DatasetInfo.LayoutFile;
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
            if (this.viewModel == null || string.IsNullOrEmpty(this.viewModel.DatasetInfo.LayoutFile))
            {
                return;
            }

            var directory = Path.GetDirectoryName(this.viewModel.DatasetInfo.LayoutFile);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            lock (this.layoutFileLock)
            {
                Directory.CreateDirectory(directory);
                using (var fs = new StreamWriter(this.viewModel.DatasetInfo.LayoutFile))
                {
                    var xmlLayout = new XmlLayoutSerializer(AvDock);
                    xmlLayout.Serialize(fs);
                }   
            }
        }

        /// <summary>
        /// Event handler for InsertModButton click.
        /// Inserts the selected modification into sequence string.
        /// </summary>
        /// <param name="sender">The sender Button</param>
        /// <param name="e">The event arguments.</param>
        private void InsertModButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.InsertModification();
        }

        /// <summary>
        /// Inserts the selected modification into sequence string.
        /// </summary>
        private void InsertModification()
        {
            var selectedMod = ModificationList.SelectedItem as Modification;
            if (selectedMod == null)
            {
                MessageBox.Show("Invalid modification.");
                return;
            }

            var position = Sequence.CaretIndex;
            string modStr;
            if (Sequence.Text.Contains("+"))
            {
                var sign = selectedMod.Mass > 0 ? "+" : "-";
                if (!(selectedMod.Name.StartsWith("+") || selectedMod.Name.StartsWith("-")))
                {
                    var roundedMass = Math.Round(selectedMod.Mass, 3);
                    modStr = string.Format("{0}{1}", sign, roundedMass);
                }
                else
                {
                    modStr = selectedMod.Name;
                }
            }
            else
            {
                modStr = string.Format("[{0}]", selectedMod.Name);
            }

            Sequence.Text = Sequence.Text.Insert(position, modStr);
        }

        /// <summary>
        /// Event handler for ModificationList OnKeyDown event.
        /// Inserts the selected modification into sequence string when Enter is pressed.
        /// </summary>
        /// <param name="sender">The sender ComboBox</param>
        /// <param name="e">The event arguments.</param>
        private void ModificationList_OnKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key;
            if (key == Key.Enter)
            {
                this.InsertModification();
            }
        }
    }
}
