using System;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectatorModels.Models
{
    public class LabeledData
    {
        public int Scan { get; private set; }
        public int Index { get; private set; }
        public IonType IonType { get; private set; }
        public bool IsFragmentIon { get; private set; }

        public LabeledData(int scan, int index, IonType ionType,  bool isFragmentIon)
        {
            Scan = scan;
            Index = index;
            IonType = ionType;
            IsFragmentIon = isFragmentIon;
        }

        public virtual string Label
        {
            get
            {
                var annotation = "";
                if (IsFragmentIon)
                {
                    var baseIonTypeName = IonType.BaseIonType.Symbol;
                    var neutralLoss = IonType.NeutralLoss.Name;
                    annotation = String.Format("{0}{1}{2}({3}+)", baseIonTypeName, Index, neutralLoss, IonType.Charge);
                }
                else
                {
                    if (Index < 0) annotation = String.Format("Precursor{0} ({1}+)", Index, IonType.Charge);
                    else if (Index == 0) annotation = String.Format("Precursor ({0}+)", IonType.Charge);
                    else if (Index > 0) annotation = String.Format("Precursor+{0} ({1}+)", Index, IonType.Charge);
                }
                return annotation;
            }
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
