using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;

namespace LcmsSpectator.ViewModels
{
    public class IonTypeSelectorViewModel: ViewModelBase
    {
        public List<BaseIonType> BaseIonTypes { get; private set; } 
        public List<NeutralLoss> NeutralLosses { get; private set; }

        public DelegateCommand SetIonChargesCommand { get; private set; }

        public event EventHandler IonTypesUpdated;

        public IonTypeSelectorViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            SetIonChargesCommand = new DelegateCommand(SetIonCharges);

            BaseIonTypes = new List<BaseIonType>
            {
                BaseIonType.A, BaseIonType.B, BaseIonType.C,
                BaseIonType.X, BaseIonType.Y, BaseIonType.Z
            };
            NeutralLosses = NeutralLoss.CommonNeutralLosses.ToList();

            SelectedBaseIonTypes = new List<BaseIonType>
            {
                BaseIonType.B,
                BaseIonType.Y
            };

            _minSelectedCharge = 1;
            _minSelectedCharge = 2;
            MinCharge = 1;
            AbsoluteMaxCharge = 2;

            SelectedNeutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
        }

        public List<BaseIonType> SelectedBaseIonTypes
        {
            get { return _selectedBaseIonTypes; }
            set
            {
                _selectedBaseIonTypes = value;
                if (IonTypesUpdated != null) IonTypesUpdated(this, null);
                OnPropertyChanged("SelectedBaseIonTypes");
            }
        }

        public List<NeutralLoss> SelectedNeutralLosses
        {
            get { return _selectedNeutralLosses; }
            set
            {
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

        private List<BaseIonType> _selectedBaseIonTypes;
        private List<NeutralLoss> _selectedNeutralLosses;
        private readonly IDialogService _dialogService;

        private int _minCharge;
        private int _maxCharge;
        private int _absoluteMaxCharge;
        private int _minSelectedCharge;
        private int _maxSelectedCharge;
    }
}
