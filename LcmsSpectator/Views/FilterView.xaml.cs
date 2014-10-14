using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for FilterView.xaml
    /// </summary>
    public partial class FilterView : Window
    {
        public FilterView()
        {
            EventManager.RegisterClassHandler(typeof(TextBox), PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyHandleMouseButton), true); 
            EventManager.RegisterClassHandler(typeof(TextBox), GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText), true);
            Loaded += (o, e) =>
            {
                var element = FindName("FilterValue");
                var comboBox = element as ComboBox;
                FocusManager.SetFocusedElement(this, comboBox);
            };
            InitializeComponent();
        }

        private static void SelectivelyHandleMouseButton(object sender, MouseButtonEventArgs e)
        {
            var textbox = (sender as TextBox); 
            if (textbox != null && !textbox.IsKeyboardFocusWithin)
            {
                if (e.OriginalSource.GetType().Name == "FilterValue")
                {
                    e.Handled = true; textbox.Focus();
                } 
            }
        }

        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null) textBox.SelectAll();
        }
    }
}
