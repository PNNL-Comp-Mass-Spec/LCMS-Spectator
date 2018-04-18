// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateSequenceView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for CreateSequenceView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views.Data
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using InformedProteomics.Backend.Data.Sequence;

    /// <summary>
    /// Interaction logic for CreateSequenceView.xaml
    /// </summary>
    public partial class CreateSequenceView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSequenceView"/> class.
        /// </summary>
        public CreateSequenceView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event handler for the insert modification button click.
        /// </summary>
        /// <param name="sender">The sender Button.</param>
        /// <param name="e">The event arguments.</param>
        private void InsertModButton_OnClick(object sender, RoutedEventArgs e)
        {
            InsertModification();
        }

        /// <summary>
        /// Insert the selected modification into the SequenceText at the position of the carat.
        /// </summary>
        private void InsertModification()
        {
            if (!(ModificationList.SelectedItem is Modification selectedMod))
            {
                MessageBox.Show("Invalid modification.");
                return;
            }

            var position = Sequence.CaretIndex;
            var modStr = string.Format("[{0}]", selectedMod.Name);
            Sequence.Text = Sequence.Text.Insert(position, modStr);
        }

        /// <summary>
        /// Event handler for modification list ComboBox key down.
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
