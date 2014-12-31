using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;

namespace LcmsSpectator.ViewModels
{
    public class PrSmViewModel: ViewModelBase
    {

        public PrSmViewModel(IMessenger messenger)
        {
            MessengerInstance = messenger;
            Sequence = new Sequence(new List<AminoAcid>());
            MessengerInstance.Register<PropertyChangedMessage<PrSm>>(this, SelectedPrSmChanged);
            MessengerInstance.Register<XicPlotViewModel.SelectedScanChangedMessage>(this, SelectedScanChanged);
            Messenger.Default.Register<PropertyChangedMessage<PrSm>>(this, SelectedPrSmChanged);
            Messenger.Default.Register<SettingsChangedNotification>(this, SettingsChanged);
        }

        #region PrSm Properties
        public PrSm PrSm
        {
            get
            {
                var prsm = new PrSm
                {
                    Lcms = Lcms,
                    RawFileName = RawFileName,
                    Scan = Scan,
                    Sequence = Sequence,
                    SequenceText = SequenceText,
                    Charge = Charge,
                    Heavy = Heavy,
                    Score = Score,
                    QValue = QValue,
                };
                return prsm;
            }
            set
            {
                PrSm newPrSm = value ?? new PrSm();
                MessengerInstance.Send(new ClearAllNotification(this));
                var oldValue = PrSm;
                //Lcms = newPrSm.Lcms;
                RawFileName = newPrSm.RawFileName;
                Heavy = newPrSm.Heavy;
                Score = newPrSm.Score;
                _noLabelPrecursorMz = newPrSm.PrecursorMz;
                _heavyPrecursorMz = IonUtils.GetPrecursorMz(IonUtils.GetHeavySequence(newPrSm.Sequence, IcParameters.Instance.HeavyModifications), newPrSm.Charge);
                PrecursorMz = Heavy ? _heavyPrecursorMz : _noLabelPrecursorMz;
                ProteinNameDesc = newPrSm.ProteinNameDesc;
                Scan = newPrSm.Scan;
                RaisePropertyChanged("PrSm", oldValue, newPrSm, true);
                Sequence = newPrSm.Sequence;
                SequenceText = newPrSm.SequenceText;
                Charge = newPrSm.Charge;
            }
        }

        public ILcMsRun Lcms
        {
            get { return _lcms; }
            set
            {
                var oldValue = _lcms;
                _lcms = value;
                RaisePropertyChanged("Lcms", oldValue, _lcms, true);
            }
        }

        public string RawFileName
        {
            get { return _rawFileName; }
            set
            {
                var oldValue = _rawFileName;
                _rawFileName = value;
                RaisePropertyChanged("RawFileName", oldValue, _rawFileName, true);
            }
        }

        public int Scan
        {
            get { return _scan; }
            set
            {
                var oldValue = _scan;
                _scan = value;
                RaisePropertyChanged("Scan", oldValue, _scan, true);
            }
        }

        public string ProteinNameDesc
        {
            get { return _proteinNameDesc; }
            set
            {
                var oldValue = _proteinNameDesc;
                _proteinNameDesc = value;
                RaisePropertyChanged("ProteinNameDesc", oldValue, _proteinNameDesc, true);
            }
        }

        public Sequence Sequence
        {
            get { return _sequence; }
            set
            {
                var oldValue = _sequence;
                _sequence = value;
                RaisePropertyChanged("Sequence", oldValue, _sequence, true);
            }
        }

        public Sequence LightSequence
        {
            get { return _lightSequence; }
            set
            {
                var oldValue = _lightSequence;
                _lightSequence = value;
                RaisePropertyChanged("LightSequence", oldValue, _lightSequence, true);
            }
        }

        public Sequence HeavySequence
        {
            get { return _heavySequence; }
            set
            {
                var oldValue = _heavySequence;
                _heavySequence = value;
                RaisePropertyChanged("HeavySequence", oldValue, _heavySequence, true);
            }
        }

        public string SequenceText
        {
            get { return _sequenceText; }
            set
            {
                var oldValue = _sequenceText;
                _sequenceText = value;
                RaisePropertyChanged("SequenceText", oldValue, _sequenceText, true);
            }
        }

        public int Charge
        {
            get { return _charge; }
            set
            {
                var oldValue = _charge;
                _charge = value;
                RaisePropertyChanged("Charge", oldValue, _charge, true);
            }
        }

        public bool Heavy
        {
            get { return _heavy; }
            set
            {
                var oldValue = _heavy;
                _heavy = value;
                PrecursorMz = _heavy ? _heavyPrecursorMz : _noLabelPrecursorMz;
                RaisePropertyChanged("Heavy", oldValue, _heavy, true);
            }
        }

        public double QValue
        {
            get { return _qValue; }
            set
            {
                var oldValue = _qValue;
                _qValue = value;
                RaisePropertyChanged("QValue", oldValue, _qValue, true);
            }
        }

        public double Score
        {
            get { return _score; }
            set
            {
                var oldValue = _score;
                _score = value;
                RaisePropertyChanged("Score", oldValue, _score, true);
            }
        }

        public double PrecursorMz
        {
            get { return _precursorMz; }
            set
            {
                var oldValue = _precursorMz;
                _precursorMz = value;
                RaisePropertyChanged("PrecursorMz", oldValue, _precursorMz, true);
            }
        }

        private Sequence _sequence;
        private string _sequenceText;
        private int _charge;
        private bool _heavy;
        private int _scan;
        private ILcMsRun _lcms;
        private string _rawFileName;
        private double _qValue;
        private double _score;
        private double _precursorMz;
        private double _noLabelPrecursorMz;
        private double _heavyPrecursorMz;
        private string _proteinNameDesc;
        private Sequence _lightSequence;
        private Sequence _heavySequence;

        #endregion

        private void SelectedPrSmChanged(PropertyChangedMessage<PrSm> message)
        {
            if (message.PropertyName == "SelectedPrSm")
            {
                PrSm = message.NewValue;
            }
        }

        private void SelectedScanChanged(XicPlotViewModel.SelectedScanChangedMessage message)
        {
            Scan = message.Scan;
        }

        private void SettingsChanged(SettingsChangedNotification message)
        {
            LightSequence = IonUtils.GetHeavySequence(Sequence, IcParameters.Instance.LightModifications);
            HeavySequence = IonUtils.GetHeavySequence(Sequence, IcParameters.Instance.HeavyModifications);
        }
    }

    public class ClearAllNotification : NotificationMessage
    {
        public ClearAllNotification(object sender, string notification = "ClearAll")
            : base(sender, notification)
        {
        }
    }
}
