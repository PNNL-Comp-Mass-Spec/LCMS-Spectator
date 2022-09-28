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

                    return new IcFileReader(fileName);

                case ".tsv":
                case ".txt":
                    if (fileName.EndsWith("_syn.txt"))
                    {
                        return new MsgfSynopsisReader(fileName);
                    }

                    string headerLine;
                    using (var streamReader = new StreamReader(fileName))
                    {
                        headerLine = streamReader.ReadLine();
                    }

                    if (headerLine?.Contains("MSGFScore") == true)
                    {
                        return new MsgfFileReader(fileName);
                    }

                    if (headerLine?.Contains("#MatchedFragments") == true)
                    {
                        return new IcFileReader(fileName);
                    }

                    if (headerLine?.Contains("Score") == true)
                    {
                        return new BruteForceSearchResultsReader(fileName);
                    }

                    break;

                case ".mzid":
                case ".mzid.gz":
                    return new MzIdentMlReader(fileName);

                case ".mtdb":
                    return new MtdbReader(fileName);
            }
            return null;
        }
    }
}
