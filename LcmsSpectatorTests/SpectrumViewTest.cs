using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Readers;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;
using OxyPlot.Axes;

namespace LcmsSpectatorTests
{
    [TestFixture]
    public class SpectrumViewTest
    {
       [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
                 @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        public void TestDisplaySpectra(string rawFile, string idFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(idFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
            var id = ids.GetHighestScoringPrSm();

            // init XicPlotViewModel
            var dialogService = new TestableMainDialogService();
            var spectrumViewModel = new SpectrumViewModel(dialogService, lcms);

            // init test ions
            var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
            var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
            const int charge = 1;
            const int minCharge = 1, maxCharge = 2;
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
            var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
            var ionVms = ions.Select(label => new LabeledIonViewModel(label.Composition, label.IonType, label.IsFragmentIon, lcms, label.PrecursorIon, label.IsChargeState, label.Index)).ToList();

        }
    }
}
