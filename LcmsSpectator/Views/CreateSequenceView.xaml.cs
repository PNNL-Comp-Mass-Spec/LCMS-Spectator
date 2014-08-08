using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for CreateSequenceView.xaml
    /// </summary>
    public partial class CreateSequenceView : UserControl
    {
        public CreateSequenceView()
        {
            InitializeComponent();
        }

        private void InsertModButton_OnClick(object sender, RoutedEventArgs e)
        {
            InsertModification();
        }

        private void InsertModification()
        {
            var selectedMod = ModificationList.SelectedItem as Modification;
            if (selectedMod == null)
            {
                MessageBox.Show("Invalid modification.");
                return;
            }
            var position = Sequence.CaretIndex;
            var modStr = String.Format("[{0}]", selectedMod.Name);
            Sequence.Text = Sequence.Text.Insert(position, modStr);
        }

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
