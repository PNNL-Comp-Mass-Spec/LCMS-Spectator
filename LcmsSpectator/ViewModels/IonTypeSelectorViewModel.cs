using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Utils;

namespace LcmsSpectator.ViewModels
{
    public class IonTypeSelectorViewModel: ViewModelBase
    {
        public List<BaseIonType> BaseIonTypes { get; private set; }
        public List<NeutralLoss> NeutralLosses { get; private set; }
        public RelayCommand SetIonChargesCommand { get; private set; }
        public IonTypeSelectorViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            SetIonChargesCommand = new RelayCommand(SetIonCharges);

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

            _minSelectedCharge = 1;
            _minSelectedCharge = 2;
            MinCharge = 1;
            AbsoluteMaxCharge = 2;
        }

        /// <summary>
        /// All ion types currently selected
        /// </summary>
        public List<IonType> IonTypes
        {
            get { return _ionTypes; }
            private set
            {
                var oldIonTypes = _ionTypes;
                _ionTypes = value;
                Broadcast(oldIonTypes, _ionTypes, "IonTypes");
                RaisePropertyChanged();
            }
        }

        public IList SelectedBaseIonTypes
        {
            get { return _selectedBaseIonTypes; }
            set
            {
                if (value == null) return;
                _selectedBaseIonTypes = value;
                UpdateIonTypes();
                RaisePropertyChanged();
            }
        }

        public IList SelectedNeutralLosses
        {
            get { return _selectedNeutralLosses; }
            set
            {
                if (value == null) return;
                _selectedNeutralLosses = value;
                UpdateIonTypes();
                RaisePropertyChanged();
            }
        }

        public int MinCharge
        {
            get { return _minCharge; }
            set
            {
                _minCharge = value;
                RaisePropertyChanged();
            }
        }

        public int MaxCharge
        {
            get { return _maxCharge; }
            set
            {
                _maxCharge = value;
                RaisePropertyChanged();
            }
        }

        public int AbsoluteMaxCharge
        {
            get { return _absoluteMaxCharge; }
            set
            {
                _absoluteMaxCharge = value;
                MaxCharge = _absoluteMaxCharge;
                RaisePropertyChanged();
            }
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
            IonTypes = IonUtils.GetIonTypes(IcParameters.Instance.IonTypeFactory, selectedBaseIonTypes,
                selectedNeutralLosses, MinCharge, MaxCharge);
        }

        private readonly IDialogService _dialogService;
        private IList _selectedBaseIonTypes;
        private IList _selectedNeutralLosses;
        private int _minCharge;
        private int _maxCharge;
        private int _absoluteMaxCharge;
        private int _minSelectedCharge;
        private int _maxSelectedCharge;
        private List<IonType> _ionTypes;
    }
}
