using System.Windows;
using LcmsSpectator.ViewModels.Data;
using WpfExtras;

namespace LcmsSpectator.Views.Data
{
    public class ScanViewBindingProxy : BindingProxy<ScanViewModel>
    {
        protected override BindingProxy<ScanViewModel> CreateNewInstance()
        {
            return new ScanViewBindingProxy();
        }

        /// <summary>
        /// Data object for binding
        /// </summary>
        public override ScanViewModel Data
        {
            get => (ScanViewModel)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        /// <summary>
        /// DependencyProperty definition for Data
        /// </summary>
        public new static readonly DependencyProperty DataProperty = BindingProxy<ScanViewModel>.DataProperty.AddOwner(typeof(ScanViewBindingProxy));
    }
}
