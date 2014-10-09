using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.PlotModels;
using LcmsSpectator.TaskServices;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorModels.Utils;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectatorTests
{
    [TestFixture]
    public class XicPlotTest
    {
        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
            @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        public void TestDisplayXic(string rawFile, string idFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(idFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
            var id = ids.GetHighestScoringPrSm();

            // init XicPlotViewModel
            SelectedPrSmViewModel.Instance.Charge = 2;
            var dialogService = new TestableMainDialogService();
            var xicPlotViewModel = new XicPlotViewModel(dialogService, new MockTaskService(), "", new LinearAxis(),
                false)
            {
                Lcms = lcms
            };

            // init test ions
            var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
            var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
            const int charge = 1;
            const int minCharge = 1, maxCharge = 2;
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
            var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
            var ionVms = new List<LabeledIonViewModel>();
            foreach (var label in ions) ionVms.Add(new LabeledIonViewModel(label));
            xicPlotViewModel.Ions = ionVms;

            Assert.True(xicPlotViewModel.Plot != null);
            Assert.True(xicPlotViewModel.Plot.Series.Count == ionVms.Count+1);
        }

        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
            @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        public void ToggleXicVisibility(string rawFile, string idFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(idFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
            var id = ids.GetHighestScoringPrSm();

            // init XicPlotViewModel
            SelectedPrSmViewModel.Instance.Charge = 2;
            var dialogService = new TestableMainDialogService();
            var xicPlotViewModel = new XicPlotViewModel(dialogService, new MockTaskService(), "", new LinearAxis(),
                false)
            {
                Lcms = lcms
            };

            // init test ions
            var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
            var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
            const int charge = 1;
            const int minCharge = 1, maxCharge = 2;
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
            var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
            var ionVms = new List<LabeledIonViewModel>();
            foreach (var label in ions) ionVms.Add(new LabeledIonViewModel(label));
            xicPlotViewModel.Ions = ionVms;

            foreach (var ionVm in ionVms)
            {
                ionVm.Selected = false;
                bool foundSeries = false;
                foreach (var series in xicPlotViewModel.Plot.Series)
                {
                    var xicSeries = series as XicSeries;
                    if (xicSeries == null) continue;
                    if (xicSeries.Label == ionVm.LabeledIon.Label)
                    {
                        foundSeries = true;
                        Assert.True(xicSeries.IsVisible == ionVm.Selected);
                    }
                }

                Assert.True(foundSeries);
            }
        }
    }
}
