// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomModificationViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a view model for editing the empirical formula or mass shift of a modification.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Reactive;
using InformedProteomics.Backend.Data.Composition;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Modifications
{
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
            ModificationName = modificationName;
            ModificationNameReadOnly = modificationNameReadOnly;
            Composition = new Composition(0, 0, 0, 0, 0);
            Status = false;

            FromFormulaChecked = true;
            FromMassChecked = false;

            SaveCommand = ReactiveCommand.Create(SaveImplementation);
            CancelCommand = ReactiveCommand.Create(CancelImplementation);

            this.WhenAnyValue(x => x.Mass).Subscribe(mass => MassStr = mass.ToString(CultureInfo.InvariantCulture));
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
            Composition = composition;
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
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        /// <summary>
        /// Gets a command that sets status to false.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        /// <summary>
        /// Gets or sets the name of the modification.
        /// </summary>
        public string ModificationName
        {
            get => modificationName;
            set => this.RaiseAndSetIfChanged(ref modificationName, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the modification name is readonly
        /// </summary>
        public bool ModificationNameReadOnly
        {
            get => modificationNameReadOnly;
            set => this.RaiseAndSetIfChanged(ref modificationNameReadOnly, value);
        }

        /// <summary>
        /// Gets or sets the number of carbon molecules in the modification formula
        /// </summary>
        public int C
        {
            get => c;
            set => this.RaiseAndSetIfChanged(ref c, value);
        }

        /// <summary>
        /// Gets or sets the number of hydrogen molecules in the modification formula
        /// </summary>
        public int H
        {
            get => h;
            set => this.RaiseAndSetIfChanged(ref h, value);
        }

        /// <summary>
        /// Gets or sets the number of nitrogen molecules in the modification formula
        /// </summary>
        public int N
        {
            get => n;
            set => this.RaiseAndSetIfChanged(ref n, value);
        }

        /// <summary>
        /// Gets or sets the number of oxygen molecules in the modification formula
        /// </summary>
        public int O
        {
            get => o;
            set => this.RaiseAndSetIfChanged(ref o, value);
        }

        /// <summary>
        /// Gets or sets number of sulfur molecules in the modification formula
        /// </summary>
        public int S
        {
            get => s;
            set => this.RaiseAndSetIfChanged(ref s, value);
        }

        /// <summary>
        /// Gets or sets the number of phosphorous molecules in the modification formula
        /// </summary>
        public int P
        {
            get => p;
            set => this.RaiseAndSetIfChanged(ref p, value);
        }

        /// <summary>
        /// Gets or sets the composition of the empirical formula for this modification.
        /// </summary>
        public Composition Composition
        {
            get => new Composition(C, H, N, O, S);

            set
            {
                C = value.C;
                H = value.H;
                N = value.N;
                O = value.O;
                S = value.S;
            }
        }

        /// <summary>
        /// Gets or sets the mass of the modification as a string.
        /// </summary>
        public string MassStr
        {
             get => massStr;
            set => this.RaiseAndSetIfChanged(ref massStr, value);
        }

        /// <summary>
        /// Gets or sets the mass of the modification.
        /// </summary>
        public double Mass
        {
            get => mass;
            set => this.RaiseAndSetIfChanged(ref mass, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this modification is defined by an empirical formula.
        /// </summary>
        public bool FromFormulaChecked
        {
            get => fromFormulaChecked;
            set => this.RaiseAndSetIfChanged(ref fromFormulaChecked, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this modification is defined by a mass shift.
        /// </summary>
        public bool FromMassChecked
        {
            get => fromMassChecked;
            set => this.RaiseAndSetIfChanged(ref fromMassChecked, value);
        }

        /// <summary>
        /// Implementation for SaveCommand.
        /// Creates a modification from the values selected and sets the status.
        /// </summary>
        private void SaveImplementation()
        {
            if (string.IsNullOrWhiteSpace(ModificationName))
            {
                dialogService.MessageBox("Modification must have a name.");
                return;
            }

            var massShift = 0.0;
            if (FromMassChecked && (string.IsNullOrEmpty(massStr) || !double.TryParse(massStr, out massShift)))
            {
                dialogService.MessageBox(string.Format("Invalid mass: {0}", massStr));
                return;
            }

            mass = massShift;

            Status = true;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implementation of CancelCommand.
        /// Gets a command that sets status to false and triggers ready to close event
        /// </summary>
        private void CancelImplementation()
        {
            Status = false;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
