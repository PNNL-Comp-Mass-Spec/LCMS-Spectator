// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MvvmTreeView.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Microsoft has done everything in their power to make using WPF TreeViews a !@#$ing nightmare, especially
//   with MVVM. The SelectedItem Dependency property of the TreeView control is readonly, which prevents conventionally
//   binding it to a view model. This control exposes SelectedItemTargetProperty which is a nonreadonly dependency
//   property for SelectedItem, so it can be easily bound to a property in a view model.
//   Nothing happens when this dependency property is set (the selected item on the TreeView does not update),
//   so it is still essentially readonly.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

namespace LcmsSpectator.Controls
{
    /// <summary>
    /// Microsoft has done everything in their power to make using WPF TreeViews a !@#$ING nightmare, especially
    /// with MVVM. The SelectedItem Dependency property of the TreeView control is readonly, which prevents conventionally
    /// binding it to a view model. This control exposes SelectedItemTargetProperty which is a non-readonly dependency
    /// property for SelectedItem, so it can be easily bound to a property in a view model.
    /// Nothing happens when this dependency property is set (the selected item on the TreeView does not update),
    /// so it is still essentially readonly.
    /// </summary>
    public class MvvmTreeView : TreeView
    {
        /// <summary>
        /// Initializes static members of the <see cref="MvvmTreeView"/> class.
        /// </summary>
        static MvvmTreeView()
        {
            SelectedItemTargetProperty = DependencyProperty.Register("SelectedItemTarget", typeof(object), typeof(MvvmTreeView), null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MvvmTreeView"/> class.
        /// </summary>
        public MvvmTreeView()
        {
            SelectedItemChanged += MvvmTreeViewSelectedItemChanged;
        }

        /// <summary>
        /// Gets the dependency property that exposes the selected item of the tree view for binding to view model.
        /// </summary>
        public static DependencyProperty SelectedItemTargetProperty { get; }

        /// <summary>
        /// Gets or sets the item selected in the tree view.
        /// </summary>
        public object SelectedItemTarget
        {
            get => GetValue(SelectedItemTargetProperty);

            // XAML binding requires this to be public even though it doesn't even use it
            set => SetCurrentValue(SelectedItemTargetProperty, value);
        }

        /// <summary>
        /// An event handler that is triggered when the selected item of the tree view changes.
        /// </summary>
        /// <param name="sender">The sender TreeView.</param>
        /// <param name="e">The event arguments.</param>
        private void MvvmTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(sender is MvvmTreeView treeView))
            {
                return;
            }

            var selectedItem = treeView.SelectedItem;
            SelectedItemTarget = selectedItem;
        }
    }
}
