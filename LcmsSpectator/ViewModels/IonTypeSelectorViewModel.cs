using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Utils;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class IonTypeSelectorViewModel: ReactiveObject
    {
        public List<BaseIonType> BaseIonTypes { get; private set; }
        public List<NeutralLoss> NeutralLosses { get; private set; }
        public IReactiveCommand SetIonChargesCommand { get; private set; }
        public IonTypeSelectorViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            _minSelectedCharge = 1;
            _minSelectedCharge = 2;
            MinCharge = 2;
            AbsoluteMaxCharge = 50;
            var setIonChargesCommand = ReactiveCommand.Create();
            setIonChargesCommand.Subscribe(_ => SetIonCharges());
            SetIonChargesCommand = setIonChargesCommand;

            BaseIonTypes = new List<BaseIonType>
            {
                BaseIonType.A, BaseIonType.B, BaseIonType.C,
                BaseIonType.X, BaseIonType.Y, BaseIonType.Z
            };

            _selectedBaseIonTypes = new List<BaseIonType>
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
                    var maxCharge = Math.Min(Math.Max(charge - 1, 2), Constants.MaxCharge);
                    MaxCharge = maxCharge;
                    UpdateIonTypes();
                });

            this.WhenAnyValue(x => x.SelectedBaseIonTypes, x => x.SelectedNeutralLosses)
                .Subscribe(_ => UpdateIonTypes());

            this.WhenAnyValue(x => x.MinCharge, x => x.MaxCharge)
                .Subscribe(_ => SetIonCharges());

            this.WhenAnyValue(x => x.ActivationMethod)
                .Subscribe(SetActivationMethod);

            IcParameters.Instance.WhenAnyValue(x => x.CidHcdIonTypes, x => x.EtdIonTypes)
                .Subscribe(_ => SetActivationMethod(ActivationMethod));
        }

        private ReactiveList<IonType> _ionTypes;
        /// <summary>
        /// All ion types currently selected
        /// </summary>
        public ReactiveList<IonType> IonTypes
        {
            get { return _ionTypes; }
            private set { this.RaiseAndSetIfChanged(ref _ionTypes, value); }
        }

        private IList _selectedBaseIonTypes;
        public IList SelectedBaseIonTypes
        {
            get { return _selectedBaseIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _selectedBaseIonTypes, value); }
        }

        private IList _selectedNeutralLosses;
        public IList SelectedNeutralLosses
        {
            get { return _selectedNeutralLosses; }
            set { this.RaiseAndSetIfChanged(ref _selectedNeutralLosses, value); }
        }

        private int _selectedCharge;
        public int SelectedCharge
        {
            get { return _selectedCharge; }
            set { this.RaiseAndSetIfChanged(ref _selectedCharge, value); }
        }

        private int _minCharge;
        public int MinCharge
        {
            get { return _minCharge; }
            set { this.RaiseAndSetIfChanged(ref _minCharge, value); }
        }

        private int _maxCharge;
        public int MaxCharge
        {
            get { return _maxCharge; }
            set { this.RaiseAndSetIfChanged(ref _maxCharge, value); }
        }

        private int _absoluteMaxCharge;
        public int AbsoluteMaxCharge
        {
            get { return _absoluteMaxCharge; }
            set { this.RaiseAndSetIfChanged(ref _absoluteMaxCharge, value); }
        }

        private ActivationMethod _activationMethod;
        public ActivationMethod ActivationMethod
        {
            get { return _activationMethod; }
            set { this.RaiseAndSetIfChanged(ref _activationMethod, value); }
        }

        private void SetIonCharges()
        {
            try
            {
                if (MinCharge < 1) throw new FormatException("Min charge must be greater than 1.");
                if (MaxCharge > _absoluteMaxCharge) throw new FormatException(String.Format("Max charge must be {0} or less.", _absoluteMaxCharge));
                if (MinCharge > MaxCharge) throw new FormatException("Max charge cannot be less than min charge.");
                _minSelectedCharge = MinCharge;
                _maxSelectedCharge = MaxCharge;
                UpdateIonTypes();
            }
            catch (FormatException f)
            {
                _dialogService.ExceptionAlert(f);
                MinCharge = _minSelectedCharge;
                MaxCharge = _maxSelectedCharge;
            }
        }

        private void UpdateIonTypes()
        {
            // set ion types
            var selectedBaseIonTypes = SelectedBaseIonTypes.Cast<BaseIonType>().ToList();
            var selectedNeutralLosses = SelectedNeutralLosses.Cast<NeutralLoss>().ToList();
            IonTypes = new ReactiveList<IonType>(IonUtils.GetIonTypes(IcParameters.Instance.IonTypeFactory, selectedBaseIonTypes,
                selectedNeutralLosses, MinCharge, MaxCharge));
        }

        private void SetActivationMethod(ActivationMethod activationMethod)
        {
            if (!IcParameters.Instance.AutomaticallySelectIonTypes) return;
            if (activationMethod == ActivationMethod.ETD &&
                !Equals(SelectedBaseIonTypes, IcParameters.Instance.EtdIonTypes))
            {
                SelectedBaseIonTypes = IcParameters.Instance.EtdIonTypes;
            }
            else if (activationMethod != ActivationMethod.ETD &&
                     !Equals(SelectedBaseIonTypes, IcParameters.Instance.CidHcdIonTypes))
            {
                SelectedBaseIonTypes = IcParameters.Instance.CidHcdIonTypes;
            }
        }

        private readonly IDialogService _dialogService;
        private int _minSelectedCharge;
        private int _maxSelectedCharge;
    }
}
