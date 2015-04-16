using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.Models;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Readers;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;
using OxyPlot;
using OxyPlot.Axes;
using ReactiveUI;

namespace LcmsSpectatorTests
{
    [TestFixture]
    public class XicPlotTest
    {
        //[TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
        //    @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        //public void TestDisplayXic(string rawFile, string idFile)
        //{
        //    // init
        //    var idFileReader = IdFileReaderFactory.CreateReader(idFile);
        //    var ids = idFileReader.Read();
        //    var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
        //    ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
        //    var id = ids.GetHighestScoringPrSm();

        //    // init XicPlotViewModel
        //    var dialogService = new TestableMainDialogService();
        //    var xicPlotViewModel = new XicPlotViewModel(dialogService, lcms, "", new LinearAxis(),
        //        false);

        //    // init test ions
        //    var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
        //    var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
        //    const int charge = 1;
        //    const int minCharge = 1, maxCharge = 2;
        //    var ionTypeFactory = new IonTypeFactory(maxCharge);
        //    var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
        //    var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
        //    var ionVms = new ReactiveList<LabeledIonViewModel>(ions.Select(label => new LabeledIonViewModel(label.Composition, label.Index, label.IonType, label.IsFragmentIon, lcms, label.PrecursorIon, label.IsChargeState)));
        //    xicPlotViewModel.Ions = ionVms;

        //    Assert.True(xicPlotViewModel.PlotModel != null);
        //    Assert.True(xicPlotViewModel.PlotModel.Series.Count == ionVms.Count+1);

        //    // Create xics
        //    var xicMap = new Dictionary<string, LabeledXic>();
        //    foreach (var ionVm in ionVms)
        //    {
        //        var ion = ionVm.Ion;
        //        Xic xic;
        //        if (ionVm.IsFragmentIon) xic = lcms.GetFullProductExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(),
        //                                                                                    IcParameters.Instance.ProductIonTolerancePpm,
        //                                                                                    ionVm.PrecursorIon.GetMostAbundantIsotopeMz());
        //        else xic = lcms.GetFullPrecursorIonExtractedIonChromatogram(ion.GetIsotopeMz(ionVm.Index), IcParameters.Instance.PrecursorTolerancePpm);
        //        xicMap.Add(ionVm.Label, new LabeledXic(ionVm.Composition, ionVm.Index,  xic.ToArray(), ionVm.IonType, ionVm.IsFragmentIon));
        //    }

        //    // Check to see that correct XICs are showing
        //    foreach (var series in xicPlotViewModel.PlotModel.Series)
        //    {
        //        var xicSeries = series as XicPointSeries;
        //        if (xicSeries == null) continue;
        //        var xic = xicMap[xicSeries.Label].Xic;
        //        Assert.True(xic.Count == xicSeries.Points.Count);
        //        foreach (var point in xicSeries.Points)
        //        {
        //            var xicPoint = point as XicDataPoint;
        //            Assert.True(xicPoint != null);
        //            Assert.True(xicPoint.ScanNum == xic[0].ScanNum);
        //            Assert.True(xicPoint.Y.Equals(xic[0].Intensity));
        //        }
        //    }
        //}

        //[TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
        //    @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        //public void TestToggleXicVisibility(string rawFile, string idFile)
        //{
        //    // init
        //    var idFileReader = IdFileReaderFactory.CreateReader(idFile);
        //    var ids = idFileReader.Read();
        //    var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
        //    ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
        //    var id = ids.GetHighestScoringPrSm();

        //    // init XicPlotViewModel
        //    var dialogService = new TestableMainDialogService();
        //    var xicPlotViewModel = new XicPlotViewModel(dialogService, lcms, "", new LinearAxis(),
        //        false);

        //    // init test ions
        //    var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
        //    var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
        //    const int charge = 1;
        //    const int minCharge = 1, maxCharge = 2;
        //    var ionTypeFactory = new IonTypeFactory(maxCharge);
        //    var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
        //    var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
        //    var ionVms = ions.Select(label => new LabeledIonViewModel(label)).ToList();
        //    xicPlotViewModel.Ions = ionVms;

        //    foreach (var ionVm in ionVms)
        //    {
        //        ionVm.Selected = false;
        //        bool foundSeries = false;
        //        foreach (var series in xicPlotViewModel.PlotModel.Series)
        //        {
        //            var xicSeries = series as XicSeries;
        //            if (xicSeries == null) continue;
        //            if (xicSeries.Label == ionVm.LabeledIon.Label)
        //            {
        //                foundSeries = true;
        //                Assert.True(xicSeries.IsVisible == ionVm.Selected);
        //            }
        //        }
        //        Assert.True(foundSeries);
        //    }
        //}

