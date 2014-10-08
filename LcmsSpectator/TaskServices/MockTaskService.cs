using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectator.Utils
{
    public class MockTaskService: ITaskService
    {
        public void Enqueue(Action action)
        {
            action();
        }
    }
}
