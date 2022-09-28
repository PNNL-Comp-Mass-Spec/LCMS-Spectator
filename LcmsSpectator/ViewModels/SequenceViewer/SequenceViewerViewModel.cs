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
using LcmsSpectator.PlotModels.ColorDictionaries;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Data;
using OxyPlot.Wpf;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.SequenceViewer
{
    public class SequenceViewerViewModel : ReactiveObject
    {
        /// <summary>
        /// Service for opening LCMSSpectator dialogs.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// The currently selected MS/MS spectrum.
        /// </summary>
        private ProductSpectrum selectedSpectrum;

        /// <summary>
        /// Number of data points (peaks) in selectedSpectrum
        /// </summary>
        private int selectedSpectrumPeakCount;

        /// <summary>
        /// The fragmentation sequence model that this view model formats for display.
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
        public SequenceViewerViewModel() : this(null)
        {
            // Not using a default parameter to make WPF design-time view happy
        }

        /// <summary>
        /// Initializes new instance of the <see cref="SequenceViewerViewModel" /> class.
        /// </summary>
        /// <param name="dialogService">Service for opening LCMSSpectator dialogs.</param>
        public SequenceViewerViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService ?? new MainDialogService();
            SequenceFragments = new ReactiveList<FragmentViewModel> { ChangeTrackingEnabled = true };

            IonColorDictionary = new IonColorDictionary(2);

            // Update the sequence displayed when the fragmentation sequence or spectrum changes.
            this.WhenAnyValue(
                              x => x.SelectedSpectrum,
                              x => x.FragmentationSequence,
                              x => x.FragmentationSequence.LabeledIonViewModels)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => ParseFragmentationSequence());

            SequenceFragments.ItemChanged.Where(x => x.PropertyName == "AminoAcid")
                .Subscribe(x => UpdateSequence(x.Sender.AminoAcid, x.Sender.Index));
        }

        /// <summary>
        /// Gets or sets the currently selected MS/MS spectrum.
        /// </summary>
        public ProductSpectrum SelectedSpectrum
        {
            get => selectedSpectrum;
            set
            {
                this.RaiseAndSetIfChanged(ref selectedSpectrum, value);
                if (value?.Peaks == null)
                    SelectedSpectrumPeakCount = 0;
                else
                    SelectedSpectrumPeakCount = value.Peaks.Length;
            }
        }

        /// <summary>
        /// Gets or sets the number of data points in SelectedSpectrum
        /// </summary>
        public int SelectedSpectrumPeakCount {
            get => selectedSpectrumPeakCount;
            set => this.RaiseAndSetIfChanged(ref selectedSpectrumPeakCount, value);
        }

        /// <summary>
        /// Gets or sets the fragmentation sequence model that this view model formats for display.
        /// </summary>
        public FragmentationSequenceViewModel FragmentationSequence
        {
            get => fragmentationSequence;
            set => this.RaiseAndSetIfChanged(ref fragmentationSequence, value);
        }

        /// <summary>
        /// Gets or sets the dictionary that determines how to color fragment ions.
        /// </summary>
        public IonColorDictionary IonColorDictionary
        {
            get => ionColorDictionary;
            private set => this.RaiseAndSetIfChanged(ref ionColorDictionary, value);
        }

        /// <summary>
        /// Gets the list of fragments formatted to be displayed.
        /// </summary>
        public ReactiveList<FragmentViewModel> SequenceFragments { get; }

        /// <summary>
        /// Gets the percentage of fragments found in the spectrum.
        /// </summary>
        public double SequenceCoverage
        {
            get => sequenceCoverage;
            private set => this.RaiseAndSetIfChanged(ref sequenceCoverage, value);
        }

        /// <summary>
        /// Parse the fragment sequence into the format required for display.
        /// </summary>
        private void ParseFragmentationSequence()
        {
            if (SequenceFragments.Count > 0)
            {
                SequenceFragments.Clear();
            }

            if (FragmentationSequence == null)
            {
                // Invalid fragmentation sequence: Just show nothing.
                SequenceFragments.Clear();
                return;
            }

            if (SelectedSpectrum == null)
            {
                // Invalid spectrum: clear all ions while retaining the sequence displayed.
                ClearSequenceFragments();
            }

            var labeledIonViewModels = FragmentationSequence.LabeledIonViewModels.Where(l => l.IsFragmentIon);
            var sequence = FragmentationSequence.FragmentationSequence.Sequence;
            var sequenceFragments = new FragmentViewModel[sequence.Count]; // sorted by sequence index
            for (var i = 0; i < sequence.Count; i++)
            {
                sequenceFragments[i] = new FragmentViewModel(sequence[i], i, dialogService);
            }

            var allPeakDataPoints = new List<PeakDataPoint>();

            foreach (var labeledIonViewModel in labeledIonViewModels)
            {
                var index = labeledIonViewModel.IonType.IsPrefixIon
                                ? labeledIonViewModel.Index - 1
                                : sequence.Count - labeledIonViewModel.Index;

                var peakDataPoints = labeledIonViewModel.GetPeaks(SelectedSpectrum, false);
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
                    var newPeakDataPoints = label.GetPeaks(SelectedSpectrum, false);
                    var currentMaxAbundance = peakDataPoints.Max(pd => pd.Y);
                    var newMaxAbundance = newPeakDataPoints.Count == 0 ? 0 : newPeakDataPoints.Max(pd => pd.Y);
                    ionToAdd = newMaxAbundance > currentMaxAbundance ? label : labeledIonViewModel;
                }

                // Determine the color of the ion.
                Brush brush = null;
                if (IonColorDictionary != null)
                {
                    var oxyColor = IonColorDictionary.GetColor(ionToAdd.IonType.BaseIonType, 1);
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

            SequenceFragments.AddRange(sequenceFragments);

            SequenceCoverage = Math.Round(IonUtils.CalculateSequenceCoverage(
                allPeakDataPoints,
                FragmentationSequence.FragmentationSequence.Sequence.Count), 3);
        }

        /// <summary>
        /// Clears all ion data related to sequence.
        /// </summary>
        private void ClearSequenceFragments()
        {
            foreach (var sequenceFragment in SequenceFragments)
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
            var fragSeq = FragmentationSequence.FragmentationSequence;
            var newSequence = new Sequence(FragmentationSequence.FragmentationSequence.Sequence) {
                [index] = aminoAcid
            };

            FragmentationSequence.FragmentationSequence =
                new FragmentationSequence(newSequence, fragSeq.Charge, fragSeq.LcMsRun, fragSeq.ActivationMethod);
        }
    }
}