        //[TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
        //    @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        //public void TestToggleShowScanMarkers(string rawFile, string idFile)
        //{
        //    // init
        //    var idFileReader = IdFileReaderFactory.CreateReader(idFile);
        //    var ids = idFileReader.Read();
        //    var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
        //    ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
        //    var id = ids.GetHighestScoringPrSm();

        //    // init XicPlotViewModel
        //    SelectedPrSmViewModel.Instance.Charge = 2;
        //    var dialogService = new TestableMainDialogService();
        //    var xicPlotViewModel = new XicPlotViewModel(dialogService, new MockTaskService(), "", new LinearAxis(),
        //        false)
        //    {
        //        Lcms = lcms
        //    };

        //    // init test ions
        //    var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
        //    var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
        //    const int charge = 1;
        //    const int minCharge = 1, maxCharge = 2;
        //    var ionTypeFactory = new IonTypeFactory(maxCharge);
        //    var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
        //    var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
        //    var ionVms = ions.Select(label => new LabeledIonViewModel(label)).ToList();
        //    xicPlotViewModel.Ions = ionVms;

        //    // Check to ensure all scan markers are off (initial condition)
        //    foreach (var series in xicPlotViewModel.PlotModel.Series)
        //    {
        //        var xicSeries = series as XicSeries;
        //        if (xicSeries == null) continue;
        //        Assert.True(xicSeries.MarkerType == MarkerType.None);
        //    }

        //    // toggle ShowScanMarkers on
        //    xicPlotViewModel.ShowScanMarkers = true;

        //    // Check to ensure all scan markers are on
        //    foreach (var series in xicPlotViewModel.PlotModel.Series)
        //    {
        //        var xicSeries = series as XicSeries;
        //        if (xicSeries == null) continue;
        //        Assert.True(xicSeries.MarkerType == MarkerType.Circle);
        //    }

        //    // toggle ShowScanMarkers off
        //    xicPlotViewModel.ShowScanMarkers = false;

        //    // Check to ensure all scan markers are turned back off
        //    foreach (var series in xicPlotViewModel.PlotModel.Series)
        //    {
        //        var xicSeries = series as XicSeries;
        //        if (xicSeries == null) continue;
        //        Assert.True(xicSeries.MarkerType == MarkerType.None);
        //    }
        //}

    //    [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
    //@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
    //    public void TestToggleShowLegend(string rawFile, string idFile)
    //    {
    //        // init
    //        var idFileReader = IdFileReaderFactory.CreateReader(idFile);
    //        var ids = idFileReader.Read();
    //        var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
    //        ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
    //        var id = ids.GetHighestScoringPrSm();

    //        // init XicPlotViewModel
    //        SelectedPrSmViewModel.Instance.Charge = 2;
    //        var dialogService = new TestableMainDialogService();
    //        var xicPlotViewModel = new XicPlotViewModel(dialogService, new MockTaskService(), "", new LinearAxis(), false, false)
    //        {
    //            Lcms = lcms
    //        };

    //        // init test ions
    //        var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
    //        var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
    //        const int charge = 1;
    //        const int minCharge = 1, maxCharge = 2;
    //        var ionTypeFactory = new IonTypeFactory(maxCharge);
    //        var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
    //        var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
    //        var ionVms = ions.Select(label => new LabeledIonViewModel(label)).ToList();
    //        xicPlotViewModel.Ions = ionVms;

    //        // Check to legend is off (initial condition)
    //        Assert.True(xicPlotViewModel.PlotModel.IsLegendVisible == false);

    //        // toggle ShowLegend on
    //        xicPlotViewModel.ShowLegend = true;

    //        // Check to ensure legend is visible
    //        Assert.True(xicPlotViewModel.PlotModel.IsLegendVisible);

    //        // toggle ShowLegend off
    //        xicPlotViewModel.ShowLegend = false;

    //        // Check to ensure legend is back off
    //        Assert.True(xicPlotViewModel.PlotModel.IsLegendVisible == false);
    //    }

        //[TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
        //          @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        //public void TestSelectedScanChanged(string rawFile, string idFile)
        //{
        //    // init
        //    var idFileReader = IdFileReaderFactory.CreateReader(idFile);
        //    var ids = idFileReader.Read();
        //    var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
        //    SelectedPrSmViewModel.Instance.Lcms = lcms;
        //    SelectedPrSmViewModel.Instance.Heavy = false;
        //    ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));

