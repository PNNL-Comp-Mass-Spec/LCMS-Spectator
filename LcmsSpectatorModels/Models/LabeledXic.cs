using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectatorModels.Models
{
    public class LabeledXic: LabeledIon
    { 
        public List<XicPoint> Xic { get; private set; }

        public LabeledXic(Composition composition, int index, List<XicPoint> xic, IonType ionType,  bool isFragmentIon=true): 
               base(composition, index, ionType, isFragmentIon)
        {
            Xic = xic;
        }

        public override string Label
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
                    if (Index < 0) annotation = String.Format("Precursor [M{0}] ({1}+)", Index, IonType.Charge);
                    else if (Index == 0) annotation = String.Format("Precursor ({0}+)", IonType.Charge);
                    else if (Index > 0) annotation = String.Format("Precursor [M+{0}] ({1}+)", Index, IonType.Charge);
                }
                return annotation;
            }
        }

        public double Area
        {
            get
            {
                return Xic.Sum(xicPoint => xicPoint.Intensity);
            }
        }
    }
}
