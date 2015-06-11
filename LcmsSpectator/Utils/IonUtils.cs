// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IonUtils.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This is a utility class for containing methods for performing common calculations on sequences and ions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.Utils;
    using LcmsSpectator.ViewModels;
    using LcmsSpectator.ViewModels.Data;
    using LcmsSpectator.ViewModels.Modifications;

    using ReactiveUI;
    
    /// <summary>
    /// This is a utility class for containing methods for performing common calculations on sequences and ions.
    /// </summary>
    public class IonUtils
    {
        /// <summary>
        /// Get all prefix/suffix compositions for a sequence.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="prefix">A value indicating if this method should calculate prefixes or suffixes.</param>
        /// <returns>
        /// List of compositions.
        /// Prefixes are ordered from start of sequence to end of sequence.
        /// Suffixes are ordered from end of sequence to beginning of sequence.
        /// </returns>
        public static List<Composition> GetCompositions(Sequence sequence, bool prefix)
        {
            var compositions = new List<Composition>();
            for (int i = 1; i < sequence.Count; i++)
            {
                compositions.Add(prefix
                    ? sequence.GetComposition(0, i)
                    : sequence.GetComposition(i, sequence.Count));
            }

            if (!prefix)
            {
                compositions.Reverse();
            }

            return compositions;
        }

        /// <summary>
        /// Get an product ion type from an ion type factory.
        /// </summary>
        /// <param name="ionTypeFactory">IonTypeFactory to get flyweight ion types.</param>
        /// <param name="baseIonType">The base ion type.</param>
        /// <param name="neutralLoss">The neutral loss of ion type.</param>
        /// <param name="charge">The charge of product ion type.</param>
        /// <returns>Ion type with given base ion type, neutral loss, and charge.</returns>
        public static IonType GetIonType(
                                         IonTypeFactory ionTypeFactory,
                                         BaseIonType baseIonType,
                                         NeutralLoss neutralLoss,
                                         int charge)
        {
            var chargeStr = string.Empty;
            if (charge > 1)
            {
                chargeStr = charge.ToString(CultureInfo.InvariantCulture);
            }

            var name = string.Format("{0}{1}{2}", baseIonType.Symbol, chargeStr, neutralLoss.Name);
            return ionTypeFactory.GetIonType(name);
        }

        /// <summary>
        /// Given a charge state, get the number of neighboring charge stats above and below.
        /// </summary>
        /// <param name="charge">The charge state.</param>
        /// <returns>Number of neighboring charge states.</returns>
        public static int GetNumNeighboringChargeStates(int charge)
        {
            int chargeStates = 1;
            if (charge >= 2 && charge <= 4)
            {
                chargeStates = 1;
            }
            else if (charge >= 5 && charge <= 10)
            {
                chargeStates = 2;
            }
            else if (charge >= 11 && charge <= 20)
            {
                chargeStates = 3;
            }
            else if (charge >= 21 && charge <= 30)
            {
                chargeStates = 4;
            }
            else if (charge >= 31)
            {
                chargeStates = 5;
            }

            return chargeStates;
        }

        /// <summary>
        /// Get all ion types of all combinations of base ion types, neutral losses, and charge range.
        /// </summary>
        /// <param name="ionTypeFactory">IonTypeFactory to get flyweight ion types from.</param>
        /// <param name="baseIonTypes">The base ion types.</param>
        /// <param name="neutralLosses">The neutral losses.</param>
        /// <param name="minCharge">Lowest charge of charge range.</param>
        /// <param name="maxCharge">Highest charge of charge range.</param>
        /// <returns>List of ion types.</returns>
        public static List<IonType> GetIonTypes(
                                                IonTypeFactory ionTypeFactory,
                                                IList<BaseIonType> baseIonTypes,
                                                IList<NeutralLoss> neutralLosses,
                                                int minCharge,
                                                int maxCharge)
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

        /// <summary>
        /// Get precursor ion.
        /// </summary>
        /// <param name="sequence">Sequence for precursor ion.</param>
        /// <param name="charge">Charge state of precursor ion.</param>
        /// <returns>The precursor ion for the sequence and charge state..</returns>
        public static Ion GetPrecursorIon(Sequence sequence, int charge)
        {
            var composition = sequence.Aggregate(Composition.H2O, (current, aa) => current + aa.Composition);
            return new Ion(composition, charge);
        }

        /// <summary>
        /// Get precursor ion label.
        /// </summary>
        /// <param name="sequence">Sequence for precursor ion.</param>
        /// <param name="charge">Charge state of precursor ion.</param>
        /// <param name="isotopeIndex">Isotope index of ion.</param>
        /// <returns>LabeledIonViewModel of isotope of precursor ion.</returns>
        public static LabeledIonViewModel GetLabeledPrecursorIon(Sequence sequence, int charge, int isotopeIndex = 0)
        {
            #pragma warning disable 0618
            var precursorIonType = new IonType("Precursor", Composition.H2O, charge, false);
            #pragma warning restore 0618
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            return new LabeledIonViewModel(composition, precursorIonType, false, null, null, false, isotopeIndex);
        }

        /// <summary>
        /// Get fragment ion labels.
        /// </summary>
        /// <param name="sequence">Sequence to calculate fragment ions from.</param>
        /// <param name="charge">Charge state of precursor ion.</param>
        /// <param name="ionTypes">Ion types to get ions for.</param>
        /// <returns>List of fragment ion labels.</returns>
        public static List<LabeledIonViewModel> GetFragmentIonLabels(Sequence sequence, int charge, IList<IonType> ionTypes)
        {
            var ions = new List<LabeledIonViewModel>();
            foreach (var ionType in ionTypes)
            {
                var compositions = GetCompositions(sequence, ionType.IsPrefixIon);
                ions.AddRange(compositions.Select((t, i) => new LabeledIonViewModel(t, ionType, true, null, GetPrecursorIon(sequence, charge), false, i + 1)));
            }

            return ions;
        }

        /// <summary>
        /// Get precursor ion labels.
        /// </summary>
        /// <param name="sequence">Sequence for precursor ion.</param>
        /// <param name="charge">Charge state of precursor ion.</param>
        /// <param name="minIsotopeIndex">Lowest isotope index of precursor ion.</param>
        /// <param name="maxIsotopeIndex">Maximum isotope index of precursor ion.</param>
        /// <returns>List of labeledIonViewModel of isotopes of precursor ion.</returns>
        public static List<LabeledIonViewModel> GetPrecursorIonLabels(Sequence sequence, int charge, int minIsotopeIndex, int maxIsotopeIndex)
        {
            var ions = new List<LabeledIonViewModel>();
            for (int i = minIsotopeIndex; i <= maxIsotopeIndex; i++)
            {
                ions.Add(GetLabeledPrecursorIon(sequence, charge, i));
            }

            return ions;
        }

        /// <summary>
        /// Get a set of isotope peaks from a MS/MS spectrum for a certain ion.
        /// </summary>
        /// <param name="ion">The ion to find isotope peaks for.</param>
        /// <param name="spectrum">The spectrum to extract peaks from.</param>
        /// <param name="tolerance">The ppm tolerance to use when finding peaks.</param>
        /// <param name="decharged">Has this ion been de charged?</param>
        /// <returns>
        /// Tuple containing array of isotope peaks and their Pearson correlation with the theoretical isotope envelope for this ion.
        /// </returns>
        public static Tuple<Peak[], double> GetIonPeaks(Ion ion, Spectrum spectrum, Tolerance tolerance, bool decharged = false)
        {
            var ionCorrelation = spectrum.GetCorrScore(ion, tolerance);
            var isotopePeaks = spectrum.GetAllIsotopePeaks(ion, tolerance, 0.1);
            return new Tuple<Peak[], double>(isotopePeaks, ionCorrelation);
        }

        /// <summary>
        /// Get a deconvoluted ion given a regular ion.
        /// </summary>
        /// <param name="labeledIon">The ion.</param>
        /// <param name="ionTypeFactory">A Deconvoluted IonTypeFactory</param>
        /// <returns>A deconvoluted ion.</returns>
        public static LabeledIonViewModel GetDeconvolutedLabeledIon(LabeledIonViewModel labeledIon, IonTypeFactory ionTypeFactory)
        {
            var deconIonTypeName = labeledIon.IonType.Name.Insert(1, "'");
            var ionType = ionTypeFactory.GetIonType(deconIonTypeName);
            var deconLabeledIon = new LabeledIonViewModel(
                                                          labeledIon.Composition,
                                                          ionType,
                                                          labeledIon.IsFragmentIon,
                                                          null,
                                                          labeledIon.PrecursorIon,
                                                          labeledIon.IsChargeState,
                                                          labeledIon.Index);
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
            var ppmErrors = new double?[isotopes.Max(i => i.Index) + 1];
            foreach (Isotope isotope in isotopes)
            {
                var isotopeIndex = isotope.Index;
                if (peaks[isotopeIndex] == null)
                {
                    ppmErrors[isotopeIndex] = null;
                }
                else
                {
                    ppmErrors[isotopeIndex] = GetPeakPpmError(peaks[isotopeIndex], ion.GetIsotopeMz(isotopeIndex));
                }
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

        /// <summary>
        /// Get the score for a protein or peptide sequence.
        /// </summary>
        /// <param name="scorer">The scorer.</param>
        /// <param name="sequence">The sequence.</param>
        /// <returns>The score calculated from the sequence.</returns>
        public static double ScoreSequence(IScorer scorer, Sequence sequence)
        {
            var prefixCompositions = GetCompositions(sequence, true);
            var suffixCompositions = GetCompositions(sequence, false);
            suffixCompositions.Reverse();

            double score = 0.0;
            for (int i = 0; i < prefixCompositions.Count; i++)
            {
                score += scorer.GetFragmentScore(prefixCompositions[i], suffixCompositions[i]);
            }

            return score;
        }

        /// <summary>
        /// Get a smoothed XIC.
        /// </summary>
        /// <param name="smoother">Smoother to smooth XIC.</param>
        /// <param name="xic">XIC to smooth.</param>
        /// <returns>Array of smoothed XIC points.</returns>
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

        /// <summary>
        /// Get a heavy sequence given a sequence and list of heavy peptide modifications.
        /// </summary>
        /// <param name="sequence">The sequence to convert to heavy sequence.</param>
        /// <param name="mods">The heavy peptide modifications.</param>
        /// <returns>Sequence with heavy peptide modifications.</returns>
        public static Sequence GetHeavySequence(Sequence sequence, SearchModification[] mods)
        {
            sequence = new Sequence(sequence);
            if (sequence.Count == 0)
            {
                return sequence;
            }

            foreach (var mod in mods)
            {
                if (mod.Location == SequenceLocation.PeptideNTerm || mod.Location == SequenceLocation.ProteinNTerm)
                {
                    sequence[0] = new ModifiedAminoAcid(sequence[0], mod.Modification);
                }
                else if (mod.Location == SequenceLocation.PeptideCTerm || mod.Location == SequenceLocation.ProteinCTerm)
                {
                    sequence[sequence.Count - 1] = new ModifiedAminoAcid(sequence[sequence.Count - 1], mod.Modification);
                }
                else
                {
                    for (int i = 0; i < sequence.Count; i++)
                    {
                        if (sequence[i].Residue == mod.TargetResidue)
                        {
                            sequence[i] = new ModifiedAminoAcid(sequence[i], mod.Modification);
                        }
                    }
                }
            }

            return sequence;
        }

        /// <summary>
        /// Calculate precursor mass-to-charge ratio of sequence.
        /// </summary>
        /// <param name="sequence">Sequence to calculate mass from.</param>
        /// <param name="charge">Charge state.</param>
        /// <returns>Mass-to-charge ratio of precursor ion for the sequence.</returns>
        public static double GetPrecursorMz(Sequence sequence, int charge)
        {
            if (sequence.Count == 0)
            {
                return 0.0;
            }

            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var ion = new Ion(composition + Composition.H2O, charge);
            return Math.Round(ion.GetMostAbundantIsotopeMz(), 2);
        }
    }
}
