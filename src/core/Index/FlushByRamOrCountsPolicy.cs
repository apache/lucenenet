using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;

namespace Lucene.Net.Index
{
    internal class FlushByRamOrCountsPolicy : FlushPolicy
    {
        public override void OnDelete(DocumentsWriterFlushControl control, DocumentsWriterPerThreadPool.ThreadState state)
        {
            if (FlushOnDeleteTerms)
            {
                // Flush this state by num del terms
                int maxBufferedDeleteTerms = indexWriterConfig.MaxBufferedDeleteTerms;
                if (control.NumGlobalTermDeletes >= maxBufferedDeleteTerms)
                {
                    control.SetApplyAllDeletes();
                }
            }
            DocumentsWriter writer = this.writer.Get();
            if ((FlushOnRAM &&
                control.DeleteBytesUsed > (1024 * 1024 * indexWriterConfig.RAMBufferSizeMB)))
            {
                control.SetApplyAllDeletes();
                if (writer.infoStream.IsEnabled("FP"))
                {
                    writer.infoStream.Message("FP", "force apply deletes bytesUsed=" + control.DeleteBytesUsed + " vs ramBuffer=" + (1024 * 1024 * indexWriterConfig.RAMBufferSizeMB));
                }
            }
        }

        public override void OnInsert(DocumentsWriterFlushControl control, DocumentsWriterPerThreadPool.ThreadState state)
        {
            if (FlushOnDocCount && state.dwpt.NumDocsInRAM >= indexWriterConfig.MaxBufferedDocs)
            {
                // Flush this state by num docs
                control.SetFlushPending(state);
            }
            else if (FlushOnRAM)
            {// flush by RAM
                long limit = (long)(indexWriterConfig.RAMBufferSizeMB * 1024d * 1024d);
                long totalRam = control.ActiveBytes + control.DeleteBytesUsed;
                if (totalRam >= limit)
                {
                    DocumentsWriter writer = this.writer.Get();
                    if (writer.infoStream.IsEnabled("FP"))
                    {
                        writer.infoStream.Message("FP", "flush: activeBytes=" + control.ActiveBytes + " deleteBytes=" + control.DeleteBytesUsed + " vs limit=" + limit);
                    }
                    MarkLargestWriterPending(control, state, totalRam);
                }
            }
        }

        protected void MarkLargestWriterPending(DocumentsWriterFlushControl control, ThreadState perThreadState, long currentBytesPerThread)
        {
            control.SetFlushPending(FindLargestNonPendingWriter(control, perThreadState));
        }

        protected bool FlushOnDocCount
        {
            get
            {
                return indexWriterConfig.MaxBufferedDocs != IndexWriterConfig.DISABLE_AUTO_FLUSH;
            }
        }

        protected bool FlushOnDeleteTerms
        {
            get
            {
                return indexWriterConfig.MaxBufferedDeleteTerms != IndexWriterConfig.DISABLE_AUTO_FLUSH;
            }
        }

        protected bool FlushOnRAM
        {
            get
            {
                return indexWriterConfig.RAMBufferSizeMB != IndexWriterConfig.DISABLE_AUTO_FLUSH;
            }
        }
    }
}
