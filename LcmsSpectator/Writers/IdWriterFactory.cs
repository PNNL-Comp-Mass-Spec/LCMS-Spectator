using System;
using System.IO;

namespace LcmsSpectator.Writers
{
    [Obsolete("Unused")]
    public class IdWriterFactory
    {
        public static IIdWriter GetIdWriter(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            IIdWriter writer = null;

            if (string.IsNullOrEmpty(extension))
                throw new Exception("Specified file path does not have an extension");
            switch (extension.ToLower())
            {
                case ".tsv":
                    writer = new IcFileWriter(filePath);
                    break;
                case ".mzid":
                    writer = new MzIdWriter(filePath);
                    break;
            }

            return writer;
        }
    }
}
