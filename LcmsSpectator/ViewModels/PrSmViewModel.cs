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
                Mass = newPrSm.Mass;
                PrecursorMz = Heavy ? _heavyPrecursorMz : _noLabelPrecursorMz;
                ProteinNameDesc = newPrSm.ProteinNameDesc;
                Scan = newPrSm.Scan;
                RaisePropertyChanged("PrSm", oldValue, newPrSm, true);
                Sequence = newPrSm.Sequence;
                SequenceText = newPrSm.SequenceText;
                Charge = newPrSm.Charge;
            }
        }

        private ILcMsRun _lcms;
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

        private string _rawFileName;
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

        private int _scan;
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

        private string _proteinNameDesc;
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

        private Sequence _sequence;
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

        private Sequence _lightSequence;
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

        private Sequence _heavySequence;
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

        private string _sequenceText;
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

        private int _charge;
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

        private bool _heavy;
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

        private double _qValue;
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

        private double _score;
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

        private double _mass;
        public double Mass
        {
            get { return _mass; }
            set
            {
                var oldValue = _mass;
                _mass = value;
                RaisePropertyChanged("Mass", oldValue, _mass, true);
            }
        }

        private double _precursorMz;
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

        private double _noLabelPrecursorMz;
        private double _heavyPrecursorMz;
    }

    public class ClearAllNotification : NotificationMessage
    {
        public ClearAllNotification(object sender, string notification = "ClearAll")
            : base(sender, notification)
        {
        }
    }
}
