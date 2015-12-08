using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectatorTests
{
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Database;
    using InformedProteomics.Backend.MassSpecData;
    using InformedProteomics.TopDown.Execution;

    using NUnit.Framework;

    [TestFixture]
    public class MsPathFinderTest
    {
        [Test]
        public void TestIanCidData()
        {
            const string specFilePath =
                @"\\protoapps\userdata\Wilkins\UIMF Files\9 pep mix 365 mTorr with ims 45V CID\9 pep mix 365 mTorr with ims 45V CID.UIMF";
            const string fastaFilePath = @"\\protoapps\userdata\Wilkins\UIMF Files\melittin.fasta";
            const string outputDirectory =
                @"\\protoapps\userdata\Wilkins\UIMF Files\9 pep mix 365 mTorr with ims 45V CID";

            const double correlationThreshold = 0.7;

            // Add missing modifications

            // Initialize search modifications
            var searchModifications = new List<SearchModification> { };
            var aminoAcidSet = new AminoAcidSet(searchModifications, 1);
            
            // Initialize spectrum file
            var lcmsRun = PbfLcMsRun.GetLcMsRun(specFilePath);
            var scanNumbers = lcmsRun.GetScanNumbers(2);

            // Initialize MSPathFinder
            var launcher = new IcTopDownLauncher(
                specFilePath,
                fastaFilePath,
                outputDirectory,
                aminoAcidSet)
            {
                MinSequenceLength = 1,
                MaxSequenceLength = 100,
                MaxNumNTermCleavages = 1,
                MaxNumCTermCleavages = 0,
                MinPrecursorIonCharge = 1,
                MaxPrecursorIonCharge = 20,
                MinProductIonCharge = 1,
                MaxProductIonCharge = 20,
                MinSequenceMass = 1,
                MaxSequenceMass = 30000,
                PrecursorIonTolerancePpm = 100,
                ProductIonTolerancePpm = 100,
                RunTargetDecoyAnalysis = DatabaseSearchMode.Both,
                SearchMode = InternalCleavageType.NoInternalCleavage,
                MaxNumThreads = 4,
                ScanNumbers = scanNumbers,
                NumMatchesPerSpectrum = 1,
                TagBasedSearch = false,
            };

            launcher.RunSearch(correlationThreshold);
        }
    }
}
