using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public event EventHandler IonTypesUpdated;
        public IonTypeSelectorViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            SetIonChargesCommand = new RelayCommand(SetIonCharges);

            BaseIonTypes = new List<BaseIonType>
            {
                BaseIonType.A, BaseIonType.B, BaseIonType.C,
                BaseIonType.X, BaseIonType.Y, BaseIonType.Z
            };

            SelectedBaseIonTypes = new List<BaseIonType>
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
            get
            {
                var selectedBaseIonTypes = SelectedBaseIonTypes.Cast<BaseIonType>().ToList();
                var selectedNeutralLosses = SelectedNeutralLosses.Cast<NeutralLoss>().ToList();
                return IonUtils.GetIonTypes(IcParameters.Instance.IonTypeFactory, selectedBaseIonTypes,
                    selectedNeutralLosses, MinCharge, MaxCharge);
            }
        }

        public IList SelectedBaseIonTypes
        {
            get { return _selectedBaseIonTypes; }
            set
            {
                if (value == null) return;
                _selectedBaseIonTypes = value;
                if (IonTypesUpdated != null) IonTypesUpdated(this, null);
                OnPropertyChanged("SelectedBaseIonTypes");
            }
        }

        public IList SelectedNeutralLosses
        {
            get { return _selectedNeutralLosses; }
            set
            {
                if (value == null) return;
                _selectedNeutralLosses = value;
                if (IonTypesUpdated != null) IonTypesUpdated(this, null);
                OnPropertyChanged("SelectedNeutralLosses");
            }
        }

        public int MinCharge
        {
            get { return _minCharge; }
            set
            {
                _minCharge = value;
                OnPropertyChanged("MinCharge");
            }
        }

        public int MaxCharge
        {
            get { return _maxCharge; }
            set
            {
                _maxCharge = value;
                OnPropertyChanged("MaxCharge");
            }
        }

        public int AbsoluteMaxCharge
        {
            get { return _absoluteMaxCharge; }
            set
            {
                _absoluteMaxCharge = value;
                MaxCharge = _absoluteMaxCharge;
                OnPropertyChanged("AbsoluteMaxCharge");
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
                if (IonTypesUpdated != null)  IonTypesUpdated(this, null);
            }
            catch (FormatException f)
            {
                _dialogService.ExceptionAlert(f);
                MinCharge = _minSelectedCharge;
                MaxCharge = _maxSelectedCharge;
            }
        }

        private readonly IDialogService _dialogService;
        private IList _selectedBaseIonTypes;
        private IList _selectedNeutralLosses;
        private int _minCharge;
        private int _maxCharge;
        private int _absoluteMaxCharge;
        private int _minSelectedCharge;
        private int _maxSelectedCharge;
    }
}
