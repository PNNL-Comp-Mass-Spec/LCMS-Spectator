using System;
using System.Threading;
using System.Threading.Tasks;

namespace LcmsSpectator.TaskServices
{
    public class TimedTaskService: ITaskService
    {
        public TimedTaskService(uint waitTime = 0)
        {
            _mostRecentAction = null;
            _time = 0;
            WaitTime = waitTime;
            _actionLock = new object();
            _runTimer = false;
        }

        public void Enqueue(Action action, bool parallel = true)
        {
            /*if (!parallel)
            {
                throw new ArgumentException("This task service can only run parallel tasks.");
            }*/

            _time = 0;
            lock (_actionLock)
            {
                _mostRecentAction = action;
                if (_timerTask == null)
                {
                    _runTimer = true;
                    _timerTask = Task.Factory.StartNew(RunTasks);
                }
            }
        }

        public uint WaitTime { get; set; }

        private void RunTasks()
        {
            while (_runTimer)
            {
                Thread.Sleep(10);
                _time += 10;

                if (_time >= WaitTime)
                {
                    lock (_actionLock)
                    {
                        Action workTask = () => _mostRecentAction();
                        ThreadPool.QueueUserWorkItem(_ => workTask());
                        _runTimer = false;
                        _timerTask = null;
                    }
                }
            }
        }

        private bool _runTimer;
        private Task _timerTask;
        private readonly Object _actionLock;
        private Action _mostRecentAction;
        private uint _time;
    }
}
