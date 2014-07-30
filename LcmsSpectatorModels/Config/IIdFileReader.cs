using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Config
{
    interface IIdFileReader
    {
        IdentificationTree Read();
    }
}
