using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Data;
using Splat;

namespace LcmsSpectator.Models
{
    public class FragmentationSequence
    {
        private const bool USE_THREADING = true;

        /// <summary>
        /// Lock for thread-safe access to fragmentIonResultCache
        /// </summary>
        private readonly object fragmentIonResultCacheLock = new object();

        /// <summary>
        /// Cache for previously calculated fragment ions for a particular set of ion types and fixed modifications
        /// </summary>
        private readonly MemoizingMRUCache<FragmentLabelGenerationParameters, FragmentLabelResultData> fragmentIonResultCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="FragmentationSequence"/> class.
        /// </summary>
        /// <param name="sequence">The underlying sequence.</param>
        /// <param name="charge">Charge of sequence.</param>
        /// <param name="lcms">The LCMSRun for the data set.</param>
        /// <param name="activationMethod">The Activation Method.</param>
        public FragmentationSequence(Sequence sequence, int charge, ILcMsRun lcms, ActivationMethod activationMethod)
        {
            Sequence = sequence;
            Charge = charge;
            LcMsRun = lcms;
            ActivationMethod = activationMethod;

            fragmentIonResultCache = new MemoizingMRUCache<FragmentLabelGenerationParameters, FragmentLabelResultData>(GetFragmentLabelResultData, 6);
        }

        /// <summary>
        /// Gets the underlying sequence.
        /// </summary>
        public Sequence Sequence { get; }

        /// <summary>
        /// Gets the charge.
        /// </summary>
        public int Charge { get; }

        /// <summary>
        /// Gets the LCMSRun for the data set.
        /// </summary>
        public ILcMsRun LcMsRun { get; }

        /// <summary>
        /// Gets the Activation Method.
        /// </summary>
        public ActivationMethod ActivationMethod { get; }

        /// <summary>
        /// Get fragment ion labels.
        /// </summary>
        /// <param name="ionTypes">List of IonTypes.</param>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of fragment labeled ions.</returns>
        public Task<List<LabeledIonViewModel>> GetFragmentLabelsAsync(IList<IonType> ionTypes, SearchModification[] labelModifications = null)
        {
            return Task.Run(() => GetFragmentLabels(ionTypes, labelModifications));
        }

        /// <summary>
        /// Get isotope ion labels for precursor.
        /// </summary>
        /// <param name="relativeIntensityThreshold">Relative intensity threshold (fraction of most abundant isotope)</param>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of precursor labeled ions.</returns>
        public Task<List<LabeledIonViewModel>> GetIsotopePrecursorLabelsAsync(
            double relativeIntensityThreshold = 0.1,
            IEnumerable<SearchModification> labelModifications = null)
        {
            return Task.Run(() => GetIsotopePrecursorLabels(relativeIntensityThreshold, labelModifications));
        }

        /// <summary>
        /// Get neighboring charge state ion labels for precursor.
        /// </summary>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of neighboring charge state labeled ions.</returns>
        public Task<List<LabeledIonViewModel>> GetChargePrecursorLabelsAsync(
            IEnumerable<SearchModification> labelModifications = null)
        {
            return Task.Run(() => GetChargePrecursorLabels(labelModifications));
        }

