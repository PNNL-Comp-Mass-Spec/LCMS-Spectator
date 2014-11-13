using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectatorModels.Models
{
    public class Feature
    {
        public FeaturePoint MinPoint { get; set; }
        public FeaturePoint MaxPoint { get; set; }
    }

    public class FeaturePoint
    {
        public int Scan { get; set; }
        public double RetentionTime { get; set; }
        public double Mass { get; set; }
        public double Mz { get; set; }
        public int Charge { get; set; }
        public double Abundance { get; set; }
        public double Score { get; set; }
        public Isotope[] Isotopes { get; set; }
    }
}
