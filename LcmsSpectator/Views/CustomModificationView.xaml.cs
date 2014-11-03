using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for CustomModificationView.xaml
    /// </summary>
    public partial class CustomModificationView : Window
    {
        public CustomModificationView()
        {
            EventManager.RegisterClassHandler(typeof(TextBox), PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyHandleMouseButton), true);
            EventManager.RegisterClassHandler(typeof(TextBox), GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText), true);
            InitializeComponent();
        }

        private static void SelectivelyHandleMouseButton(object sender, MouseButtonEventArgs e)
        {
            var textbox = (sender as TextBox);
            if (textbox != null && !textbox.IsKeyboardFocusWithin)
            {
                var name = e.OriginalSource.GetType().Name;
                e.Handled = true; textbox.Focus();
            }
        }

        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null) textBox.SelectAll();
        }
    }
}
