namespace LcmsSpectator.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.MassSpecData;

    class ProMexModel
    {
        /// <summary>
        /// The LCMSRun for the data set this feature map shows.
        /// </summary>
        private readonly LcMsRun lcms;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProMexModel"/> class.
        /// </summary>
        /// <param name="lcms">The LCMSRun for this feature set.</param>
        public ProMexModel(LcMsRun lcms)
        {
            this.lcms = lcms;
        }
    }
}
