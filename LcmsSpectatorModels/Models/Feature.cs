using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectatorModels.Models
{
    public class Feature
    {
        public int MinScan { get; set; }
        public int MaxScan { get; set; }
        public double MinRetentionTime { get; set; }
        public double MaxRetentionTime { get; set; }
        public double Mass { get; set; }
        public double Mz { get; set; }
        public int MinCharge { get; set; }
        public int MaxCharge { get; set; }
        public double Abundance { get; set; }
        public double Score { get; set; }
        public Isotope[] Isotopes { get; set; }
    }
}
