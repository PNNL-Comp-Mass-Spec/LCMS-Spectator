using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using LcmsSpectator.Models;
using LcmsSpectator.Utils;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Data
{
    public class FragmentationSequenceViewModel : ReactiveObject, IFragmentationSequenceViewModel
    {
        /// <summary>
        /// The underlying fragmentation sequence.
        /// </summary>
        private FragmentationSequence fragmentationSequence;

        /// <summary>
        /// The heavy peptide modifications.
        /// </summary>
        private SearchModification[] heavyModifications;

        /// <summary>
        /// The LabeledIonViewModels for this sequence.
        /// </summary>
        private LabeledIonViewModel[] labeledIonViewModels;

        /// <summary>
        /// The selected ion types.
        /// </summary>
        private IonType[] selectedIonTypes;

        /// <summary>
        /// A value indicating whether to add precursor ions to labeled ion lists.
        /// </summary>
        private bool addPrecursorIons;

        /// <summary>
        /// Initializes a new instance of the <see cref="FragmentationSequenceViewModel"/> class.
        /// </summary>
        public FragmentationSequenceViewModel()
        {
            FragmentationSequence = new FragmentationSequence(
                new Sequence(new List<AminoAcid>()),
                1,
                null,
                ActivationMethod.HCD);

            var baseIonTypes = BaseIonType.AllBaseIonTypes.Select(
                    bit =>
                    new BaseIonTypeViewModel
                    {
                        BaseIonType = bit,
                        IsSelected = bit == BaseIonType.B || bit == BaseIonType.Y
                    });

            BaseIonTypes = new ReactiveList<BaseIonTypeViewModel>(baseIonTypes) { ChangeTrackingEnabled = true };

            NeutralLosses = new ReactiveList<NeutralLossViewModel>
            {
                new NeutralLossViewModel { NeutralLoss = NeutralLoss.NoLoss, IsSelected = true },
                new NeutralLossViewModel { NeutralLoss = NeutralLoss.H2O },
                new NeutralLossViewModel { NeutralLoss = NeutralLoss.NH3 }
            };

            NeutralLosses.ChangeTrackingEnabled = true;

            HeavyModifications = new SearchModification[0];
            LabeledIonViewModels = new LabeledIonViewModel[0];
            SelectedIonTypes = new IonType[0];

            AddPrecursorIons = true;

            // HideAllIonsCommand deselects all ion types and neutral losses.
            var hideAllIonsCommand = ReactiveCommand.Create();
            hideAllIonsCommand.Subscribe(_ =>
            {
                AddPrecursorIons = false;
                foreach (var baseIonType in BaseIonTypes)
                {
                    baseIonType.IsSelected = false;
                }

                foreach (var neutralLoss in NeutralLosses)
                {
                    neutralLoss.IsSelected = neutralLoss.NeutralLoss == NeutralLoss.NoLoss && neutralLoss.IsSelected;
                }
            });
            HideAllIonsCommand = hideAllIonsCommand;

            // When Base Ion Types are selected/deselected, update ion types.
            BaseIonTypes.ItemChanged.Where(x => x.PropertyName == "IsSelected")
                .Select(_ => GetIonTypes())
                .Subscribe(ionTypes => SelectedIonTypes = ionTypes);

            // When Neutral Losses are selected/deselected, update ion types
            NeutralLosses.ItemChanged.Where(x => x.PropertyName == "IsSelected")
                .Select(_ => GetIonTypes())
                .Subscribe(ionTypes => SelectedIonTypes = ionTypes);

            // When FragmentationSequence is set, select IonTypes for ActivationMethod.
            this.WhenAnyValue(x => x.FragmentationSequence)
                .Where(fragSeq => fragSeq != null)
                .Subscribe(fragSeq => SetActivationMethod(fragSeq.ActivationMethod));

            // When fragmentation sequence changes, update labeled ions
            this.WhenAnyValue(x => x.FragmentationSequence, x => x.SelectedIonTypes, x => x.HeavyModifications, x => x.AddPrecursorIons)
                .SelectMany(async _ => await GetLabeledIonViewModels())
                .Subscribe(livms => LabeledIonViewModels = livms);

            SelectAllIonsCommand = ReactiveCommand.Create();
            SelectAllIonsCommand.Subscribe(_ =>
            {
                foreach (var ion in BaseIonTypes)
                {
                    ion.IsSelected = true;
                }

                AddPrecursorIons = true;
            });

            IcParameters.Instance.WhenAnyValue(x => x.CidHcdIonTypes, x => x.EtdIonTypes)
                                 .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                                 .Where(_ => FragmentationSequence != null)
                                 .Subscribe(_ => SetActivationMethod(FragmentationSequence.ActivationMethod));
        }

        /// <summary>
        /// Gets or sets the underlying fragmentation sequence.
        /// </summary>
        public FragmentationSequence FragmentationSequence
        {
            get => fragmentationSequence;
            set => this.RaiseAndSetIfChanged(ref fragmentationSequence, value);
        }

        /// <summary>
        /// Gets a command that deselects all ion types.
        /// </summary>
        public IReactiveCommand HideAllIonsCommand { get; }

        /// <summary>
        /// Gets a command that selects all ion types.
        /// </summary>
        public ReactiveCommand<object> SelectAllIonsCommand { get; }

        /// <summary>
        /// Gets the list of possible base ion types.
        /// </summary>
        public ReactiveList<BaseIonTypeViewModel> BaseIonTypes { get; }

        /// <summary>
        /// Gets the list of possible neutral losses.
        /// </summary>
        public ReactiveList<NeutralLossViewModel> NeutralLosses { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to add precursor ions to labeled ion lists.
        /// </summary>
        public bool AddPrecursorIons
        {
            get => addPrecursorIons;
            set => this.RaiseAndSetIfChanged(ref addPrecursorIons, value);
        }

        /// <summary>
        /// Gets or sets the heavy peptide modifications.
        /// </summary>
        public SearchModification[] HeavyModifications
        {
            get => heavyModifications;
            set => this.RaiseAndSetIfChanged(ref heavyModifications, value);
        }

        /// <summary>
        /// Gets the LabeledIonViewModels for this sequence.
        /// </summary>
        public LabeledIonViewModel[] LabeledIonViewModels
        {
            get => labeledIonViewModels;
            private set => this.RaiseAndSetIfChanged(ref labeledIonViewModels, value);
        }

        /// <summary>
        /// Gets the selected ion types.
        /// </summary>
        public IonType[] SelectedIonTypes
        {
            get => selectedIonTypes;
            private set => this.RaiseAndSetIfChanged(ref selectedIonTypes, value);
        }

        /// <summary>
        /// Gets the labeled ion view models for the sequence.
        /// </summary>
        /// <returns>Task that returns the labeled ion view models for the sequence.</returns>
        private async Task<LabeledIonViewModel[]> GetLabeledIonViewModels()
        {
            var ionTypes = SelectedIonTypes;
            if (SelectedIonTypes == null || SelectedIonTypes.Length == 0)
            {
                ionTypes = GetIonTypes();
            }

            var fragmentIons =
                await fragmentationSequence.GetFragmentLabelsAsync(ionTypes, HeavyModifications);

            if (AddPrecursorIons)
            {
                var precursorIons = await FragmentationSequence.GetChargePrecursorLabelsAsync(HeavyModifications);
                fragmentIons.AddRange(precursorIons);
            }

            return fragmentIons.ToArray();
        }

        /// <summary>
        /// Set ion types based on the selected activation method.
        /// </summary>
        /// <param name="selectedActivationMethod">The selected activation method.</param>
        public void SetActivationMethod(ActivationMethod selectedActivationMethod)
        {
            if (!IcParameters.Instance.AutomaticallySelectIonTypes)
            {
                return;
            }

            var selectedBaseIonTypes = selectedActivationMethod == ActivationMethod.ETD
                                           ? IcParameters.Instance.EtdIonTypes
                                           : IcParameters.Instance.CidHcdIonTypes;

            foreach (var baseIonType in BaseIonTypes)
            {
                baseIonType.IsSelected = selectedBaseIonTypes.Contains(baseIonType.BaseIonType);
            }
        }

        /// <summary>
        /// Gets selected ion types based on the selected BaseIonTypes and NeutralLosses.
        /// </summary>
        /// <returns>Array of IonTypes.</returns>
        private IonType[] GetIonTypes()
        {
            var charge = Math.Min(fragmentationSequence.Charge - 1, 100);
            charge = Math.Max(charge, 1);

            return IonUtils.GetIonTypes(
                IcParameters.Instance.IonTypeFactory,
                BaseIonTypes.Where(bit => bit.IsSelected).Select(bit => bit.BaseIonType).ToList(),
                NeutralLosses.Where(nl => nl.IsSelected).Select(nl => nl.NeutralLoss).ToList(),
                1,
                charge).ToArray();
        }

        Task<LabeledIonViewModel[]> IFragmentationSequenceViewModel.GetLabeledIonViewModels()
        {
            return GetLabeledIonViewModels();
        }
    }
}
