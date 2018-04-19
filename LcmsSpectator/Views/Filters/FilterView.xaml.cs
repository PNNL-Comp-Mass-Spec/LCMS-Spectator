// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilterView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for FilterView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for FilterView.xaml
    /// </summary>
    public partial class FilterView : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterView"/> class.
        /// </summary>
        public FilterView()
        {
            EventManager.RegisterClassHandler(typeof(TextBox), PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyHandleMouseButton), true);
            EventManager.RegisterClassHandler(typeof(TextBox), GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText), true);
            Loaded += (o, e) =>
            {
                var element = FindName("FilterValue");
                var comboBox = element as ComboBox;
                FocusManager.SetFocusedElement(this, comboBox);

                var distinctItems = new HashSet<string>();
                foreach (var item in FilterValue.ItemsSource)
                {
                    if (item is string filterValue && !distinctItems.Contains(filterValue))
                    {
                        distinctItems.Add(filterValue);
                    }
                }

                FilterValue.ItemsSource = distinctItems;
            };

            InitializeComponent();
        }

        /// <summary>
        /// Event handler for PreviewLeftButtonDownEvent.
        /// Causes the sending TextBox to select all of its contents when focused (when clicked).
        /// </summary>
        /// <param name="sender">The sender TextBox.</param>
        /// <param name="e">The event arguments.</param>
        private static void SelectivelyHandleMouseButton(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textbox && !textbox.IsKeyboardFocusWithin)
            {
                if (e.OriginalSource.GetType().Name == "FilterValue")
                {
                    e.Handled = true;
                    textbox.Focus();
                }
            }
        }

        /// <summary>
        /// Event handler for GotKeyBoardFocusEvent
        /// Causes the sending TextBox to select all of its contents when focused (when tabbed to).
        /// </summary>
        /// <param name="sender">The sender TextBox.</param>
        /// <param name="e">The event arguments.</param>
        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}
