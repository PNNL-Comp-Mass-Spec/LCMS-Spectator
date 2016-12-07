namespace LcmsSpectator.ViewModels.SequenceViewer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using System.Reactive.Linq;
    using System.Windows.Media;

    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.PlotModels;
    using LcmsSpectator.PlotModels.ColorDicionaries;
    using LcmsSpectator.Utils;
    using LcmsSpectator.ViewModels.Data;

    using OxyPlot.Wpf;

    using ReactiveUI;

    public class SequenceViewerViewModel : ReactiveObject
    {
        /// <summary>
        /// Service for opening LCMSSpectator dialogs.
        /// </summary>
        private IMainDialogService dialogService;

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
        /// The percentage of fragments found in the spectrum.
        /// </summary>
        private double sequenceCoverage;

        /// <summary>
        /// Initializes new instance of the <see cref="SequenceViewerViewModel" /> class.
        /// </summary>
        /// <param name="dialogService">Service for opening LCMSSpectator dialogs.</param>
        public SequenceViewerViewModel(IMainDialogService dialogService = null)
        {
            this.dialogService = dialogService ?? new MainDialogService();
            this.SequenceFragments = new ReactiveList<FragmentViewModel> { ChangeTrackingEnabled = true };

            this.IonColorDictionary = new IonColorDictionary(2);

            // Update the sequence displayed when the fragmentation sequence or spectrum changes.
            this.WhenAnyValue(
                              x => x.SelectedSpectrum,
                              x => x.FragmentationSequence,
                              x => x.FragmentationSequence.LabeledIonViewModels)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.ParseFragmentationSequence());

            this.SequenceFragments.ItemChanged.Where(x => x.PropertyName == "AminoAcid")
                .Subscribe(x => this.UpdateSequence(x.Sender.AminoAcid, x.Sender.Index));

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
            private set { this.RaiseAndSetIfChanged(ref this.ionColorDictionary, value); }
        }

        /// <summary>
        /// Gets the list of fragments formatted to be displayed.
        /// </summary>
        public ReactiveList<FragmentViewModel> SequenceFragments { get; private set; }

        /// <summary>
        /// Gets the percentage of fragments found in the spectrum.
        /// </summary>
        public double SequenceCoverage
        {
            get { return this.sequenceCoverage; }
            private set { this.RaiseAndSetIfChanged(ref this.sequenceCoverage, value); }
        }

        /// <summary>
        /// Parse the fragment sequence into the format required for display.
        /// </summary>
        private void ParseFragmentationSequence()
        {
            if (this.SequenceFragments.Count > 0)
            {
                this.SequenceFragments.Clear();
            }

            if (this.FragmentationSequence == null)
            {
                // Invalid fragmentation sequence: Just show nothing.
                this.SequenceFragments.Clear();
                return;
            }

            if (this.SelectedSpectrum == null)
            {
                // Invalid spectrum: clear all ions while retaining the sequence displayed.
                this.ClearSequenceFragments();
            }

            var labeledIonViewModels = this.FragmentationSequence.LabeledIonViewModels.Where(l => l.IsFragmentIon);
            var sequence = this.FragmentationSequence.FragmentationSequence.Sequence;
            var sequenceFragments = new FragmentViewModel[sequence.Count]; // sorted by sequence index
            for (int i = 0; i < sequence.Count; i++)
            {
                sequenceFragments[i] = new FragmentViewModel(sequence[i], i, this.dialogService);
            }

            var allPeakDataPoints = new List<PeakDataPoint>();

            foreach (var labeledIonViewModel in labeledIonViewModels)
            {
                var index = labeledIonViewModel.IonType.IsPrefixIon
                                ? labeledIonViewModel.Index - 1
                                : sequence.Count - labeledIonViewModel.Index;

                var peakDataPoints = labeledIonViewModel.GetPeaks(this.SelectedSpectrum, false);
                if (peakDataPoints.Count == 1 && peakDataPoints[0].X.Equals(double.NaN))
                {
                    // Not observed, nothing to add.
                    continue;
                }

                allPeakDataPoints.Add(peakDataPoints[0]);

                var sequenceFragment = sequenceFragments[index];
                var fragmentIon = labeledIonViewModel.IonType.IsPrefixIon
                                      ? sequenceFragment.PrefixIon
                                      : sequenceFragment.SuffixIon;
                var ionToAdd = labeledIonViewModel;
                if (fragmentIon != null)
                    // Add fragment prefix or suffix ion to the sequence.
                {
                    // This a fragment ion has already been marked for this cleavage, select the one that is higher intensity.
                    var label = fragmentIon.LabeledIonViewModel;
                    var newPeakDataPoints = label.GetPeaks(this.SelectedSpectrum, false);
                    var currentMaxAbundance = peakDataPoints.Max(pd => pd.Y);
                    var newMaxAbundance = newPeakDataPoints.Count == 0 ? 0 : newPeakDataPoints.Max(pd => pd.Y);
                    ionToAdd = newMaxAbundance > currentMaxAbundance ? label : labeledIonViewModel;
                }

                // Determine the color of the ion.
                Brush brush = null;
                if (this.IonColorDictionary != null)
                {
                    var oxyColor = this.IonColorDictionary.GetColor(ionToAdd.IonType.BaseIonType, 1);
                    brush = oxyColor.ToBrush();
                }

                // Finally, add the ion.
                if (labeledIonViewModel.IonType.IsPrefixIon)
                {
                    sequenceFragment.PrefixIon = new FragmentIonViewModel
                                                     {
                                                         LabeledIonViewModel = ionToAdd,
                                                         Color = brush
                                                     };
                }
                else
                {
                    sequenceFragment.SuffixIon = new FragmentIonViewModel
                                                     {
                                                         LabeledIonViewModel = ionToAdd,
                                                         Color = brush
                                                     };
                }
            }

            foreach (var sequenceFragment in sequenceFragments)
            {
                this.SequenceFragments.Add(sequenceFragment);
            }

            this.SequenceCoverage = Math.Round(IonUtils.CalculateSequenceCoverage(
                allPeakDataPoints,
                this.FragmentationSequence.FragmentationSequence.Sequence.Count), 3);
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

        /// <summary>
        /// Update the sequence when a fragment amino acid is changed.
        /// </summary>
        private void UpdateSequence(AminoAcid aminoAcid, int index)
        {
            var fragSeq = this.FragmentationSequence.FragmentationSequence;
            var newSequence = new Sequence(this.FragmentationSequence.FragmentationSequence.Sequence);
            newSequence[index] = aminoAcid;
            this.FragmentationSequence.FragmentationSequence = 
                new FragmentationSequence(newSequence, fragSeq.Charge, fragSeq.LcMsRun, fragSeq.ActivationMethod);
        }
    }
}
