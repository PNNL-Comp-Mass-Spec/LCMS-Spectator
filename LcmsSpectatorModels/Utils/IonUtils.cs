using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
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

        public static LabeledIonPeaks GetIonPeaks(LabeledIon ion, Spectrum spectrum, Tolerance productIonTolerance, Tolerance precursorIonTolerance)
        {
            var tolerance = ion.IsFragmentIon ? productIonTolerance : precursorIonTolerance;
            var ionCorrelation = spectrum.GetCorrScore(ion.Ion, tolerance);
            var isotopePeaks = spectrum.GetAllIsotopePeaks(ion.Ion, tolerance, 0.1);
            return new LabeledIonPeaks(ion.Composition, ion.Index, isotopePeaks, ionCorrelation, ion.IonType, ion.IsFragmentIon);
        }

        public static List<LabeledIonPeaks> GetIonPeaks(List<LabeledIon> ions, Spectrum spectrum,
                                                        Tolerance productIonTolerance, Tolerance precursorIonTolerance,
                                                        double corrThreshold)
        {
            var labeledIonPeaks = new List<LabeledIonPeaks>();
            foreach (var ion in ions)
            {
                var labeledIon = GetIonPeaks(ion, spectrum, productIonTolerance, precursorIonTolerance);
                if (labeledIon.CorrelationScore >= corrThreshold) labeledIonPeaks.Add(labeledIon);
            }
            return labeledIonPeaks;
        }

        /// <summary>
        /// Calculate M/Z error of peaks in isotope envelope.
        /// </summary>
        /// <param name="peaks">The peaks to calculate error for.</param>
        /// <param name="ion">The ion to calculate theoretical isotope envelope for.</param>
        /// <returns>Array of ppm errors for each peak.</returns>
        public static double[] GetIsotopePpmError(Peak[] peaks, Ion ion)
        {
            var theoIsotopes = ion.GetIsotopes(peaks.Length).ToArray();
            var ppmErrors = new double[peaks.Length];
            for (int i = 0; i < peaks.Length; i++)
            {
                ppmErrors[i] = GetPeakPpmError(peaks[i], ion.GetIsotopeMz(theoIsotopes[i].Index));
            }
            return ppmErrors;
        }

        /// <summary>
        /// Calculate the error in ppm of a peak and its theoretical M/Z
        /// </summary>
        /// <param name="peak">The peak to calculate the error for</param>
        /// <param name="theoMz">Theoretical M/Z</param>
        /// <returns>The error in ppm.</returns>
        public static double GetPeakPpmError(Peak peak, double theoMz)
        {
            // error = (observed - theo)/(observed*10e6)
            return (peak.Mz - theoMz) / peak.Mz * Math.Pow(10, 6);
        }

        public static IList<XicPoint> SmoothXic(SavitzkyGolaySmoother smoother, IList<XicPoint> xic)
        {
            var xicP = new double[xic.Count];
            for (int i = 0; i < xic.Count; i++)
            {
                xicP[i] = xic[i].Intensity;
            }
            double[] smoothedPoints;
            try
            {
                smoothedPoints = smoother.Smooth(xicP);
            }
            catch (IndexOutOfRangeException)
            {
                smoothedPoints = xicP;
            }
            var smoothedXic = new List<XicPoint>();
            for (int i = 0; i < xicP.Length; i++)
            {
                smoothedXic.Add(new XicPoint(xic[i].ScanNum, xic[i].Mz, smoothedPoints[i]));
            }
            return smoothedXic;
        }
    }
}
