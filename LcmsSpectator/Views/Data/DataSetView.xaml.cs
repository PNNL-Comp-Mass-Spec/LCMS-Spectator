﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSetView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for DataSetView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InformedProteomics.Backend.Data.Sequence;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace LcmsSpectator.Views.Data
{
    /// <summary>
    /// Interaction logic for DataSetView.xaml
    /// </summary>
    public partial class DataSetView : UserControl
    {
        /// <summary>
        /// The selected item in the ScanDataGrid.
        /// </summary>
        private object selectedItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSetView"/> class.
        /// </summary>
        public DataSetView()
        {
            InitializeComponent();

            ScanDataGrid.SelectionChanged += (o, e) =>
            {
                var item = ScanDataGrid.SelectedItem;
                if (ScanDataGrid.SelectedItem == null && selectedItem != null)
                {
                    item = selectedItem;
                }

                selectedItem = item;
                ScanDataGrid.ScrollIntoView(item);
                ScanDataGrid.UpdateLayout();
            };

            DataContextChanged += (o, e) => SpectrumView.StartMsPfSearch.DataContext = DataContext;
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
            var fileName = "layoutdoc.xml";
            if (File.Exists(fileName))
            {
                LoadLayout(fileName);
            }
            else
            {
                var result = MessageBox.Show(
                    "Cannot find layoutdoc.xml. Would you like to search for it?",
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
                    serializer.LayoutSerializationCallback += (s, args) => args.Content = FindName(args.Model.ContentId);
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
            ////using (var fs = new StreamWriter("layoutdoc1.xml"))
            ////{
            ////    var xmlLayout = new XmlLayoutSerializer(AvDock);
            ////    xmlLayout.Serialize(fs);
            ////}
        }

        /// <summary>
        /// Event handler for InsertModButton click.
        /// Inserts the selected modification into sequence string.
        /// </summary>
        /// <param name="sender">The sender Button</param>
        /// <param name="e">The event arguments.</param>
        private void InsertModButton_OnClick(object sender, RoutedEventArgs e)
        {
            InsertModification();
        }

        /// <summary>
        /// Inserts the selected modification into sequence string.
        /// </summary>
        private void InsertModification()
        {
            var selectedMod = (Modification)ModificationList.SelectedItem;
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
                InsertModification();
            }
        }
    }
}
