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
            _taskQueue = new Queue<QTask>();
            _runningTasksCount = 0;
        }

        public void Enqueue(Action action, bool parallel=false)
        {
            _queueLock.WaitOne();
            _taskQueue.Enqueue(new QTask{ Action = action, IsParallel = parallel});
            _queueLock.ReleaseMutex();
            RunTasks();
        }

        private void RunTasks()
        {
            _queueLock.WaitOne();
            if (_runningTasksCount != 0)
            {
                _queueLock.ReleaseMutex();
                return;
            }
            while (_taskQueue.Count > 0 && _taskQueue.Peek().IsParallel)
            {
                var task = _taskQueue.Dequeue();
                QueueWorkItem(task);
            }
            if (_taskQueue.Count > 0 && _runningTasksCount == 0)
            {
                var action = _taskQueue.Dequeue();
                QueueWorkItem(action);
            }
            _queueLock.ReleaseMutex();
        }

        private void QueueWorkItem(QTask task)
        {
            Action workTask = () =>
            {
                task.Action();
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
        private readonly Queue<QTask> _taskQueue; 
        private readonly Mutex _queueLock;

        private class QTask
        {
            public Action Action { get; set; }
            public bool IsParallel { get; set; }
        }
    }
}
