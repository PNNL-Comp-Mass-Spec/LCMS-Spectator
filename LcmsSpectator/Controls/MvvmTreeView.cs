using System.Windows;
using System.Windows.Controls;

namespace LcmsSpectator.Controls
{
    /// <summary>
    /// Microsoft has done everything in their power to make using WPF TreeViews a !@#$ing nightmare, especially
    /// with MVVM. The SelectedItem Dependency property of the TreeView control is readonly, which prevents conventionally
    /// binding it to a view model. This control exposes SelectedItemTargetProperty which is a nonreadonly dependency
    /// property for SelectedItem, so it can be easily bound to a property in a view model.
    /// Nothing happens when this dependency property is set (the selected item on the TreeView does not update),
    /// so it is still essentially readonly.
    /// </summary>
    public class MvvmTreeView: TreeView
    {
        public MvvmTreeView()
        {
            SelectedItemChanged += MvvmTreeView_SelectedItemChanged;
        }

        public object SelectedItemTarget
        {
            get
            {
                return ((GetValue(SelectedItemTargetProperty)));
            }
            set     // XAML binding requires this to be public even though it doesn't even use it
            {
                SetCurrentValue(SelectedItemTargetProperty, value);
            }
        }
        public static DependencyProperty SelectedItemTargetProperty = DependencyProperty.Register("SelectedItemTarget", typeof(object), typeof(MvvmTreeView), null);

        private void MvvmTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = sender as MvvmTreeView;
            if (treeView == null) return;
            var selectedItem = treeView.SelectedItem;
            SelectedItemTarget = selectedItem;
        }
    }
}
