﻿using System.IO;

namespace LcmsSpectatorModels.Readers
{
    public class IdFileReaderFactory
    {
        public static IIdFileReader CreateReader(string fileName)
        {
            IIdFileReader reader = null;

            var extension = Path.GetExtension(fileName);

            switch (extension)
            {
                case ".tsv":
                case ".txt":
                    var streamReader = new StreamReader(fileName);
                    var line = streamReader.ReadLine();
                    if (line != null && line.Contains("MSGFScore")) reader = new MsgfFileReader(fileName);
                    if (line != null && line.Contains("#MatchedFragments")) reader = new IcFileReader(fileName);
                    streamReader.Close();
                    break;
                case ".gz":
                case ".mzId":
                    reader = new MzIdentMlReader(fileName);
                    break;
            }
            return reader;
        }
    }
}
