// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IonTypeSelectorViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model that builds a list of fragment ion types based on selected based ion types, neutral losses, and charge range.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Utils;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Data
{
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
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public IonTypeSelectorViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IonTypeSelectorViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public IonTypeSelectorViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            minSelectedCharge = 1;
            minSelectedCharge = 2;
            MinCharge = 2;
            AbsoluteMaxCharge = 50;
            SetIonChargesCommand = ReactiveCommand.Create(SetIonChargesImplementation);

            BaseIonTypes = new List<BaseIonType>
            {
                BaseIonType.A, BaseIonType.B, BaseIonType.C,
                BaseIonType.X, BaseIonType.Y, BaseIonType.Z
            };

            selectedBaseIonTypes = new List<BaseIonType>
            {
                BaseIonType.B,
                BaseIonType.Y
            };

            NeutralLosses = NeutralLoss.CommonNeutralLosses.ToList();
            SelectedNeutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };

            UpdateIonTypes();

            this.WhenAnyValue(x => x.SelectedCharge)
                .Subscribe(charge =>
                {
                    MinCharge = 1;
                    MaxCharge = Math.Min(Math.Max(charge - 1, 2), Constants.MaxCharge);
                    UpdateIonTypes();
                });

            this.WhenAnyValue(x => x.SelectedBaseIonTypes, x => x.SelectedNeutralLosses)
                .Subscribe(_ => UpdateIonTypes());

            this.WhenAnyValue(x => x.MinCharge, x => x.MaxCharge)
                .Subscribe(_ => SetIonChargesImplementation());

            this.WhenAnyValue(x => x.ActivationMethod)
                .Subscribe(SetActivationMethod);

            IcParameters.Instance.WhenAnyValue(x => x.CidHcdIonTypes, x => x.EtdIonTypes)
                .Subscribe(_ => SetActivationMethod(ActivationMethod));
        }

        /// <summary>
        /// Gets a list of all possible base ion types (A,B,C,X,Y,Z)
        /// </summary>
        public List<BaseIonType> BaseIonTypes { get; }

        /// <summary>
        /// Gets a list of all possible neutral losses (-H2O, -NH3, No Loss)
        /// </summary>
        public List<NeutralLoss> NeutralLosses { get; }

        /// <summary>
        /// Gets a command that calculates new ion types when the charge range changes.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SetIonChargesCommand { get; }

        /// <summary>
        /// Gets all ion types currently selected
        /// </summary>
        public ReactiveList<IonType> IonTypes
        {
            get => ionTypes;
            private set => this.RaiseAndSetIfChanged(ref ionTypes, value);
        }

        /// <summary>
        /// Gets or sets the base ion types that have been selected from the base ion type list.
        /// </summary>
        public IList SelectedBaseIonTypes
        {
            get => selectedBaseIonTypes;
            set => this.RaiseAndSetIfChanged(ref selectedBaseIonTypes, value);
        }

        /// <summary>
        /// Gets or sets the neutral losses selected from neutral loss list.
        /// </summary>
        public IList SelectedNeutralLosses
        {
            get => selectedNeutralLosses;
            set => this.RaiseAndSetIfChanged(ref selectedNeutralLosses, value);
        }

        /// <summary>
        /// Gets or sets the selected charge to adjust charge range around.
        /// </summary>
        public int SelectedCharge
        {
            get => selectedCharge;
            set => this.RaiseAndSetIfChanged(ref selectedCharge, value);
        }

        /// <summary>
        /// Gets or sets the charge value selected for minimum of charge range.
        /// </summary>
        public int MinCharge
        {
            get => minCharge;
            set => this.RaiseAndSetIfChanged(ref minCharge, value);
        }

        /// <summary>
        /// Gets or sets the charge value selected for maximum of charge range.
        /// </summary>
        public int MaxCharge
        {
            get => maxCharge;
            set => this.RaiseAndSetIfChanged(ref maxCharge, value);
        }

        /// <summary>
        /// Gets or sets the highest possible charge of the maximum charge value in the charge range.
        /// </summary>
        public int AbsoluteMaxCharge
        {
            get => absoluteMaxCharge;
            set => this.RaiseAndSetIfChanged(ref absoluteMaxCharge, value);
        }

        /// <summary>
        /// Gets or sets the activation method used to automatically select ideal ion types.
        /// </summary>
        public ActivationMethod ActivationMethod
        {
            get => activationMethod;
            set => this.RaiseAndSetIfChanged(ref activationMethod, value);
        }

        /// <summary>
        /// Implementation for SetIonChargesCommand.
        /// Calculates new ion types when the charge range changes.
        /// </summary>
        private void SetIonChargesImplementation()
        {
            try
            {
                if (MinCharge < 1)
                {
                    throw new FormatException("Min charge must be greater than 1.");
                }

                if (MaxCharge > absoluteMaxCharge)
                {
                    throw new FormatException(string.Format("Max charge must be {0} or less.", absoluteMaxCharge));
                }

                if (MinCharge > MaxCharge)
                {
                    throw new FormatException("Max charge cannot be less than min charge.");
                }

                minSelectedCharge = MinCharge;
                maxSelectedCharge = MaxCharge;
                UpdateIonTypes();
            }
            catch (FormatException f)
            {
                dialogService.ExceptionAlert(f);
                MinCharge = minSelectedCharge;
                MaxCharge = maxSelectedCharge;
            }
        }

        /// <summary>
        /// Update ion types based on selected base ion types, selected neutral losses, and charge range.
        /// </summary>
        private void UpdateIonTypes()
        {
            // set ion types
            var selectedBase = SelectedBaseIonTypes.Cast<BaseIonType>().ToList();
            var selectedLosses = SelectedNeutralLosses.Cast<NeutralLoss>().ToList();
            IonTypes =
                new ReactiveList<IonType>(
                    IonUtils.GetIonTypes(
                        IcParameters.Instance.IonTypeFactory,
                        selectedBase,
                        selectedLosses,
                        MinCharge,
                        MaxCharge));
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
                && !Equals(SelectedBaseIonTypes, IcParameters.Instance.EtdIonTypes))
            {
                SelectedBaseIonTypes = IcParameters.Instance.EtdIonTypes;
            }
            else if (selectedActivationMethod != ActivationMethod.ETD
                     && !Equals(SelectedBaseIonTypes, IcParameters.Instance.CidHcdIonTypes))
            {
                SelectedBaseIonTypes = IcParameters.Instance.CidHcdIonTypes;
            }
        }
    }
}