        /// <summary>
        /// Calculate fragment ion labels.
        /// </summary>
        /// <param name="ionTypes">List of IonTypes.</param>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of fragment labeled ions.</returns>
        public List<LabeledIonViewModel> GetFragmentLabels(IList<IonType> ionTypes, SearchModification[] labelModifications = null)
        {
            if (Sequence.Count < 1 || LcMsRun == null)
            {
                return new List<LabeledIonViewModel>(0);
            }

            FragmentLabelResultData resultCache;
            // Avoid duplication of computation effort: check for existing results with the same parameters
            lock (fragmentIonResultCacheLock)
            {
                var key = new FragmentLabelGenerationParameters(ionTypes, labelModifications);
                resultCache = fragmentIonResultCache.Get(key);
            }

            // Only allow one thread to compute/access results at a time
            // This effectively allows the first thread to run the whole computation while blocking other threads until computation is done.
            lock (resultCache.ComputeLock)
            {
                // Non-first thread: By the time other threads access this property, the results should be available
                if (resultCache.ResultsComputed)
                {
                    // Return a copy of the results (limiting ability to affect the original results)
                    return resultCache.Results.ToList();
                }

                // First thread: run all of the computations for this set of parameters
                var fragmentListLock = new object();

                // This holds all of the theoretical b and y ions (or c and z ions for HCD)
                var fragmentLabelList = resultCache.Results;

                var sequence = labelModifications == null ? Sequence : IonUtils.GetHeavySequence(Sequence, labelModifications);

                var precursorIon = IonUtils.GetPrecursorIon(sequence, Charge);

                var parallelOptions = new ParallelOptions();

                var useThreading = USE_THREADING;

                // ReSharper disable ConditionIsAlwaysTrueOrFalse
                // ReSharper disable HeuristicUnreachableCode
                if (!useThreading)
                {
                    parallelOptions.MaxDegreeOfParallelism = 1;
                    GetIonFragmentsNonThreaded(precursorIon, sequence, ionTypes, fragmentLabelList);
                }
                else
                {
                    // Automatically break up the ionTypes into groups and process in parallel
                    Parallel.ForEach(ionTypes, parallelOptions, ionType =>
                    {
                        var ionFragments = new List<LabeledIonViewModel>();
                        for (var i = 1; i < Sequence.Count; i++)
                        {
                            var startIndex = ionType.IsPrefixIon ? 0 : i;
                            var length = ionType.IsPrefixIon ? i : sequence.Count - i;
                            var fragment = new Sequence(Sequence.GetRange(startIndex, length));
                            foreach (var ion in ionType.GetPossibleIons(fragment))
                            {
                                var labeledIonViewModel = new LabeledIonViewModel(ion.Composition, ionType, true, LcMsRun)
                                {
                                    Index = length,
                                    PrecursorIon = precursorIon
                                };

                                ionFragments.Add(labeledIonViewModel);
                            }

                            if (!ionType.IsPrefixIon)
                            {
                                ionFragments.Reverse();
                            }
                        }

                        // Only allow one thread at a time to write to the output list.
                        lock (fragmentListLock)
                        {
                            fragmentLabelList.AddRange(ionFragments);
                        }
                    });
                }
                // ReSharper restore HeuristicUnreachableCode
                // ReSharper restore ConditionIsAlwaysTrueOrFalse

                // Flag computation as complete
                resultCache.ResultsComputed = true;

                // Return a copy of the results (limiting ability to affect the original results)
                return fragmentLabelList.ToList();
            }
        }

        /// <summary>
        /// Construct the theoretical list of b and y ions
        /// </summary>
        /// <param name="precursorIon"></param>
        /// <param name="sequence"></param>
        /// <param name="ionTypes"></param>
        /// <param name="fragmentLabelList"></param>
        void GetIonFragmentsNonThreaded(
            InformedProteomics.Backend.Data.Biology.Ion precursorIon,
            Sequence sequence,
            IEnumerable<IonType> ionTypes,
            List<LabeledIonViewModel> fragmentLabelList)
        {
            foreach (var ionType in ionTypes)
            {
                var ionFragments = new List<LabeledIonViewModel>();
                for (var i = 1; i < Sequence.Count; i++)
                {
                    var startIndex = ionType.IsPrefixIon ? 0 : i;
                    var length = ionType.IsPrefixIon ? i : sequence.Count - i;
                    var fragment = new Sequence(Sequence.GetRange(startIndex, length));
                    foreach (var ion in ionType.GetPossibleIons(fragment))
                    {
                        var labeledIonViewModel = new LabeledIonViewModel(ion.Composition, ionType, true, LcMsRun)
                        {
                            Index = length,
                            PrecursorIon = precursorIon
                        };

                        ionFragments.Add(labeledIonViewModel);
                    }

                    if (!ionType.IsPrefixIon)
                    {
                        ionFragments.Reverse();
                    }
                }

                fragmentLabelList.AddRange(ionFragments);
            }
        }

