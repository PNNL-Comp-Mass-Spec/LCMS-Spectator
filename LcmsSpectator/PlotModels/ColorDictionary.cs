using System;
using System.Collections.Generic;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.ViewModels;
using OxyPlot;

namespace LcmsSpectator.PlotModels
{
    public class ColorDictionary
    {
        public ColorDictionary(int length)
        {
            BuildColorDictionary(length);
        }

        public OxyColor GetColor(LabeledIonViewModel label)
        {
            OxyColor color;
            if (label.IsFragmentIon)
            {
                var index = Math.Min(label.IonType.Charge - 1, _fragmentColors[label.IonType.BaseIonType.Symbol].Count - 1);
                color = _fragmentColors[label.IonType.BaseIonType.Symbol][index];
            }
            else
            {
                if (_precursorColors.ContainsKey(label.Index))
                    color = _precursorColors[label.Index];
                else
                    color = label.Index > 0 ? OxyColors.DarkRed : OxyColors.AliceBlue;
            }

            return color;
        }

        public OxyColor GetColor(BaseIonType baseIonType, int charge)
        {
            var index = Math.Min(charge - 1, _fragmentColors[baseIonType.Symbol].Count - 1);
            return _fragmentColors[baseIonType.Symbol][index];
        }

        public OxyColor GetColor(int precursorIndex)
        {
            OxyColor color;
            if (_precursorColors.ContainsKey(precursorIndex))
                color = _precursorColors[precursorIndex];
            else
                color = precursorIndex > 0 ? OxyColors.DarkRed : OxyColors.AliceBlue;
            return color;
        }

        public void BuildColorDictionary(int length)
        {
            const byte colorMin = 150;
            const byte colorMax = 255;

            _fragmentColors = new Dictionary<string, IList<OxyColor>>();

            var aStart = OxyColor.FromRgb(0, colorMin, 0);
            var aEnd = OxyColor.FromRgb(0, colorMax, 0);
            var aColors = OxyPalette.Interpolate(length, new[] { aEnd, aStart });
            _fragmentColors.Add(BaseIonType.A.Symbol, aColors.Colors);
            _fragmentColors.Add(BaseIonType.A.GetDeconvolutedIon().Symbol, aColors.Colors);

            var bStart = OxyColor.FromRgb(0, 0, colorMin);
            var bEnd = OxyColor.FromRgb(0, 0, colorMax);
            var bColors = OxyPalette.Interpolate(length, new[] { bEnd, bStart });
            _fragmentColors.Add(BaseIonType.B.Symbol, bColors.Colors);
            _fragmentColors.Add(BaseIonType.B.GetDeconvolutedIon().Symbol, bColors.Colors);

            var cStart = OxyColor.FromRgb(0, colorMin, colorMin);
            var cEnd = OxyColor.FromRgb(0, colorMax, colorMax);
            var cColors = OxyPalette.Interpolate(length, new[] { cEnd, cStart });
            _fragmentColors.Add(BaseIonType.C.Symbol, cColors.Colors);
            _fragmentColors.Add(BaseIonType.C.GetDeconvolutedIon().Symbol, cColors.Colors);

            var xStart = OxyColor.FromRgb(colorMin, colorMin, 0);
            var xEnd = OxyColor.FromRgb(colorMax, colorMax, 0);
            var xColors = OxyPalette.Interpolate(length, new[] { xEnd, xStart });
            _fragmentColors.Add(BaseIonType.X.Symbol, xColors.Colors);
            _fragmentColors.Add(BaseIonType.X.GetDeconvolutedIon().Symbol, xColors.Colors);

            var yStart = OxyColor.FromRgb(colorMin, 0, 0);
            var yEnd = OxyColor.FromRgb(colorMax, 0, 0);
            var yColors = OxyPalette.Interpolate(length, new[] { yEnd, yStart });
            _fragmentColors.Add(BaseIonType.Y.Symbol, yColors.Colors);
            _fragmentColors.Add(BaseIonType.Y.GetDeconvolutedIon().Symbol, yColors.Colors);

            var zStart = OxyColor.FromRgb(colorMin, 0, colorMin);
            var zEnd = OxyColor.FromRgb(colorMax, 0, colorMax);
            var zColors = OxyPalette.Interpolate(length, new[] { zEnd, zStart });
            _fragmentColors.Add(BaseIonType.Z.Symbol, zColors.Colors);
            _fragmentColors.Add(BaseIonType.Z.GetDeconvolutedIon().Symbol, zColors.Colors);

            _precursorColors = new Dictionary<int, OxyColor>
            {
                {-1, OxyColors.DarkGray},
                {0, OxyColors.Purple},
                {1, OxyColors.Red},
                {2, OxyColors.Blue},
                {3, OxyColors.Green},
                {4, OxyColors.Gold},
                {5, OxyColors.Brown},
                {6, OxyColors.Orange},
                {7, OxyColors.PaleGreen},
                {8, OxyColors.Turquoise},
                {9, OxyColors.Olive},
                {10, OxyColors.Beige},
                {11, OxyColors.Lime},
                {12, OxyColors.Salmon},
                {13, OxyColors.MintCream},
                {14, OxyColors.SteelBlue},
                {15, OxyColors.Violet},
                {16, OxyColors.Blue},
                {17, OxyColors.Navy},
                {18, OxyColors.Red},
                {19, OxyColors.SpringGreen},
                {20, OxyColors.Gold},
                {21, OxyColors.DarkOrange}
            };
        }

        private Dictionary<string, IList<OxyColor>> _fragmentColors;
        private Dictionary<int, OxyColor> _precursorColors;
    }
}
