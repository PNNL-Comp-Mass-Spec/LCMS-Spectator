using InformedProteomics.Backend.SearchResults;

namespace LcmsSpectatorTests
{

    using NUnit.Framework;

    using PSI_Interface.IdentData;

    [TestFixture]
    public class MainWindowTest
    {
        [Test]
        [Ignore("Undefined")]
        public void TestRegisterModification()
        {

        }

        [Test]
        [Ignore("Undefined")]
        public void TestUnregisterModification()
        {

        }

        [Test]
        [Ignore("Undefined")]
        public void TestUpdateModification()
        {

        }

        [Test]
        [Ignore("Missing data file")]
        public void TestFileReading()
        {
            var mzIdReader = new SimpleMZIdentMLReader();
            var results = mzIdReader.Read(@"C:\Users\wilk011\Documents\mspf_test\er\2016-12-27_Ecoli_Ribosome_1.mzid");

            foreach (var id in results.Identifications)
            {
                var sequence = id.Peptide.GetIpSequence();
            }
        }
    }
}
