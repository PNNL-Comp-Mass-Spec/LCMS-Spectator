namespace LcmsSpectator.ViewModels.SequenceViewer
{
    using System;
    using System.Linq;

    using System.Reactive.Linq;
    using System.Windows.Media;

    using InformedProteomics.Backend.Data.Spectrometry;

    using LcmsSpectator.PlotModels.ColorDicionaries;
    using LcmsSpectator.ViewModels.Data;

    using ReactiveUI;

    public class SequenceViewerViewModel : ReactiveObject
    {
        /// <summary>
        /// The currently selected MS/MS spectrum.
        /// </summary>
        private ProductSpectrum selectedSpectrum;

        /// <summary>
        /// The fragmentation sequence model that this view model formats for displasy.
        /// </summary>
        private FragmentationSequenceViewModel fragmentationSequence;

        /// <summary>
        /// The dictionary that determines how to color fragment ions.
        /// </summary>
        private IonColorDictionary ionColorDictionary;

        /// <summary>
        /// Initializes new instance of the <see cref="SequenceViewerViewModel" /> class.
        /// </summary>
        public SequenceViewerViewModel()
        {
            this.SequenceFragments = new ReactiveList<FragmentViewModel>();
            this.WhenAnyValue(
                              x => x.SelectedSpectrum,
                              x => x.FragmentationSequence,
                              x => x.IonColorDictionary)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .Subscribe(_ => this.ParseFragmentationSequence());
        }

        /// <summary>
        /// Gets or sets the currently selected MS/MS spectrum.
        /// </summary>
        public ProductSpectrum SelectedSpectrum
        {
            get { return this.selectedSpectrum; }
            set { this.RaiseAndSetIfChanged(ref this.selectedSpectrum, value); }
        }

        /// <summary>
        /// Gets or sets the fragmentation sequence model that this view model formats for displasy.
        /// </summary>
        public FragmentationSequenceViewModel FragmentationSequence
        {
            get { return this.fragmentationSequence; }
            set { this.RaiseAndSetIfChanged(ref this.fragmentationSequence, value); }
        }

        /// <summary>
        /// Gets or sets the dictionary that determines how to color fragment ions.
        /// </summary>
        public IonColorDictionary IonColorDictionary
        {
            get { return this.ionColorDictionary; }
            set { this.RaiseAndSetIfChanged(ref this.ionColorDictionary, value); }
        }

        /// <summary>
        /// Gets the list of fragments formatted to be displayed.
        /// </summary>
        public ReactiveList<FragmentViewModel> SequenceFragments { get; private set; }

        /// <summary>
        /// Parse the fragment sequence into the format required for display.
        /// </summary>
        private void ParseFragmentationSequence()
        {
            if (this.SequenceFragments != null && this.SequenceFragments.Count > 0)
            {
                this.SequenceFragments.Clear();
            }

            if (this.FragmentationSequence == null)
            {   // Invalid fragmentation sequence: Just show nothing.
                this.SequenceFragments.Clear();
                return;
            }

            if (this.SelectedSpectrum == null)
            {   // Invalid spectrum: clear all ions while retaining the sequence displayed.
                this.ClearSequenceFragments();
            }

            var labeledIonViewModels = this.FragmentationSequence.LabeledIonViewModels;
            var sequence = this.FragmentationSequence.FragmentationSequence.Sequence;
            var sequenceFragments = new FragmentViewModel[sequence.Count]; // sorted by sequence index
            for (int i = 0; i < sequence.Count; i++)
            {
                sequenceFragments[i] = new FragmentViewModel { Residue = sequence[i].Residue };
            }

            foreach (var labeledIonViewModel in labeledIonViewModels)
            {
                var index = labeledIonViewModel.IonType.IsPrefixIon
                                ? labeledIonViewModel.Index
                                : sequence.Count - labeledIonViewModel.Index;

                var peakDataPoints = labeledIonViewModel.GetPeaks(this.SelectedSpectrum, false);
                if (peakDataPoints.Count == 0)
                {   // Not observed, nothing to add.
                    continue;
                }

                // Add fragment prefix or suffix ion to the sequence.
                var sequenceFragment = sequenceFragments[index];
                var fragmentIon = labeledIonViewModel.IonType.IsPrefixIon
                                      ? sequenceFragment.PrefixIon.LabeledIonViewModel
                                      : sequenceFragment.SuffixIon.LabeledIonViewModel;
                var ionToAdd = labeledIonViewModel;
                if (fragmentIon != null)
                {   // This a fragment ion has already been marked for this cleavage, select the one that is higher intensity.
                    var newPeakDataPoints = fragmentIon.GetPeaks(this.SelectedSpectrum, false);
                    var currentMaxAbundance = peakDataPoints.Max(pd => pd.Y);
                    var newMaxAbundance = newPeakDataPoints.Count == 0 ? 0 : newPeakDataPoints.Max(pd => pd.Y);
                    ionToAdd = newMaxAbundance > currentMaxAbundance ? fragmentIon : labeledIonViewModel;
                }

                // Determine the color of the ion.
                var color = new Color();
                if (this.IonColorDictionary != null)
                {
                    var oxyColor = this.IonColorDictionary.GetColor(ionToAdd.IonType.BaseIonType, 1);
                    color = new Color { R = oxyColor.R, G = oxyColor.G, B = oxyColor.B };
                }

                // Finally, add the ion.
                if (labeledIonViewModel.IonType.IsPrefixIon)
                {
                    sequenceFragment.PrefixIon = new FragmentIonViewModel
                    {
                        LabeledIonViewModel = ionToAdd,
                        Color = color
                    };
                }
                else
                {
                    sequenceFragment.SuffixIon = new FragmentIonViewModel
                    {
                        LabeledIonViewModel = ionToAdd,
                        Color = color
                    };
                }
            }

            foreach (var sequenceFragment in sequenceFragments)
            {
                this.SequenceFragments.Add(sequenceFragment);
            }

            //this.SequenceFragments.AddRange(sequenceFragments);
        }

        /// <summary>
        /// Clears all ion data related to sequence.
        /// </summary>
        private void ClearSequenceFragments()
        {
            foreach (var sequenceFragment in this.SequenceFragments)
            {
                sequenceFragment.PrefixIon = null;
                sequenceFragment.SuffixIon = null;
            }
        }
    }
}
