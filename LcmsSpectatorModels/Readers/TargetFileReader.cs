using System;
using System.Collections.Generic;
using System.IO;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Readers
{
    public class TargetFileReader
    {
        public string TargetFileName { get; private set; }
        public TargetFileReader(string targetFileName)
        {
            TargetFileName = targetFileName;
        }

        public TargetFileReader(Stream inputStream)
        {
            _inputStream = inputStream;
        }

        public List<Target> Read()
        {
            if (String.IsNullOrEmpty(TargetFileName)) return ReadStream();
            return ReadFile();
        }

        public List<Target> ReadStream()
        {
            var targetList = new List<Target>();
            var reader = new StreamReader(_inputStream);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var parts = line.Split('\t');
                if (parts[0] == "Sequence") continue;
                var sequence = parts[0];
                var charge = 0;
                if (parts.Length > 1) charge = Convert.ToInt32(parts[1]);
                targetList.Add(new Target(sequence, charge));
            }
            return targetList;
        }

        public List<Target> ReadFile()
        {
            var targetList = new List<Target>();
            var file = File.ReadLines(TargetFileName);
            foreach (var line in file)
            {
                var parts = line.Split('\t');
                if (parts[0] == "Sequence") continue;
                var sequence = parts[0];
                var charge = 0;
                if (parts.Length > 1) charge = Convert.ToInt32(parts[1]);
                targetList.Add(new Target(sequence, charge));
            }
            return targetList;
        }

        private readonly Stream _inputStream;
    }
}
