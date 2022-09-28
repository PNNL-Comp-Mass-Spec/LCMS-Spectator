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

            if (string.IsNullOrEmpty(extension))
                throw new Exception("Specified file path does not have an extension");

            switch (extension.ToLower())
            {
                case ".tsv":
                    return new IcFileWriter(filePath);

                case ".mzid":
                    return new MzIdWriter(filePath);
            }

            return null;
        }
    }
}
