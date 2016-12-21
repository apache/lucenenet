namespace Lucene.Net.Index
{
    // LUCENENET specific
    public interface IConcurrentMergeScheduler : IMergeScheduler
    {
        int MaxThreadCount { get; }
        int MaxMergeCount { get; }
        int MergeThreadPriority { get; }

        void SetMergeThreadPriority(int priority);
        void Sync();
        void SetMaxMergesAndThreads(int maxMergeCount, int maxThreadCount);

        void SetSuppressExceptions();
        void ClearSuppressExceptions();
    }
}
