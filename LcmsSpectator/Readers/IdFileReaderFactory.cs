using System.IO;

namespace LcmsSpectator.Readers
{
    public class IdFileReaderFactory
    {
        public static IIdFileReader CreateReader(string fileName)
        {
            IIdFileReader reader = null;

            var extension = Path.GetExtension(fileName);
	        if (extension == ".gz") // "gz" is a compound extension - the original extension precedes it.
	        {
		        extension = Path.GetExtension(Path.GetFileNameWithoutExtension(fileName)) + extension;
	        }

            switch (extension.ToLower())
            {
                case ".zip":
                    reader = new IcFileReader(fileName);
                    break;
                case ".tsv":
                case ".txt":
                    if (fileName.EndsWith("_syn.txt"))
                    {
                        reader = new MsgfSynopsisReader(fileName);
                        break;
                    }

                    var streamReader = new StreamReader(fileName);
                    var line = streamReader.ReadLine();
                    if (line != null && line.Contains("MSGFScore")) reader = new MsgfFileReader(fileName);
                    else if (line != null && line.Contains("#MatchedFragments")) reader = new IcFileReader(fileName);
                    else if (line != null && line.Contains("Score")) reader = new BruteForceSearchResultsReader(fileName);
                    streamReader.Close();
                    break;
				case ".mzid":
				case ".mzid.gz":
                    reader = new MzIdentMlReader(fileName);
                    break;
				case ".mtdb":
					reader = new MtdbReader(fileName);
		            break;
            }
            return reader;
        }
    }
}
