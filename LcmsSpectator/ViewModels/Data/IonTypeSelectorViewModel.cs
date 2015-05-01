// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IonTypeSelectorViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model that builds a list of fragment ion types based on selected based ion types, neutral losses, and charge range.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using InformedProteomics.Backend.Data.Spectrometry;

    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Utils;

    using ReactiveUI;

    /// <summary>
    /// A view model that builds a list of fragment ion types based on selected based ion types, neutral losses, and charge range.
    /// </summary>
    public class IonTypeSelectorViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// Minimum of selected charge range.
        /// </summary>
        private int minSelectedCharge;

        /// <summary>
        /// Maximum of selected charge range.
        /// </summary>
        private int maxSelectedCharge;

        /// <summary>
        /// All ion types currently selected
        /// </summary>
        private ReactiveList<IonType> ionTypes;

        /// <summary>
        /// The base ion types that have been selected from the base ion type list.
        /// </summary>
        private IList selectedBaseIonTypes;

        /// <summary>
        /// The neutral losses selected from neutral loss list.
        /// </summary>
        private IList selectedNeutralLosses;

        /// <summary>
        /// The selected charge to adjust charge range around.
        /// </summary>
        private int selectedCharge;

        /// <summary>
        /// The charge value selected for minimum of charge range.
        /// </summary>
        private int minCharge;

        /// <summary>
        /// The charge value selected for maximum of charge range.
        /// </summary>
        private int maxCharge;

        /// <summary>
        /// The highest possible charge of the maximum charge value in the charge range.
        /// </summary>
        private int absoluteMaxCharge;

        /// <summary>
        /// The activation method used to automatically select ideal ion types.
        /// </summary>
        private ActivationMethod activationMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="IonTypeSelectorViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public IonTypeSelectorViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.minSelectedCharge = 1;
            this.minSelectedCharge = 2;
            this.MinCharge = 2;
            this.AbsoluteMaxCharge = 50;
            var setIonChargesCommand = ReactiveCommand.Create();
            setIonChargesCommand.Subscribe(_ => this.SetIonChargesImplementation());
            this.SetIonChargesCommand = setIonChargesCommand;

            this.BaseIonTypes = new List<BaseIonType>
            {
                BaseIonType.A, BaseIonType.B, BaseIonType.C,
                BaseIonType.X, BaseIonType.Y, BaseIonType.Z
            };

            this.selectedBaseIonTypes = new List<BaseIonType>
            {
                BaseIonType.B,
                BaseIonType.Y
            };

            this.NeutralLosses = NeutralLoss.CommonNeutralLosses.ToList();
            this.SelectedNeutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };

            this.UpdateIonTypes();

            this.WhenAnyValue(x => x.SelectedCharge)
                .Subscribe(charge =>
                {
                    this.MinCharge = 1;
                    var max = Math.Min(Math.Max(charge - 1, 2), Constants.MaxCharge);
                    this.MaxCharge = max;
                    this.UpdateIonTypes();
                });

            this.WhenAnyValue(x => x.SelectedBaseIonTypes, x => x.SelectedNeutralLosses)
                .Subscribe(_ => this.UpdateIonTypes());

            this.WhenAnyValue(x => x.MinCharge, x => x.MaxCharge)
                .Subscribe(_ => this.SetIonChargesImplementation());

            this.WhenAnyValue(x => x.ActivationMethod)
                .Subscribe(this.SetActivationMethod);

            IcParameters.Instance.WhenAnyValue(x => x.CidHcdIonTypes, x => x.EtdIonTypes)
                .Subscribe(_ => this.SetActivationMethod(this.ActivationMethod));
        }

        /// <summary>
        /// Gets a list of all possible base ion types (A,B,C,X,Y,Z)
        /// </summary>
        public List<BaseIonType> BaseIonTypes { get; private set; }

        /// <summary>
        /// Gets a list of all possible neutral losses (-H2O, -NH3, No Loss)
        /// </summary>
        public List<NeutralLoss> NeutralLosses { get; private set; }

        /// <summary>
        /// Gets a command that calculates new ion types when the charge range changes.
        /// </summary>
        public IReactiveCommand SetIonChargesCommand { get; private set; }

        /// <summary>
        /// Gets all ion types currently selected
        /// </summary>
        public ReactiveList<IonType> IonTypes
        {
            get { return this.ionTypes; }
            private set { this.RaiseAndSetIfChanged(ref this.ionTypes, value); }
        }

        /// <summary>
        /// Gets or sets the base ion types that have been selected from the base ion type list.
        /// </summary>
        public IList SelectedBaseIonTypes
        {
            get { return this.selectedBaseIonTypes; }
            set { this.RaiseAndSetIfChanged(ref this.selectedBaseIonTypes, value); }
        }

        /// <summary>
        /// Gets or sets the neutral losses selected from neutral loss list.
        /// </summary>
        public IList SelectedNeutralLosses
        {
            get { return this.selectedNeutralLosses; }
            set { this.RaiseAndSetIfChanged(ref this.selectedNeutralLosses, value); }
        }

        /// <summary>
        /// Gets or sets the selected charge to adjust charge range around.
        /// </summary>
        public int SelectedCharge
        {
            get { return this.selectedCharge; }
            set { this.RaiseAndSetIfChanged(ref this.selectedCharge, value); }
        }

        /// <summary>
        /// Gets or sets the charge value selected for minimum of charge range.
        /// </summary>
        public int MinCharge
        {
            get { return this.minCharge; }
            set { this.RaiseAndSetIfChanged(ref this.minCharge, value); }
        }

        /// <summary>
        /// Gets or sets the charge value selected for maximum of charge range.
        /// </summary>
        public int MaxCharge
        {
            get { return this.maxCharge; }
            set { this.RaiseAndSetIfChanged(ref this.maxCharge, value); }
        }

        /// <summary>
        /// Gets or sets the highest possible charge of the maximum charge value in the charge range.
        /// </summary>
        public int AbsoluteMaxCharge
        {
            get { return this.absoluteMaxCharge; }
            set { this.RaiseAndSetIfChanged(ref this.absoluteMaxCharge, value); }
        }

        /// <summary>
        /// Gets or sets the activation method used to automatically select ideal ion types.
        /// </summary>
        public ActivationMethod ActivationMethod
        {
            get { return this.activationMethod; }
            set { this.RaiseAndSetIfChanged(ref this.activationMethod, value); }
        }

        /// <summary>
        /// Implementation for SetIonChargesCommand.
        /// Calculates new ion types when the charge range changes.
        /// </summary>
        private void SetIonChargesImplementation()
        {
            try
            {
                if (this.MinCharge < 1)
                {
                    throw new FormatException("Min charge must be greater than 1.");
                }

                if (this.MaxCharge > this.absoluteMaxCharge)
                {
                    throw new FormatException(string.Format("Max charge must be {0} or less.", this.absoluteMaxCharge));
                }

                if (this.MinCharge > this.MaxCharge)
                {
                    throw new FormatException("Max charge cannot be less than min charge.");
                }

                this.minSelectedCharge = this.MinCharge;
                this.maxSelectedCharge = this.MaxCharge;
                this.UpdateIonTypes();
            }
            catch (FormatException f)
            {
                this.dialogService.ExceptionAlert(f);
                this.MinCharge = this.minSelectedCharge;
                this.MaxCharge = this.maxSelectedCharge;
            }
        }

        /// <summary>
        /// Update ion types based on selected base ion types, selected neutral losses, and charge range.
        /// </summary>
        private void UpdateIonTypes()
        {
            // set ion types
            var selectedBase = this.SelectedBaseIonTypes.Cast<BaseIonType>().ToList();
            var selectedLosses = this.SelectedNeutralLosses.Cast<NeutralLoss>().ToList();
            this.IonTypes =
                new ReactiveList<IonType>(
                    IonUtils.GetIonTypes(
                        IcParameters.Instance.IonTypeFactory,
                        selectedBase,
                        selectedLosses, 
                        this.MinCharge, 
                        this.MaxCharge));
        }

        /// <summary>
        /// Set ion types based on the selected activation method.
        /// </summary>
        /// <param name="selectedActivationMethod">The selected activation method.</param>
        private void SetActivationMethod(ActivationMethod selectedActivationMethod)
        {
            if (!IcParameters.Instance.AutomaticallySelectIonTypes)
            {
                return;
            }

            if (selectedActivationMethod == ActivationMethod.ETD
                && !Equals(this.SelectedBaseIonTypes, IcParameters.Instance.EtdIonTypes))
            {
                this.SelectedBaseIonTypes = IcParameters.Instance.EtdIonTypes;
            }
            else if (selectedActivationMethod != ActivationMethod.ETD
                     && !Equals(this.SelectedBaseIonTypes, IcParameters.Instance.CidHcdIonTypes))
            {
                this.SelectedBaseIonTypes = IcParameters.Instance.CidHcdIonTypes;
            }
        }
    }
}
