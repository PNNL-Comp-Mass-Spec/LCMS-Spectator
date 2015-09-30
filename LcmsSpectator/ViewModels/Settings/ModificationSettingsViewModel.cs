using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Settings
{
    using LcmsSpectator.ViewModels.Dataset;

    public class ModificationSettingsViewModel : BaseSettingsViewModel
    {
        public ModificationSettingsViewModel(IEnumerable<DatasetViewModel> datasets) : base(datasets)
        {
        }
    }
}