        /// <summary>
        /// Calculate isotope ion labels for precursor.
        /// </summary>
        /// <param name="relativeIntensityThreshold">Relative intensity threshold (fraction of most abundant isotope)</param>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of precursor labeled ions.</returns>
        public List<LabeledIonViewModel> GetIsotopePrecursorLabels(double relativeIntensityThreshold = 0.1, IEnumerable<SearchModification> labelModifications = null)
        {
            var ions = new List<LabeledIonViewModel>();
            if (Sequence.Count == 0 || LcMsRun == null)
            {
                return ions;
            }

            var sequence = Sequence;
            if (labelModifications != null)
            {
                sequence = IonUtils.GetHeavySequence(sequence, labelModifications.ToArray());
            }

#pragma warning disable 0618
            var precursorIonType = new IonType("Precursor", Composition.H2O, Charge, false);
#pragma warning restore 0618
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var relativeIntensities = composition.GetIsotopomerEnvelope();
            var indices = new List<int> { -1 };
            for (var i = 0; i < relativeIntensities.Envelope.Length; i++)
            {
                if (relativeIntensities.Envelope[i] >= relativeIntensityThreshold || i == 0)
                {
                    indices.Add(i);
                }
            }

            ions.AddRange(indices.Select(index => new LabeledIonViewModel(composition, precursorIonType, false, LcMsRun, null, false, index)));
            return ions;
        }

        /// <summary>
        /// Calculate neighboring charge state ion labels for precursor.
        /// </summary>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of neighboring charge state labeled ions.</returns>
        public List<LabeledIonViewModel> GetChargePrecursorLabels(IEnumerable<SearchModification> labelModifications = null)
        {
            var ions = new List<LabeledIonViewModel>();
            var numChargeStates = IonUtils.GetNumNeighboringChargeStates(Charge);
            if (Sequence.Count == 0 || LcMsRun == null)
            {
                return ions;
            }

            var sequence = Sequence;
            if (labelModifications != null)
            {
                sequence = IonUtils.GetHeavySequence(sequence, labelModifications.ToArray());
            }

            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var minCharge = Math.Max(1, Charge - numChargeStates);
            var maxCharge = Charge + numChargeStates;

            for (var i = minCharge; i <= maxCharge; i++)
            {
                var index = i - minCharge;
                if (index == 0)
                {
                    index = Charge - minCharge;
                }

                if (i == Charge)
                {
                    index = 0;         // guarantee that actual charge is index 0
                }

#pragma warning disable 0618
                var precursorIonType = new IonType("Precursor", Composition.H2O, i, false);
#pragma warning restore 0618
                ions.Add(new LabeledIonViewModel(composition, precursorIonType, false, LcMsRun, null, true, index));
            }

            return ions;
        }

        /// <summary>
        /// Get a new <see cref="FragmentLabelResultData"/> object for the specified parameters
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private FragmentLabelResultData GetFragmentLabelResultData(FragmentLabelGenerationParameters parameters, object context)
        {
            return new FragmentLabelResultData(new List<LabeledIonViewModel>(Sequence.Count * parameters.IonTypes.Count * Charge));
        }

        #region Internal Classes for results caching

        /// <summary>
        /// Parameter class, with additions to perform proper checking for equality of the collections and their contents.
        /// </summary>
        private class FragmentLabelGenerationParameters : IEquatable<FragmentLabelGenerationParameters>
        {
            public IReadOnlyList<IonType> IonTypes { get; }
            private IReadOnlyList<SearchModification> LabelModifications { get; }

            public FragmentLabelGenerationParameters(IEnumerable<IonType> ionTypes, IEnumerable<SearchModification> labelModifications)
            {
                IonTypes = ionTypes.ToList();
                LabelModifications = labelModifications.ToList();
            }

