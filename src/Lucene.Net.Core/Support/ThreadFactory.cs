using System.Threading;

namespace Lucene.Net.Support
{
    public abstract class ThreadFactory // LUCENENET TODO: Since NamedThreadFactory (the only subclass) is Java-specific, we can eliminate this
    {
        public abstract Thread NewThread(IThreadRunnable r);
    }
}