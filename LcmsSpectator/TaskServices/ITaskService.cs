using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectator.Utils
{
    public interface ITaskService
    {
        void Enqueue(Action action);
    }
}
