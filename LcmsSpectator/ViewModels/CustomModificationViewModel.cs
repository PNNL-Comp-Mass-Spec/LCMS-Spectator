// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomModificationViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a view model for editing the empirical formula or mass shift of a modification.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Globalization;

    using InformedProteomics.Backend.Data.Composition;
    using LcmsSpectator.DialogServices;
    using ReactiveUI;

    /// <summary>
    /// This class is a view model for editing the empirical formula or mass shift of a modification.
    /// </summary>
    public class CustomModificationViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// The name of the modification.
        /// </summary>
        private string modificationName;

        /// <summary>
        /// A value indicating whether the modification name is readonly
        /// </summary>
        private bool modificationNameReadOnly;

        /// <summary>
        /// Number of carbon molecules in the modification formula
        /// </summary>
        private int c;

        /// <summary>
        /// Number of hydrogen molecules in the modification formula
        /// </summary>
        private int h;

        /// <summary>
        /// Number of nitrogen molecules in the modification formula
        /// </summary>
        private int n;

        /// <summary>
        /// Number of oxygen molecules in the modification formula
        /// </summary>
        private int o;

        /// <summary>
        /// Number of sulfur molecules in the modification formula
        /// </summary>
        private int s;

        /// <summary>
        /// Number of phosphorous molecules in the modification formula
        /// </summary>
        private int p;

        /// <summary>
        /// The mass of the modification as a string.
        /// </summary>
        private string massStr;

        /// <summary>
        /// The mass of the modification.
        /// </summary>
        private double mass;

        /// <summary>
        /// A value indicating whether this modification is defined by an empirical formula.
        /// </summary>
        private bool fromFormulaChecked;

        /// <summary>
        /// A value indicating whether this modification is defined by a mass shift.
        /// </summary>
        private bool fromMassChecked;

        /// <summary>
        /// Initializes a new instance of the CustomModificationViewModel class when there is no known composition.
        /// </summary>
        /// <param name="modificationName">The name of the modification.</param>
        /// <param name="modificationNameReadOnly">Should the modification name be readonly?</param>
        /// <param name="dialogService">Dialog service for opening dialogs from a view model.</param>
        public CustomModificationViewModel(string modificationName, bool modificationNameReadOnly, IDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.ModificationName = modificationName;
            this.ModificationNameReadOnly = modificationNameReadOnly;
            this.Composition = new Composition(0, 0, 0, 0, 0);
            this.Status = false;

            this.FromFormulaChecked = true;
            this.FromMassChecked = false;

            var saveCommand = ReactiveCommand.Create();
            saveCommand.Subscribe(_ => this.SaveImplementation());
            this.SaveCommand = saveCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;

            this.WhenAnyValue(x => x.Mass).Subscribe(mass => this.MassStr = mass.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Initializes a new instance of the CustomModificationViewModel class when there is a known composition.
        /// </summary>
        /// <param name="modificationName">The name of the modification.</param>
        /// <param name="modificationNameReadOnly">Should the modification name be readonly?</param>
        /// <param name="composition">The known composition of the modification.</param>
        /// <param name="dialogService">Dialog service for opening dialogs from a view model.</param>
        public CustomModificationViewModel(string modificationName, bool modificationNameReadOnly, Composition composition, IDialogService dialogService)
            : this(modificationName, modificationNameReadOnly, dialogService)
        {
            this.Composition = composition;
        }

        /// <summary>
        /// Event that is triggered when save or cancel are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a value indicating whether a valid modification has been selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets a command creates a modification from the values selected and sets the status.
        /// </summary>
        public IReactiveCommand SaveCommand { get; private set; }
        
        /// <summary>
        /// Gets a command that sets status to false.
        /// </summary>
        public IReactiveCommand CancelCommand { get; private set; }

        /// <summary>
        /// Gets or sets the name of the modification.
        /// </summary>
        public string ModificationName
        {
            get { return this.modificationName; }
            set { this.RaiseAndSetIfChanged(ref this.modificationName, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the modification name is readonly
        /// </summary>
        public bool ModificationNameReadOnly
        {
            get { return this.modificationNameReadOnly; }
            set { this.RaiseAndSetIfChanged(ref this.modificationNameReadOnly, value); }
        }

        /// <summary>
        /// Gets or sets the number of carbon molecules in the modification formula
        /// </summary>
        public int C
        {
            get { return this.c; }
            set { this.RaiseAndSetIfChanged(ref this.c, value); }
        }

        /// <summary>
        /// Gets or sets the number of hydrogen molecules in the modification formula
        /// </summary>
        public int H
        {
            get { return this.h; }
            set { this.RaiseAndSetIfChanged(ref this.h, value); }
        }

        /// <summary>
        /// Gets or sets the number of nitrogen molecules in the modification formula
        /// </summary>
        public int N
        {
            get { return this.n; }
            set { this.RaiseAndSetIfChanged(ref this.n, value); }
        }

        /// <summary>
        /// Gets or sets the number of oxygen molecules in the modification formula
        /// </summary>
        public int O
        {
            get { return this.o; }
            set { this.RaiseAndSetIfChanged(ref this.o, value); }
        }

        /// <summary>
        /// Gets or sets number of sulfur molecules in the modification formula
        /// </summary>
        public int S
        {
            get { return this.s; }
            set { this.RaiseAndSetIfChanged(ref this.s, value); }
        }

        /// <summary>
        /// Gets or sets the number of phosphorous molecules in the modification formula
        /// </summary>
        public int P
        {
            get { return this.p; }
            set { this.RaiseAndSetIfChanged(ref this.p, value); }
        }

        /// <summary>
        /// Gets or sets the composition of the empirical formula for this modification.
        /// </summary>
        public Composition Composition
        {
            get
            {
                return new Composition(this.C, this.H, this.N, this.O, this.S);
            }

            set
            {
                this.C = value.C;
                this.H = value.H;
                this.N = value.N;
                this.O = value.O;
                this.S = value.S;
            }
        }

        /// <summary>
        /// Gets or sets the mass of the modification as a string.
        /// </summary>
        public string MassStr
        {
             get { return this.massStr; }
            set { this.RaiseAndSetIfChanged(ref this.massStr, value); }
        }

        /// <summary>
        /// Gets or sets the mass of the modification.
        /// </summary>
        public double Mass
        {
            get { return this.mass; }
            set { this.RaiseAndSetIfChanged(ref this.mass, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this modification is defined by an empirical formula.
        /// </summary>
        public bool FromFormulaChecked
        {
            get { return this.fromFormulaChecked; }
            set { this.RaiseAndSetIfChanged(ref this.fromFormulaChecked, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this modification is defined by a mass shift.
        /// </summary>
        public bool FromMassChecked
        {
            get { return this.fromMassChecked; }
            set { this.RaiseAndSetIfChanged(ref this.fromMassChecked, value); }
        }

        /// <summary>
        /// Implementation for SaveCommand.
        /// Creates a modification from the values selected and sets the status.
        /// </summary>
        private void SaveImplementation()
        {
            if (string.IsNullOrWhiteSpace(this.ModificationName))
            {
                this.dialogService.MessageBox("Modification must have a name.");
                return;
            }

            double massShift = 0.0;
            if (this.FromMassChecked && (string.IsNullOrEmpty(this.massStr) || !double.TryParse(this.massStr, out massShift)))
            {
                this.dialogService.MessageBox(string.Format("Invalid mass: {0}", this.massStr));
                return;
            }

            this.mass = massShift;

            this.Status = true;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation of CancelCommand.
        /// Gets a command that sets status to false and triggers ready to close event
        /// </summary>
        private void CancelImplementation()
        {
            this.Status = false;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }
    }
}
