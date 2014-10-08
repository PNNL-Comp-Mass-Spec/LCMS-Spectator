using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectatorModels.Config;

namespace LcmsSpectator.ViewModels
{
    public class HeavyModificationsViewModel: ViewModelBase
    {
        public ObservableCollection<Modification> Modifications { get; private set; }
        public IList SelectedLightModifications { get; set; }
        public IList SelectedHeavyModifications { get; set; }

        public HeavyModificationsViewModel()
        {
            Modifications = new ObservableCollection<Modification>
            {
                Modification.LysToHeavyLys,
                Modification.ArgToHeavyArg
            };

            SelectedLightModifications = IcParameters.Instance.LightModifications;
            SelectedHeavyModifications = IcParameters.Instance.HeavyModifications;
        }

        public void Save()
        {
            IcParameters.Instance.LightModifications = SelectedLightModifications.Cast<Modification>().ToList();
            IcParameters.Instance.HeavyModifications = SelectedHeavyModifications.Cast<Modification>().ToList();
        }
    }
}
