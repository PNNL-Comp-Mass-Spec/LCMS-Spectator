using LcmsSpectator.Utils;

namespace LcmsSpectator.TaskServices
{
    public class TaskServiceFactory
    {
        public static ITaskService GetTaskServiceLike(ITaskService taskService)
        {
            ITaskService newTaskService = null;
            if (taskService is TaskService) newTaskService = new TaskService();
            else if (taskService is MockTaskService) newTaskService = new MockTaskService();
            return newTaskService;
        }
    }
}
