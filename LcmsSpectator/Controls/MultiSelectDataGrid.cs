using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LcmsSpectator.Controls
{
    public class MultiSelectDataGrid: DataGrid
    {
        public MultiSelectDataGrid()
        {
            _internalChange = false;
            SelectedItemsSource = SelectedItems.OfType<object>().ToList();
            SelectionChanged += SelectedItemsChanged;
        }

        private void SelectedItemsChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as MultiSelectDataGrid;
            if (dataGrid == null) return;
            if (!dataGrid._internalChange) SelectedItemsSource = dataGrid.SelectedItems.OfType<object>().ToList();
        }

        private static void OnSelectedItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var datagrid = (MultiSelectDataGrid) d;
            if (datagrid.SelectedItemsSource == null) return;
            datagrid._internalChange = true;
            datagrid.SelectedItems.Clear();
            foreach (var item in datagrid.SelectedItemsSource) datagrid.SelectedItems.Add(item);
            datagrid._internalChange = false;
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

        public static DependencyProperty SelectedItemsSourceProperty = DependencyProperty.Register("SelectedItemsSource", typeof(IList), typeof(MultiSelectDataGrid),
                                                                   new FrameworkPropertyMetadata(OnSelectedItemsSourceChanged));

        private bool _internalChange;
    }
}
