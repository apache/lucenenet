using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Util
{


    /// <summary>
    ///  Contract for a strategy that removes dead object references created by 
    /// <see cref="PurgeableThreadLocal{T}"/>
    /// </summary>
    public interface IPurgeStrategy : IDisposable
    {
        /// <summary>
        /// Adds the value asynchronously.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><see cref="Task"/></returns>
        Task AddAsync(object value);

        /// <summary>
        /// Purges the object references asynchronously.
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        Task PurgeAsync();
    }
}
