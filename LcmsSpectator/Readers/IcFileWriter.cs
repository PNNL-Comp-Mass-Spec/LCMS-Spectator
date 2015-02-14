using LcmsSpectator.Models;

namespace LcmsSpectator.Readers
{
    public class IcFileWriter
    {
        public void Write(string fileName, IdentificationTree idTree)
        {
            var prsms = idTree.AllPrSms;
            prsms.Sort(new PrSmScoreComparer());

            foreach (var prsm in prsms)
            {
                
            }
        }
    }
}
