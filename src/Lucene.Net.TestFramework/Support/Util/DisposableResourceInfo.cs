using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace Lucene.Net.Util
{
    /// <summary>
    /// Allocation information (Thread, allocation stack) for tracking disposable
    /// resources.
    /// </summary>
    internal sealed class DisposableResourceInfo // From randomizedtesing
    {
        private readonly IDisposable resource;
        private readonly LifecycleScope scope;
        private readonly StackTrace stackTrace;
        private readonly string? threadName;

        public DisposableResourceInfo(IDisposable resource, LifecycleScope scope, string? threadName, StackTrace stackTrace)
        {
            Debug.Assert(resource != null);

            this.resource = resource!;
            this.scope = scope;
            this.stackTrace = stackTrace;
            this.threadName = threadName;
        }

        public IDisposable Resource => resource;

        public StackTrace StackTrace => stackTrace;

        public LifecycleScope Scope => scope;

        public string? ThreadName => threadName;
    }
}
