using System.Collections;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Config;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class HeavyModificationsViewModel: ReactiveObject
    {
        public ReactiveList<Modification> Modifications { get; private set; }
        public IList SelectedLightModifications { get; set; }
        public IList SelectedHeavyModifications { get; set; }

        public HeavyModificationsViewModel()
        {
            Modifications = new ReactiveList<Modification>
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
