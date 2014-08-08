using System;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectatorModels.Models
{
    public class LabeledIon: IComparable<LabeledIon>, IEquatable<LabeledIon>
    {
        public Composition Composition { get; private set; }
        public Ion PrecursorIon { get; private set; }
        public int Index { get; private set; }
        public IonType IonType { get; private set; }
        public bool IsFragmentIon { get; private set; }

        public LabeledIon(Composition composition, int index, IonType ionType,  bool isFragmentIon, Ion precursorIon=null)
        {
            Composition = composition;
            PrecursorIon = precursorIon;
            Index = index;
            IonType = ionType;
            IsFragmentIon = isFragmentIon;
        }

        public Ion Ion
        {
            get { return IonType.GetIon(Composition); }
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

        public int CompareTo(LabeledIon other)
        {
            return String.Compare(Label, other.Label, StringComparison.Ordinal);
        }

        public bool Equals(LabeledIon other)
        {
            return Label.Equals(other.Label);
        }
    }
}
