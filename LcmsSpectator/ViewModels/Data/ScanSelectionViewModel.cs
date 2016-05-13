namespace LcmsSpectator.ViewModels.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;

    using ReactiveUI;

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
                throw new ArgumentOutOfRangeException("msLevel", msLevel, @"MSLevel must be greater than 0.");
            }

            this.MsLevel = msLevel;
            this.possibleScanNumbers = new HashSet<int>(possibleScanNumbers);
            this.ScanNumbers = new ReactiveList<int> { ChangeTrackingEnabled = true };

            this.AddScanRangeCommand = ReactiveCommand.Create();
            this.AddScanRangeCommand.Subscribe(_ => this.InsertScans());

            this.RemoveSelectedScanCommand = ReactiveCommand.Create();
            this.RemoveSelectedScanCommand
                .Where(_ => this.ScanNumbers.Contains(this.SelectedScanNumber))
                .Subscribe(_ => this.ScanNumbers.Remove(this.SelectedScanNumber));

            this.ClearScansCommand = ReactiveCommand.Create();
            this.ClearScansCommand.Subscribe(_ => this.ScanNumbers.Clear());

            this.AbsoluteMaxScanNumber = this.possibleScanNumbers.Max();

            var msLevelStr = this.MsLevel == 1 ? "MS1" : "MS/MS";
            this.ScanRangeDescription = string.Format("Select {0} range", msLevelStr);

            // When UseScanRange changes, toggle UseScanOffset.
            this.WhenAnyValue(x => x.UseScanRange).Subscribe(value => { this.UseScanOffset = !value; });

            // When UseScanOffset changes, toggle UseScanRange
            this.WhenAnyValue(x => x.UseScanOffset).Subscribe(value => { this.UseScanRange = !value; });
        }

        /// <summary>
        /// Initializes new instance of the <see cref="ScanSelectionViewModel" /> class.
        /// Default constructor for design time use.
        /// </summary>
        public ScanSelectionViewModel()
        {
            this.MsLevel = 1;
            this.possibleScanNumbers = new HashSet<int>();
            this.ScanNumbers = new ReactiveList<int>();
            this.ScanRangeDescription = "Select MS1 range.";
        }

        /// <summary>
        /// Gets a command that selects scan numbers from the scan number range
        /// and adds them to the <see cref="ScanNumbers" /> list.
        /// </summary>
        public ReactiveCommand<object> AddScanRangeCommand { get; private set; }

        /// <summary>
        /// Gets a command that removes the <see cref="SelectedScanNumber" /> from the
        /// <see cref="ScanNumbers" /> list.
        /// </summary>
        public ReactiveCommand<object> RemoveSelectedScanCommand { get; private set; }

        /// <summary>
        /// Gets a command that removes all of the scan numbers from the
        /// <see cref="ScanNumbers" /> list.
        /// </summary>
        public ReactiveCommand<object> ClearScansCommand { get; private set; }

        /// <summary>
        /// Gets or sets the MS level of scan numbers to select from.
        /// </summary>
        public int MsLevel
        {
            get { return this.msLevel; }
            private set { this.RaiseAndSetIfChanged(ref this.msLevel, value); }
        }

        /// <summary>
        /// Gets or sets the minimum scan number of the range to select scan numbers from.
        /// </summary>
        public int MinScanNumber
        {
            get { return this.minScanNumber; }
            set { this.RaiseAndSetIfChanged(ref this.minScanNumber, value); }
        }

        /// <summary>
        /// Gets or sets the maximum scan number of the range to select scan numbers from.
        /// </summary>
        public int MaxScanNumber
        {
            get { return this.maxScanNumber; }
            set { this.RaiseAndSetIfChanged(ref this.maxScanNumber, value); }
        }

        /// <summary>
        /// Gets the highest possible scan number that can be selected.
        /// </summary>
        public int AbsoluteMaxScanNumber
        {
            get { return this.absoluteMaxScanNumber; }
            private set { this.RaiseAndSetIfChanged(ref this. absoluteMaxScanNumber, value); }
        }

        /// <summary>
        /// Gets or sets the negative scan offset: the number of <see cref="MsLevel" /> scans
        /// below the base scan.
        /// </summary>
        public int NegativeScanOffset
        {
            get { return this.negativeScanOffset; }
            set { this.RaiseAndSetIfChanged(ref this.negativeScanOffset, value); }
        }

        /// <summary>
        /// Gets or sets the positive scan offset: the number of <see cref="MsLevel" /> scans
        /// above the base scan.
        /// </summary>
        public int PositiveScanOffset
        {
            get { return this.positiveScanOffset; }
            set { this.RaiseAndSetIfChanged(ref this.positiveScanOffset, value); }
        }

        /// <summary>
        /// Gets or sets the base scan for determining a scan range from two offsets.
        /// </summary>
        public int BaseScan
        {
            get { return this.baseScan; }
            set { this.RaiseAndSetIfChanged(ref this.baseScan, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scan range should be used for
        /// inserting scans into the <see cref="ScanNumbers" /> list.
        /// </summary>
        public bool UseScanRange
        {
            get { return this.useScanRange; }
            set { this.RaiseAndSetIfChanged(ref this.useScanRange, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scan offset should be used for
        /// inserting scans into the <see cref="ScanNumbers" /> list.
        /// </summary>
        public bool UseScanOffset
        {
            get { return this.useScanOffset; }
            set { this.RaiseAndSetIfChanged(ref this.useScanOffset, value); }
        }

        /// <summary>
        /// Gets or sets the scan number selected from the <see cref="ScanNumbers" /> list.
        /// </summary>
        public int SelectedScanNumber
        {
            get { return this.selectedScanNumber; }
            set { this.RaiseAndSetIfChanged(ref this.selectedScanNumber, value); }
        }

        /// <summary>
        /// Gets the selected scan numbers.
        /// </summary>
        public ReactiveList<int> ScanNumbers { get; private set; }

        /// <summary>
        /// Gets or sets the text describing the type of scans to select.
        /// </summary>
        public string ScanRangeDescription
        {
            get { return this.scanRangeDescription; }
            set { this.RaiseAndSetIfChanged(ref this.scanRangeDescription, value); }
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
            this.BaseScan = baseScanNum;
            this.NegativeScanOffset = minus;
            this.PositiveScanOffset = plus;
            this.InsertScanRange();
        }

        /// <summary>
        /// Sets the minimum and maximum scan and automatically inserts them into the
        /// <see cref="ScanNumbers" /> list.
        /// </summary>
        /// <param name="minScan">The minimum scan number in the range.</param>
        /// <param name="maxScan">The maximum scan number in the range.</param>
        public void SetScanRange(int minScan, int maxScan)
        {
            this.MinScanNumber = minScan;
            this.MaxScanNumber = maxScan;
            this.InsertScanRange();
        }

        /// <summary>
        /// Gets the spectrum selected in the <see cref="ScanNumbers" /> list if there is a single  
        /// scan, or sums multiple scans if there is more than one.
        /// </summary>
        /// <returns></returns>
        public Spectrum GetSelectedSpectrum(LcMsRun lcms)
        {
            Spectrum spectrum = null;
            if (this.ScanNumbers.Count == 1)
            {   // Single spectrum selected.
                spectrum = lcms.GetSpectrum(this.ScanNumbers[0]);
            }
            else if (this.ScanNumbers.Count > 1)
            {   // Multiple spectra selected. Need to sum.
                var summedScanNumbers = this.ScanNumbers.Sum();
                var summedSpectrum = lcms.GetSummedSpectrum(this.ScanNumbers);
                spectrum = this.MsLevel == 1
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
            return this.ScanNumbers.Contains(scanNum) || this.ScanNumbers.Sum() == scanNum;
        }

        /// <summary>
        /// Gets an observable that determines whether or not the Success command is executable.
        /// </summary>
        protected override IObservable<bool> CanSucceed
        {
            get { return this.WhenAnyValue(x => x.ScanNumbers.Count).Select(_ => this.Validate()); }
        }

        /// <summary>
        /// Function that checks whether or not valid scans have been selected.
        /// </summary>
        /// <returns>A value indicating whether or not valid scans have been selected.</returns>
        protected override bool Validate()
        {
            return this.ScanNumbers.Count > 0;
        }

        /// <summary>
        /// Insert the scans given by the selected scan range into the <see cref="ScanNumbers" /> list.
        /// </summary>
        private void InsertScans()
        {
            if (this.UseScanOffset)
            {
                this.InsertScanOffset();
            }
            else
            {
                this.InsertScanRange();
            }
        }

        /// <summary>
        /// Inserts scan numbers from the scan range selected by the user into
        /// the <see cref="ScanNumbers" /> list.
        /// </summary>
        private void InsertScanRange()
        {
            for (int scan = this.MinScanNumber; scan <= this.MaxScanNumber; scan++)
            {
                if (this.possibleScanNumbers.Contains(scan) && !this.ScanNumbers.Contains(scan))
                {
                    this.ScanNumbers.Add(scan);
                }
            }

            this.ScanNumbers.Sort();
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
            int negativeScansSeen = 0;
            for (int scan = this.BaseScan; scan >= 0; scan--)
            {
                if (!this.possibleScanNumbers.Contains(scan))
                {
                    continue;
                }

                if (negativeScansSeen++ > this.NegativeScanOffset)
                {
                    break;
                }

                if (!this.ScanNumbers.Contains(scan))
                {
                    this.ScanNumbers.Add(scan);
                }
            }

            // Move forward
            int positiveScansSeen = 0;
            for (int scan = this.BaseScan; scan <= this.AbsoluteMaxScanNumber; scan++)
            {
                if (!this.possibleScanNumbers.Contains(scan))
                {
                    continue;
                }

                if (positiveScansSeen++ > this.PositiveScanOffset)
                {
                    break;
                }

                if (!this.ScanNumbers.Contains(scan))
                {
                    this.ScanNumbers.Add(scan);
                }
            }

            this.ScanNumbers.Sort();
        }
    }
}
