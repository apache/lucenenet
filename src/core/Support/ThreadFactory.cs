using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Support
{
    public abstract class ThreadFactory
    {
        public abstract Thread NewThread(IThreadRunnable r);
    }
}