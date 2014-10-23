using System;
using System.Collections.Generic;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectatorModels.Models;
using OxyPlot;

namespace LcmsSpectator.PlotModels
{
    public class ColorDictionary
    {
        public ColorDictionary(int length)
        {
            BuildColorDictionary(length);
        }

        public OxyColor GetColor(LabeledIon label)
        {
            OxyColor color;
            if (label.IsFragmentIon)
            {
                var index = Math.Min(label.IonType.Charge - 1, _fragmentColors[label.IonType.BaseIonType].Count - 1);
                color = _fragmentColors[label.IonType.BaseIonType][index];
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

        public void BuildColorDictionary(int length)
        {
            const byte colorMin = 150;
            const byte colorMax = 255;

            _fragmentColors = new Dictionary<BaseIonType, IList<OxyColor>>();

            var aStart = OxyColor.FromRgb(0, colorMin, 0);
            var aEnd = OxyColor.FromRgb(0, colorMax, 0);
            var aColors = OxyPalette.Interpolate(length, new[] { aEnd, aStart });
            _fragmentColors.Add(BaseIonType.A, aColors.Colors);

            var bStart = OxyColor.FromRgb(0, 0, colorMin);
            var bEnd = OxyColor.FromRgb(0, 0, colorMax);
            var bColors = OxyPalette.Interpolate(length, new[] { bEnd, bStart });
            _fragmentColors.Add(BaseIonType.B, bColors.Colors);

            var cStart = OxyColor.FromRgb(0, colorMin, colorMin);
            var cEnd = OxyColor.FromRgb(0, colorMax, colorMax);
            var cColors = OxyPalette.Interpolate(length, new[] { cEnd, cStart });
            _fragmentColors.Add(BaseIonType.C, cColors.Colors);

            var xStart = OxyColor.FromRgb(colorMin, colorMin, 0);
            var xEnd = OxyColor.FromRgb(colorMax, colorMax, 0);
            var xColors = OxyPalette.Interpolate(length, new[] { xEnd, xStart });
            _fragmentColors.Add(BaseIonType.X, xColors.Colors);

            var yStart = OxyColor.FromRgb(colorMin, 0, 0);
            var yEnd = OxyColor.FromRgb(colorMax, 0, 0);
            var yColors = OxyPalette.Interpolate(length, new[] { yEnd, yStart });
            _fragmentColors.Add(BaseIonType.Y, yColors.Colors);

            var zStart = OxyColor.FromRgb(colorMin, 0, colorMin);
            var zEnd = OxyColor.FromRgb(colorMax, 0, colorMax);
            var zColors = OxyPalette.Interpolate(length, new[] { zEnd, zStart });
            _fragmentColors.Add(BaseIonType.Z, zColors.Colors);

            _precursorColors = new Dictionary<int, OxyColor>
            {
                {-4, OxyColors.AliceBlue},
                {-3, OxyColors.LightBlue},
                {-2, OxyColors.Blue},
                {-1, OxyColors.Navy},
                {0, OxyColors.Purple},
                {1, OxyColors.LightGreen},
                {2, OxyColors.Green},
                {3, OxyColors.Yellow},
                {4, OxyColors.Orange},
                {5, OxyColors.OrangeRed},
                {6, OxyColors.Red},
                {7, OxyColors.DarkRed}
            };
        }

        private Dictionary<BaseIonType, IList<OxyColor>> _fragmentColors;
        private Dictionary<int, OxyColor> _precursorColors;
    }
}
