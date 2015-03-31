using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.Utils;
using LcmsSpectator.Models;
using LcmsSpectator.ViewModels;
using ReactiveUI;

namespace LcmsSpectator.Utils
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

        public static int GetNumNeighboringChargeStates(int charge)
        {
            int chargeStates = 1;
            if (charge >= 2 && charge <= 4) chargeStates = 1;
            else if (charge >= 5 && charge <= 10) chargeStates = 2;
            else if (charge >= 11 && charge <= 20) chargeStates = 3;
            else if (charge >= 21 && charge <= 30) chargeStates = 4;
            else if (charge >= 31) chargeStates = 5;
            return chargeStates;
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
            #pragma warning disable 0618
            var precursorIonType = new IonType("Precursor", Composition.H2O, charge, false);
            #pragma warning restore 0618
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

        public static Tuple<Peak[], double> GetIonPeaks(Ion ion, Spectrum spectrum, Tolerance tolerance, bool decharged=false)
        {
            //if (decharged && !ion.IsFragmentIon) ion = new LabeledIon(ion.Composition, ion.Index, 
            //                                                         new IonType(ion.IonType.Name,
            //                                                                     ion.IonType.OffsetComposition, 1, 
            //                                                                     ion.IonType.IsPrefixIon), 
            //                                                         false, ion.PrecursorIon, ion.IsChargeState);
            var ionCorrelation = spectrum.GetCorrScore(ion, tolerance);
            var isotopePeaks = spectrum.GetAllIsotopePeaks(ion, tolerance, 0.1);
            return new Tuple<Peak[], double>(isotopePeaks, ionCorrelation);
        }

        //public static List<Tuple<Peak[], double>> GetIonPeaks(List<LabeledIon> ions, Spectrum spectrum,
        //                                                Tolerance productIonTolerance, Tolerance precursorIonTolerance,
        //                                                double corrThreshold, bool decharged)
        //{
        //    var labeledIonPeaks = new List<Tuple<Peak[], double>>();
        //    foreach (var ion in ions)
        //    {
        //        var labeledIon = GetIonPeaks(ion, spectrum, tolerance, decharged);
        //        if (labeledIon.Item2 >= corrThreshold) labeledIonPeaks.Add(labeledIon);
        //    }
        //    return labeledIonPeaks;
        //}

        public static LabeledIon GetDeconvolutedLabeledIon(LabeledIon labeledIon, IonTypeFactory ionTypeFactory)
        {
            var deconIonTypeName = labeledIon.IonType.Name.Insert(1, "'");
            var ionType = ionTypeFactory.GetIonType(deconIonTypeName);
            var deconLabeledIon = new LabeledIon(labeledIon.Composition, labeledIon.Index, ionType,
                labeledIon.IsFragmentIon, labeledIon.PrecursorIon, labeledIon.IsChargeState);
            return deconLabeledIon;
        }

        /// <summary>
        /// Calculate M/Z error of peaks in isotope envelope.
        /// </summary>
        /// <param name="peaks">The peaks to calculate error for.</param>
        /// <param name="ion">The ion to calculate theoretical isotope envelope for.</param>
        /// <param name="relativeIntensityThreshold">Relative intensity threshold for calculating isotopes</param>
        /// <returns>Array of ppm errors for each peak.</returns>
        public static double?[] GetIsotopePpmError(Peak[] peaks, Ion ion, double relativeIntensityThreshold)
        {
            var isotopes = ion.GetIsotopes(relativeIntensityThreshold).ToArray();
            var ppmErrors = new double?[isotopes.Max(i => i.Index)+1];
            foreach (Isotope isotope in isotopes)
            {
                var isotopeIndex = isotope.Index;
                if (peaks[isotopeIndex] == null) ppmErrors[isotopeIndex] = null;
                else ppmErrors[isotopeIndex] = GetPeakPpmError(peaks[isotopeIndex], ion.GetIsotopeMz(isotopeIndex));
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
            // error = (observed - theo)/observed*10e6
            return (peak.Mz - theoMz) / peak.Mz * Math.Pow(10, 6);
        }

        public static XicPoint[] SmoothXic(SavitzkyGolaySmoother smoother, IList<XicPoint> xic)
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
            var smoothedXic = new XicPoint[xicP.Length];
            for (int i = 0; i < xicP.Length; i++)
            {
                smoothedXic[i] = new XicPoint(xic[i].ScanNum, xic[i].Mz, smoothedPoints[i]);
            }
            return smoothedXic;
        }

        public static Sequence GetHeavySequence(Sequence sequence, ReactiveList<ModificationViewModel> mods)
        {
            if (sequence.Count == 0) return sequence;
            var lastAa = sequence[sequence.Count - 1];

            foreach (var mod in mods)
            {
                if (mod.Modification.Equals(Modification.ArgToHeavyArg) && lastAa.Residue == 'R')
                {
                    lastAa = new ModifiedAminoAcid(lastAa, mod.Modification);
                }
                else if (mod.Modification.Equals(Modification.LysToHeavyLys) && lastAa.Residue == 'K')
                {
                    lastAa = new ModifiedAminoAcid(lastAa, mod.Modification);
                }
            }

            var tempSequence = sequence.ToList();
            tempSequence[tempSequence.Count - 1] = lastAa;
            return new Sequence(tempSequence);   
        }

        public static double GetPrecursorMz(Sequence sequence, int charge)
        {
            if (sequence.Count == 0) return 0.0;
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var ion = new Ion(composition + Composition.H2O, charge);
            return Math.Round(ion.GetMostAbundantIsotopeMz(), 2);
        }
    }
}
