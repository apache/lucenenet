using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    public class TaskSchedulerCompletionService<T> : ICompletionService<T>
    {
        private readonly TaskFactory<T> factory;
        private readonly Queue<Task<T>> taskQueue = new Queue<Task<T>>();

        public TaskSchedulerCompletionService(TaskScheduler scheduler)
        {
            this.factory = new TaskFactory<T>(scheduler ?? TaskScheduler.Default);
        }

        public Task<T> Submit(ICallable<T> task)
        {
            var t = factory.StartNew(task.Call);
            taskQueue.Enqueue(t);
            return t;
        }

        public Task<T> Take()
        {
            return taskQueue.Dequeue();
        }
    }
}