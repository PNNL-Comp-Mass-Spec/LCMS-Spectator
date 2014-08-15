using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using MultiDimensionalPeakFinding;

namespace LcmsSpectatorModels.Utils
{
    public class IonUtils
    {
        public static List<Composition> GetCompositions(Sequence sequence, bool prefix)
        {
            var compositions = new List<Composition>();
            for (int i = 1; i < sequence.Count; i++)
            {
                compositions.Add(prefix
                    ? sequence.GetComposition(0, i)
                    : sequence.GetComposition(i, sequence.Count));
            }
            if (!prefix) compositions.Reverse();
            return compositions;
        }

        public static IonType GetIonType(IonTypeFactory ionTypeFactory,
                                         BaseIonType baseIonType, NeutralLoss neutralLoss,
                                         int charge)
        {
            var chargeStr = "";
            if (charge > 1) chargeStr = charge.ToString(CultureInfo.InvariantCulture);
            var name = String.Format("{0}{1}{2}", baseIonType.Symbol, chargeStr, neutralLoss.Name);
            return ionTypeFactory.GetIonType(name);
        }

        public static List<IonType> GetIonTypes(IonTypeFactory ionTypeFactory,
                                                IList<BaseIonType> baseIonTypes, IList<NeutralLoss> neutralLosses,
                                                int minCharge, int maxCharge)
        {
            var ionTypes = new List<IonType>();
            foreach (var baseIonType in baseIonTypes)
            {
                foreach (var neutralLoss in neutralLosses)
                {
                    for (int i = minCharge; i <= maxCharge; i++)
                    {
                        ionTypes.Add(GetIonType(ionTypeFactory, baseIonType, neutralLoss, i));
                    }
                }
            }
            return ionTypes;
        }

        public static Ion GetPrecursorIon(Sequence sequence, int charge)
        {
            var composition = sequence.Aggregate(Composition.H2O, (current, aa) => current + aa.Composition);
            return new Ion(composition, charge);
        }

        public static LabeledIon GetLabeledPrecursorIon(Sequence sequence, int charge, int isotopeIndex = 0)
        {
            var precursorIonType = new IonType("Precursor", Composition.H2O, charge, false);
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            return new LabeledIon(composition, isotopeIndex, precursorIonType, false);
        }

        public static List<LabeledIon> GetFragmentIonLabels(Sequence sequence, int charge, IList<IonType> ionTypes)
        {
            var ions = new List<LabeledIon>();
            foreach (var ionType in ionTypes)
            {
                var compositions = GetCompositions(sequence, ionType.IsPrefixIon);
                for (int i = 0; i < compositions.Count; i++)
                {
                    ions.Add(new LabeledIon(compositions[i], i+1, ionType, true, GetPrecursorIon(sequence, charge)));
                }
            }
            return ions;
        }

        public static List<LabeledIon> GetPrecursorIonLabels(Sequence sequence, int charge, int minIsotopeIndex, int maxIsotopeIndex)
        {
            var ions = new List<LabeledIon>();
            for (int i = minIsotopeIndex; i <= maxIsotopeIndex; i++)
            {
                ions.Add(GetLabeledPrecursorIon(sequence, charge, i));
            }
            return ions;
        }

        public static List<LabeledIon> ReduceLabels(List<LabeledIon> source, List<LabeledIon> target)
        {
            return source.Where(target.Contains).ToList();
        }

        public static IList<XicRetPoint> SmoothXic(SavitzkyGolaySmoother smoother, IList<XicRetPoint> xic)
        {
            var xicP = new double[xic.Count];
            for (int i = 0; i < xic.Count; i++)
            {
                xicP[i] = xic[i].Intensity;
            }
            var smoothedPoints = smoother.Smooth(xicP);
            var smoothedXic = new List<XicRetPoint>();
            for (int i = 0; i < xicP.Length; i++)
            {
                smoothedXic.Add(new XicRetPoint(xic[i].ScanNum, xic[i].RetentionTime, xic[i].Mz, smoothedPoints[i]));
            }
            return smoothedXic;
        }
    }
}
