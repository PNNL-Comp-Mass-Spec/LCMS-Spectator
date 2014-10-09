using System;
using System.Collections.Generic;
using System.Threading;
using LcmsSpectator.Utils;

namespace LcmsSpectator.TaskServices
{
    public class TaskService: ITaskService
    {
        public TaskService()
        {
            _queueLock = new Mutex();
            _taskQueue = new Queue<Action>();
            _runningTasksCount = 0;
        }

        public void Enqueue(Action action)
        {
            _queueLock.WaitOne();
            _taskQueue.Enqueue(action);
            _queueLock.ReleaseMutex();
            RunTasks();
        }

        private void RunTasks()
        {
            _queueLock.WaitOne();
            if (_taskQueue.Count > 0 && _runningTasksCount == 0)
            {
                var action = _taskQueue.Dequeue();
                QueueWorkItem(action);
            }
            _queueLock.ReleaseMutex();
        }

        private void QueueWorkItem(Action action)
        {
            Action workTask = () =>
            {
                action();
                OnTaskCompleted();
            };

            _runningTasksCount++;
            ThreadPool.QueueUserWorkItem(_ => workTask());
        }

        private void OnTaskCompleted()
        {
            _queueLock.WaitOne();
            _runningTasksCount--;
            if (_runningTasksCount == 0) RunTasks();
            _queueLock.ReleaseMutex();
        }

        private int _runningTasksCount;
        private readonly Queue<Action> _taskQueue; 
        private readonly Mutex _queueLock;
    }
}
