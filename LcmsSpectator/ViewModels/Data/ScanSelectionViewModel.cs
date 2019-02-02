using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Data
{
    /// <summary>
    /// This is a view model for selecting a range of scan numbers.
    /// </summary>
    public class ScanSelectionViewModel : WindowViewModel
    {
        /// <summary>
        /// The MS level of scan numbers to select from.
        /// </summary>
        private int msLevel;

        /// <summary>
        /// All of scan numbers at the provided MS level.
        /// </summary>
        private readonly HashSet<int> possibleScanNumbers;

        /// <summary>
        /// The minimum scan number of the range to select scan numbers from.
        /// </summary>
        private int minScanNumber;

        /// <summary>
        /// The maximum scan number of the range to select scan numbers from.
        /// </summary>
        private int maxScanNumber;

        /// <summary>
        /// The highest possible scan number that can be selected.
        /// </summary>
        private int absoluteMaxScanNumber;

        /// <summary>
        /// The base scan for determining a scan range from two offsets.
        /// </summary>
        private int baseScan;

        /// <summary>
        /// The negative scan offset: the number of <see cref="MsLevel" /> scans
        /// below the base scan.
        /// </summary>
        private int negativeScanOffset;

        /// <summary>
        /// The positive scan offset: the number of <see cref="MsLevel" /> scans
        /// above the base scan.
        /// </summary>
        private int positiveScanOffset;

        /// <summary>
        /// A value indicating whether the scan range should be used for
        /// inserting scans into the <see cref="ScanNumbers" /> list.
        /// </summary>
        private bool useScanRange;

        /// <summary>
        /// A value indicating whether the scan offset should be used for
        /// inserting scans into the <see cref="ScanNumbers" /> list.
        /// </summary>
        private bool useScanOffset;

        /// <summary>
        /// The scan number selected from the <see cref="ScanNumbers" /> list.
        /// </summary>
        private int selectedScanNumber;

        /// <summary>
        /// The text describing the type of scans to select.
        /// </summary>
        private string scanRangeDescription;

        /// <summary>
        /// Initializes new instance of the <see cref="ScanSelectionViewModel" /> class.
        /// </summary>
        /// <param name="msLevel">The MS level of scan numbers to select from.</param>
        /// <param name="possibleScanNumbers">All of scan numbers at the provided MS level.</param>
        public ScanSelectionViewModel(int msLevel, IEnumerable<int> possibleScanNumbers)
        {
            if (msLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(msLevel), msLevel, @"MSLevel must be greater than 0.");
            }

            MsLevel = msLevel;
            this.possibleScanNumbers = new HashSet<int>(possibleScanNumbers);
            ScanNumbers = new ReactiveList<int> { ChangeTrackingEnabled = true };

            AddScanRangeCommand = ReactiveCommand.Create(InsertScans);

            RemoveSelectedScanCommand = ReactiveCommand.Create(() => ScanNumbers.Remove(SelectedScanNumber), this.WhenAnyValue(x => x.SelectedScanNumber, x => x.ScanNumbers.Count).Select(x => ScanNumbers.Contains(x.Item1)));

            ClearScansCommand = ReactiveCommand.Create(() => ScanNumbers.Clear());

            AbsoluteMaxScanNumber = this.possibleScanNumbers.Max();

            var msLevelStr = MsLevel == 1 ? "MS1" : "MS/MS";
            ScanRangeDescription = string.Format("Select {0} range", msLevelStr);

            // When UseScanRange changes, toggle UseScanOffset.
            this.WhenAnyValue(x => x.UseScanRange).Subscribe(value => { UseScanOffset = !value; });

            // When UseScanOffset changes, toggle UseScanRange
            this.WhenAnyValue(x => x.UseScanOffset).Subscribe(value => { UseScanRange = !value; });
        }

        /// <summary>
        /// Initializes new instance of the <see cref="ScanSelectionViewModel" /> class.
        /// Default constructor for design time use.
        /// </summary>
        public ScanSelectionViewModel()
        {
            MsLevel = 1;
            possibleScanNumbers = new HashSet<int>();
            ScanNumbers = new ReactiveList<int>();
            ScanRangeDescription = "Select MS1 range.";
        }

        /// <summary>
        /// Gets a command that selects scan numbers from the scan number range
        /// and adds them to the <see cref="ScanNumbers" /> list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddScanRangeCommand { get; }

        /// <summary>
        /// Gets a command that removes the <see cref="SelectedScanNumber" /> from the
        /// <see cref="ScanNumbers" /> list.
        /// </summary>
        public ReactiveCommand<Unit, bool> RemoveSelectedScanCommand { get; }

        /// <summary>
        /// Gets a command that removes all of the scan numbers from the
        /// <see cref="ScanNumbers" /> list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ClearScansCommand { get; }

        /// <summary>
        /// Gets or sets the MS level of scan numbers to select from.
        /// </summary>
        public int MsLevel
        {
            get => msLevel;
            private set => this.RaiseAndSetIfChanged(ref msLevel, value);
        }

        /// <summary>
        /// Gets or sets the minimum scan number of the range to select scan numbers from.
        /// </summary>
        public int MinScanNumber
        {
            get => minScanNumber;
            set => this.RaiseAndSetIfChanged(ref minScanNumber, value);
        }

        /// <summary>
        /// Gets or sets the maximum scan number of the range to select scan numbers from.
        /// </summary>
        public int MaxScanNumber
        {
            get => maxScanNumber;
            set => this.RaiseAndSetIfChanged(ref maxScanNumber, value);
        }

        /// <summary>
        /// Gets the highest possible scan number that can be selected.
        /// </summary>
        public int AbsoluteMaxScanNumber
        {
            get => absoluteMaxScanNumber;
            private set => this.RaiseAndSetIfChanged(ref absoluteMaxScanNumber, value);
        }

        /// <summary>
        /// Gets or sets the negative scan offset: the number of <see cref="MsLevel" /> scans
        /// below the base scan.
        /// </summary>
        public int NegativeScanOffset
        {
            get => negativeScanOffset;
            set => this.RaiseAndSetIfChanged(ref negativeScanOffset, value);
        }

        /// <summary>
        /// Gets or sets the positive scan offset: the number of <see cref="MsLevel" /> scans
        /// above the base scan.
        /// </summary>
        public int PositiveScanOffset
        {
            get => positiveScanOffset;
            set => this.RaiseAndSetIfChanged(ref positiveScanOffset, value);
        }

        /// <summary>
        /// Gets or sets the base scan for determining a scan range from two offsets.
        /// </summary>
        public int BaseScan
        {
            get => baseScan;
            set => this.RaiseAndSetIfChanged(ref baseScan, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scan range should be used for
        /// inserting scans into the <see cref="ScanNumbers" /> list.
        /// </summary>
        public bool UseScanRange
        {
            get => useScanRange;
            set => this.RaiseAndSetIfChanged(ref useScanRange, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scan offset should be used for
        /// inserting scans into the <see cref="ScanNumbers" /> list.
        /// </summary>
        public bool UseScanOffset
        {
            get => useScanOffset;
            set => this.RaiseAndSetIfChanged(ref useScanOffset, value);
        }

        /// <summary>
        /// Gets or sets the scan number selected from the <see cref="ScanNumbers" /> list.
        /// </summary>
        public int SelectedScanNumber
        {
            get => selectedScanNumber;
            set => this.RaiseAndSetIfChanged(ref selectedScanNumber, value);
        }

        /// <summary>
        /// Gets the selected scan numbers.
        /// </summary>
        public ReactiveList<int> ScanNumbers { get; }

        /// <summary>
        /// Gets or sets the text describing the type of scans to select.
        /// </summary>
        public string ScanRangeDescription
        {
            get => scanRangeDescription;
            set => this.RaiseAndSetIfChanged(ref scanRangeDescription, value);
        }

        /// <summary>
        /// Set the scan range based on a base scan, and subtracting a
        /// certain number of scans and adding a certain number of scans.
        /// </summary>
        /// <param name="baseScanNum">The base scan to subtract or add scans to.</param>
        /// <param name="minus">The minimum of the scan range defined relative to the base scan.</param>
        /// <param name="plus">The maximum of the scan range defined relative to the base scan.</param>
        public void SetScanRange(int baseScanNum, int minus, int plus)
        {
            BaseScan = baseScanNum;
            NegativeScanOffset = minus;
            PositiveScanOffset = plus;
            InsertScanRange();
        }

        /// <summary>
        /// Sets the minimum and maximum scan and automatically inserts them into the
        /// <see cref="ScanNumbers" /> list.
        /// </summary>
        /// <param name="minScan">The minimum scan number in the range.</param>
        /// <param name="maxScan">The maximum scan number in the range.</param>
        public void SetScanRange(int minScan, int maxScan)
        {
            MinScanNumber = minScan;
            MaxScanNumber = maxScan;
            InsertScanRange();
        }

        /// <summary>
        /// Gets the spectrum selected in the <see cref="ScanNumbers" /> list if there is a single
        /// scan, or sums multiple scans if there is more than one.
        /// </summary>
        /// <returns></returns>
        public Spectrum GetSelectedSpectrum(ILcMsRun lcms)
        {
            Spectrum spectrum = null;
            if (ScanNumbers.Count == 1)
            {   // Single spectrum selected.
                spectrum = lcms.GetSpectrum(ScanNumbers[0]);
            }
            else if (ScanNumbers.Count > 1)
            {   // Multiple spectra selected. Need to sum.
                var summedScanNumbers = ScanNumbers.Sum();
                var summedSpectrum = lcms.GetSummedSpectrum(ScanNumbers);
                spectrum = MsLevel == 1
                               ? new Spectrum(summedSpectrum.Peaks, 0)
                               : new ProductSpectrum(summedSpectrum.Peaks, summedScanNumbers);
            }

            return spectrum;
        }

        /// <summary>
        /// Determines whether a scan number is contained within the range specified,
        /// or if the scan came from summing multiple spectra within the scan range specified.
        /// </summary>
        /// <param name="scanNum">The scan number to find.</param>
        /// <returns>A value indicating whether the scan range contains the provided scan number.</returns>
        public bool Contains(int scanNum)
        {
            return ScanNumbers.Contains(scanNum) || ScanNumbers.Sum() == scanNum;
        }

        /// <summary>
        /// Gets an observable that determines whether or not the Success command is executable.
        /// </summary>
        protected override IObservable<bool> CanSucceed
        {
            get { return this.WhenAnyValue(x => x.ScanNumbers.Count).Select(_ => Validate()); }
        }

        /// <summary>
        /// Function that checks whether or not valid scans have been selected.
        /// </summary>
        /// <returns>A value indicating whether or not valid scans have been selected.</returns>
        protected override bool Validate()
        {
            return ScanNumbers.Count > 0;
        }

        /// <summary>
        /// Insert the scans given by the selected scan range into the <see cref="ScanNumbers" /> list.
        /// </summary>
        private void InsertScans()
        {
            if (UseScanOffset)
            {
                InsertScanOffset();
            }
            else
            {
                InsertScanRange();
            }
        }

        /// <summary>
        /// Inserts scan numbers from the scan range selected by the user into
        /// the <see cref="ScanNumbers" /> list.
        /// </summary>
        private void InsertScanRange()
        {
            for (var scan = MinScanNumber; scan <= MaxScanNumber; scan++)
            {
                if (possibleScanNumbers.Contains(scan) && !ScanNumbers.Contains(scan))
                {
                    ScanNumbers.Add(scan);
                }
            }

            ScanNumbers.Sort();
        }

        /// <summary>
        /// Inserts scan numbers given by the range
        /// <see cref="BaseScan" /> - <see cref="NegativeScanOffset"/>
        /// to <see cref="BaseScan" /> + <see cref="PositiveScanOffset" />
        /// selected by the user into the <see cref="ScanNumbers" /> list.
        /// </summary>
        private void InsertScanOffset()
        {
            // Move backward
            var negativeScansSeen = 0;
            for (var scan = BaseScan; scan >= 0; scan--)
            {
                if (!possibleScanNumbers.Contains(scan))
                {
                    continue;
                }

                if (negativeScansSeen++ > NegativeScanOffset)
                {
                    break;
                }

                if (!ScanNumbers.Contains(scan))
                {
                    ScanNumbers.Add(scan);
                }
            }

            // Move forward
            var positiveScansSeen = 0;
            for (var scan = BaseScan; scan <= AbsoluteMaxScanNumber; scan++)
            {
                if (!possibleScanNumbers.Contains(scan))
                {
                    continue;
                }

                if (positiveScansSeen++ > PositiveScanOffset)
                {
                    break;
                }

                if (!ScanNumbers.Contains(scan))
                {
                    ScanNumbers.Add(scan);
                }
            }

            ScanNumbers.Sort();
        }
    }
}
