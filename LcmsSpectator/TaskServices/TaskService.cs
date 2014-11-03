using System;
using System.Collections.Generic;
using System.Threading;

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
            lock (_queueLock)
            {
                _taskQueue.Enqueue(new QTask { Action = action, IsParallel = parallel });   
            }
            RunTasks();
        }

        private void RunTasks()
        {
            lock (_queueLock)
            {
                if (_runningTasksCount != 0)
                {
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
            }
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
            lock (_queueLock)
            {
                _runningTasksCount--;
                if (_runningTasksCount == 0) RunTasks();   
            }
        }

        private int _runningTasksCount;
        private readonly Queue<QTask> _taskQueue; 
        private readonly Object _queueLock;

        private class QTask
        {
            public Action Action { get; set; }
            public bool IsParallel { get; set; }
        }
    }
}
