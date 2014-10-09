using System;
using LcmsSpectator.Utils;

namespace LcmsSpectator.TaskServices
{
    public class MockTaskService: ITaskService
    {
        public void Enqueue(Action action)
        {
            action();
        }
    }
}
