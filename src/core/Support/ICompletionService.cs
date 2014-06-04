using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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