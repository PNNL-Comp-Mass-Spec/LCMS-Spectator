using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Config
{
    public class TargetFileReader
    {
        public string TargetFileName { get; private set; }
        public TargetFileReader(string targetFileName)
        {
            TargetFileName = targetFileName;
        }

        public List<Target> Read()
        {
            var file = File.ReadLines(TargetFileName);
            var targetList = file.Select(line => new Target(line)).ToList();
            return targetList;
        }
    }
}
