using System;
using System.Collections.Generic;
using System.IO;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectatorModels.Models
{
    public class RtLcMsRun: LcMsRun
    {
        public RtLcMsRun(string filePath, IMassSpecDataReader massSpecDataReader, double precursorSignalToNoiseRatioThreshold, double productSignalToNoiseRatioThreshold): 
                            base(massSpecDataReader, precursorSignalToNoiseRatioThreshold, productSignalToNoiseRatioThreshold)
        {
            var xcal = new XCaliburReader(filePath);
            _scanToRtMap = new Dictionary<int, double> { {0, 0.0} };
            var scanList = new List<int>();
            scanList.AddRange(GetScanNumbers(1));
            scanList.AddRange(GetScanNumbers(2));
            foreach (var scan in scanList)
            {
                _scanToRtMap.Add(scan, xcal.RtFromScanNum(scan));                
            }
        }

        public static RtLcMsRun GetRtLcMsRun(string filePath, MassSpecDataType dataType, double precursorSignalToNoiseRatioThreshold, double productSignalToNoiseRatioThreshold)
        {
            var pbfFilePath = Path.ChangeExtension(filePath, ".pbf");

            if (!File.Exists(pbfFilePath) || !PbfReader.CheckFileFormatVersion(pbfFilePath))
            {
                RtLcMsRun run = dataType == MassSpecDataType.XCaliburRun ? new RtLcMsRun(filePath, new XCaliburReader(filePath), 0, 0) : null;
                if (run != null) run.WriteTo(pbfFilePath);
                else throw new Exception("Unsupported raw file format!");
            }

            return new RtLcMsRun(filePath, new PbfReader(pbfFilePath), precursorSignalToNoiseRatioThreshold, productSignalToNoiseRatioThreshold);
        }

        public double MinRetentionTime { get { return _scanToRtMap[MinLcScan]; } }
        public double MaxRetentionTime { get { return _scanToRtMap[MaxLcScan];  } }
        
        public double GetRetentionTime(int scanNum)
        {
            return _scanToRtMap[scanNum]; 
        }

        private readonly Dictionary<int, double> _scanToRtMap;
    }
}
