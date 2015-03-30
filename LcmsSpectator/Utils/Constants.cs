namespace LcmsSpectator.Utils
{
    public class Constants
    {
        public const int MinCharge = 1;
        public const int MaxCharge = 15;
        public const int IsotopeOffsetTolerance = 2;
        public const int MinIsotopeIndex = -1;
        public const int MaxIsotopeIndex = 2;
    }

    public class FileConstants
    {
        public const string IdFileFormatString =
            @"Supported Files|*.txt;*.tsv;*.mzId;*.mzId.gz;*.mtdb|TSV Files (*.txt; *.tsv)|*.txt;*.tsv|MzId Files (*.mzId[.gz])|*.mzId;*.mzId.gz|MTDB Files (*.mtdb)|*.mtdb";

        public const string RawFileFormatString =
            @"Supported Files|*.raw;*.mzML;*.mzML.gz|Raw Files (*.raw)|*.raw|MzMl Files (*.mzMl[.gz])|*.mzMl;*.mzML.gz";

        public const string FeatureFileFormatString = @"Ms1FT Files (*.ms1ft)|*.ms1ft";
    }
}
