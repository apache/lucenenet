using System;

namespace Lucene.Net.Index
{
    public interface IMergeScheduler : IDisposable
    {
        void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound);

        IMergeScheduler Clone();
    }
}
