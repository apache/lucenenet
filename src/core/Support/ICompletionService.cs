using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    public interface ICompletionService<V>
    {
        //Task<V> Poll();

        //Task<V> Poll(long timeout, TimeUnit unit);

        Task<V> Submit(ICallable<V> task);

        Task<V> Take();
    }
}