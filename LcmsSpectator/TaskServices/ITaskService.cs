using System;

namespace LcmsSpectator.TaskServices
{
    public interface ITaskService
    {
        void Enqueue(Action action);
    }
}