            public bool Equals(FragmentLabelGenerationParameters other)
            {
                if (other == null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(IonTypes, other.IonTypes) && Equals(LabelModifications, other.LabelModifications);
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((FragmentLabelGenerationParameters)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((IonTypes != null ? GetHashCode(IonTypes) : 0) * 397) ^ (LabelModifications != null ? GetHashCode(LabelModifications) : 0);
                }
            }

            #region Collection Equality/HashCodes

            private bool Equals(IEnumerable<IonType> x, IEnumerable<IonType> y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return x.OrderBy(GetHashCode).SequenceEqual(y.OrderBy(GetHashCode));
            }

            private int GetHashCode(IEnumerable<IonType> obj)
            {
                unchecked
                {
                    var hashCode = 0;
                    foreach (var o in obj)
                    {
                        hashCode = (hashCode * 397) ^ GetHashCode(o);
                    }
                    return hashCode;
                }
            }

            private bool Equals(IEnumerable<SearchModification> x, IEnumerable<SearchModification> y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return x.OrderBy(GetHashCode).SequenceEqual(y.OrderBy(GetHashCode));
            }

            private int GetHashCode(IEnumerable<SearchModification> obj)
            {
                unchecked
                {
                    var hashCode = 0;
                    foreach (var o in obj)
                    {
                        hashCode = (hashCode * 397) ^ GetHashCode(o);
                    }
                    return hashCode;
                }
            }

            #endregion

            #region IonType Equality/HashCode

            [ObsoleteAttribute("Unused")]
            private bool Equals(IonType x, IonType y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }
                return string.Equals(x.Name, y.Name) && Equals(x.OffsetComposition, y.OffsetComposition) && x.Charge == y.Charge && x.IsPrefixIon == y.IsPrefixIon && Equals(x.BaseIonType, y.BaseIonType) && Equals(x.NeutralLoss, y.NeutralLoss);
            }

            private int GetHashCode(IonType obj)
            {
                if (obj == null)
                {
                    return 0;
                }
                unchecked
                {
                    var hashCode = (obj.Name != null ? obj.Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.OffsetComposition != null ? obj.OffsetComposition.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ obj.Charge;
                    hashCode = (hashCode * 397) ^ obj.IsPrefixIon.GetHashCode();
                    hashCode = (hashCode * 397) ^ (obj.BaseIonType != null ? obj.BaseIonType.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.NeutralLoss != null ? obj.NeutralLoss.GetHashCode() : 0);
                    return hashCode;
                }
            }

            #endregion

            #region IonType Equality/HashCode

            [ObsoleteAttribute("Unused")]
            private bool Equals(SearchModification x, SearchModification y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }
                return Equals(x.Modification, y.Modification) && x.TargetResidue == y.TargetResidue && x.Location == y.Location && x.IsFixedModification == y.IsFixedModification;
            }

            private int GetHashCode(SearchModification obj)
            {
                if (obj == null)
                {
                    return 0;
                }
                unchecked
                {
                    var hashCode = (obj.Modification != null ? GetHashCode(obj.Modification) : 0);
                    hashCode = (hashCode * 397) ^ obj.TargetResidue.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)obj.Location;
                    hashCode = (hashCode * 397) ^ obj.IsFixedModification.GetHashCode();
                    return hashCode;
                }
            }

            private bool Equals(Modification x, Modification y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }
                return Equals(x.Name, y.Name) && x.AccessionNum == y.AccessionNum && Equals(x.Composition, y.Composition);
            }

            private int GetHashCode(Modification obj)
            {
                if (obj == null)
                {
                    return 0;
                }
                unchecked
                {
                    var hashCode = (obj.Name != null ? obj.Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ obj.AccessionNum;
                    hashCode = (hashCode * 397) ^ obj.Composition.GetHashCode();
                    return hashCode;
                }
            }

            #endregion
        }

        /// <summary>
        /// Results container
        /// </summary>
        private class FragmentLabelResultData
        {
            /// <summary>
            /// List of results
            /// </summary>
            public List<LabeledIonViewModel> Results { get; }

            /// <summary>
            /// Lock object to only allow one thread at a time to determine if computation needs to occur.
            /// </summary>
            public object ComputeLock { get; } = new object();

            /// <summary>
            /// If the results have already been computed.
            /// </summary>
            public bool ResultsComputed { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="results">List for results, allows capacity to be pre-set.</param>
            public FragmentLabelResultData(List<LabeledIonViewModel> results)
            {
                Results = results;
            }
        }

        #endregion
    }
}