        //    // init XicPlotViewModel
        //    SelectedPrSmViewModel.Instance.Charge = 2;
        //    var dialogService = new TestableMainDialogService();
        //    var xicPlotViewModel = new XicPlotViewModel(dialogService, new MockTaskService(), "", new LinearAxis(), false, false)
        //    {
        //        Lcms = lcms
        //    };

        //    var scans = lcms.GetScanNumbers(2);
        //    foreach (var scan in scans)
        //    {
        //        SelectedPrSmViewModel.Instance.Scan = scan;
        //        var rt = lcms.GetElutionTime(scan);
        //        Assert.True(xicPlotViewModel.SelectedRt.Equals(rt));
        //        var marker = xicPlotViewModel.PlotModel.GetPointMarker();
        //        Assert.True(marker != null);
        //        Assert.True(marker.X.Equals(rt));
        //    }
        //}

        //[TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
        //    @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        //public void TestAreaCalculation(string rawFile, string idFile)
        //{
        //    const double tolerance = 0.01;

        //    // init
        //    var idFileReader = IdFileReaderFactory.CreateReader(idFile);
        //    var ids = idFileReader.Read();
        //    var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
        //    ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
        //    var id = ids.GetHighestScoringPrSm();

        //    // init XicPlotViewModel
        //    SelectedPrSmViewModel.Instance.Charge = 2;
        //    var dialogService = new TestableMainDialogService();
        //    var xicPlotViewModel = new XicPlotViewModel(dialogService, new MockTaskService(), "", new LinearAxis(), false, false)
        //    {
        //        Lcms = lcms,
        //        PointsToSmooth = 0
        //    };

        //    // init test ions
        //    var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
        //    var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
        //    const int charge = 1;
        //    const int minCharge = 1, maxCharge = 2;
        //    var ionTypeFactory = new IonTypeFactory(maxCharge);
        //    var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
        //    var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
        //    var ionVms = ions.Select(label => new LabeledIonViewModel(label)).ToList();
        //    xicPlotViewModel.Ions = ionVms;

        //    var xics = new List<LabeledXic>();
        //    foreach (var ionVm in ionVms)
        //    {
        //        var ion = ionVm.LabeledIon.Ion;
        //        var label = ionVm.LabeledIon;
        //        Xic xic;
        //        if (label.IsFragmentIon) xic = lcms.GetFullProductExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(),
        //                                                                                    IcParameters.Instance.ProductIonTolerancePpm,
        //                                                                                    label.PrecursorIon.GetMostAbundantIsotopeMz());
        //        else xic = lcms.GetFullPrecursorIonExtractedIonChromatogram(ion.GetIsotopeMz(label.Index), IcParameters.Instance.PrecursorTolerancePpm);
        //        xics.Add(new LabeledXic(label.Composition, label.Index, xic, label.IonType, label.IsFragmentIon));
        //    }

        //    // calculate area
        //    double area = xics.SelectMany(xic => xic.Xic).Sum(xicPoint => xicPoint.Intensity);
        //    Assert.True(Math.Abs(area - xicPlotViewModel.Area) < tolerance);
        //}

        //[TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
        //@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        //public void TestM1InAreaCalc(string rawFile, string idFile)
        //{
        //    // init
        //    var idFileReader = IdFileReaderFactory.CreateReader(idFile);
        //    var ids = idFileReader.Read();
        //    var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
        //    ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
        //    var id = ids.GetHighestScoringPrSm();

        //    // init XicPlotViewModel
        //    SelectedPrSmViewModel.Instance.Charge = 2;
        //    var dialogService = new TestableMainDialogService();
        //    var xicPlotViewModel = new XicPlotViewModel(dialogService, new MockTaskService(), "", new LinearAxis(), false, false)
        //    {
        //        Lcms = lcms,
        //        PointsToSmooth = 0
        //    };
        //    var ions = IonUtils.GetPrecursorIonLabels(id.Sequence, id.Charge, -1, 2);
        //    var ionVms = ions.Select(label => new LabeledIonViewModel(label)).ToList();
        //    xicPlotViewModel.Ions = ionVms;

        //    var area = xicPlotViewModel.GetCurrentArea();

        //    // hide M - 1 precursor
        //    ionVms[0].Selected = false;

        //    var area1 = xicPlotViewModel.GetCurrentArea();

        //    // areas should be the same if M-1 isn't used in area calculation
        //    Assert.True(area.Equals(area1));
        //}
    }
}
