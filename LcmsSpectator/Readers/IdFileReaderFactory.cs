using System.IO;

namespace LcmsSpectator.Readers
{
    public class IdFileReaderFactory
    {
        public static IIdFileReader CreateReader(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var extension = Path.GetExtension(fileName);
            string extensionToCheck;

            if (extension == ".gz") // "gz" is a compound extension - the original extension precedes it.
            {
                // Results.mzid.gz
                // Results.mzid
                // .mzid.gz
                extensionToCheck = Path.GetExtension(Path.GetFileNameWithoutExtension(fileName)) + extension;
            }
            else
            {
                extensionToCheck = extension ?? string.Empty;
            }

            switch (extensionToCheck.ToLower())
            {
                case ".zip":
                    // Assume we're reading MSPathFinder results, for example Dataset_IcTsv.zip
                    // The zip file has three files:
                    //   Dataset_IcTarget.tsv
                    //   Dataset_IcDecoy.tsv
                    //   Dataset_IcTda.tsv

                    var icFileReader = new IcFileReader(fileName);
                    return icFileReader;

                case ".tsv":
                case ".txt":
                    if (fileName.EndsWith("_syn.txt"))
                    {
                        var synFileReader = new MsgfSynopsisReader(fileName);
                        return synFileReader;
                    }

                    string headerLine;
                    using (var streamReader = new StreamReader(fileName))
                    {
                        headerLine = streamReader.ReadLine();
                    }

                    if (headerLine != null && headerLine.Contains("MSGFScore"))
                    {
                        var msgfPlusReader = new MsgfFileReader(fileName);
                        return msgfPlusReader;
                    }

                    if (headerLine != null && headerLine.Contains("#MatchedFragments"))
                    {
                        var msPathFinderReader = new IcFileReader(fileName);
                        return msPathFinderReader;
                    }

                    if (headerLine != null && headerLine.Contains("Score"))
                    {
                        var genericReader = new BruteForceSearchResultsReader(fileName);
                        return genericReader;
                    }

                    break;

                case ".mzid":
                case ".mzid.gz":
                    var mzidReader = new MzIdentMlReader(fileName);
                    return mzidReader;

                case ".mtdb":
                    var mtdbReader = new MtdbReader(fileName);
                    return mtdbReader;
            }
            return null;
        }
    }
}
