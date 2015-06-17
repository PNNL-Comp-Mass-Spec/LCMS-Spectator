namespace LcmsSpectator.Writers
{
    using System.IO;

    public class IdWriterFactory
    {
        public static IIdWriter GetIdWriter(string filePath)
        {
            var extension = Path.GetFileNameWithoutExtension(filePath);
            IIdWriter writer = null;

            switch (filePath)
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
