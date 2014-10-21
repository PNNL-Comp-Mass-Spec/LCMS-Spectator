using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace LcmsSpectator.Utils
{
    public class XicCloseRequest : NotificationMessage
    {
        public XicCloseRequest(object sender, string notification = "XicClosing") : base(sender, notification) { }
    }
}
