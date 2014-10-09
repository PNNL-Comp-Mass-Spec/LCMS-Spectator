using System;

namespace LcmsSpectator.TaskServices
{
    public class MockTaskService: ITaskService
    {
        public void Enqueue(Action action, bool parallel=false)
        {
            action();
        }
    }
}
