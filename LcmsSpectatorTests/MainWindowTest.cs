namespace LcmsSpectatorTests
{
    using System.IO;
    using InformedProteomics.Backend.MassSpecData;
    using InformedProteomics.Backend.Results;

    using LcmsSpectator.Models;
    using LcmsSpectator.Readers;
    using LcmsSpectator.ViewModels;
    using LcmsSpectatorTests.DialogServices;
    using NUnit.Framework;

    using PSI_Interface.IdentData;

    [TestFixture]
    public class MainWindowTest
    {
        [Test]
        public void TestRegisterModification()
        {
            
        }

        [Test]
        public void TestUnregisterModification()
        {
            
        }

        [Test]
        public void TestUpdateModification()
        {
            
        }

        [Test]
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
