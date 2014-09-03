using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LcmsSpectator.Controls
{
    public class MultiSelectListBox : ListBox
    {
        public MultiSelectListBox()
        {
            _internalChange = false;
            SelectionMode = SelectionMode.Extended;
            SelectedItemsSource = SelectedItems.OfType<object>().ToList();
            SelectionChanged += SelectedItemsChanged;
        }

        private void SelectedItemsChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as MultiSelectListBox;
            if (listBox == null) return;
            if (!listBox._internalChange) SelectedItemsSource = listBox.SelectedItems.OfType<object>().ToList();
        }

        private static void OnSelectedItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBox = (MultiSelectListBox)d;
            if (listBox.SelectedItemsSource == null) return;
            listBox._internalChange = true;
            listBox.SelectedItems.Clear();
            foreach (var item in listBox.SelectedItemsSource) listBox.SelectedItems.Add(item);
            listBox._internalChange = false;
        }

        public IList SelectedItemsSource
        {
            get
            {
                return ((IList)(GetValue(SelectedItemsSourceProperty)));
            }
            set
            {
                SetCurrentValue(SelectedItemsSourceProperty, value);
            }
        }

        public static DependencyProperty SelectedItemsSourceProperty = DependencyProperty.Register("SelectedItemsSource", typeof(IList), typeof(MultiSelectListBox),
                                                                   new FrameworkPropertyMetadata(OnSelectedItemsSourceChanged));
        private bool _internalChange;
    }
}
