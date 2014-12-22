using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace LcmsSpectator.Utils
{
    public class DataSetCloseRequest : NotificationMessage
    {
        public DataSetCloseRequest(object sender, string notification = "DataSetClosing") : base(sender, notification) { }
    }
}
