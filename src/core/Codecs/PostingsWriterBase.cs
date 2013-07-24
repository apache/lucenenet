using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class PostingsWriterBase : PostingsConsumer, IDisposable
    {
        protected PostingsWriterBase()
        {
        }

        public abstract void Start(IndexOutput termsOut);

        public abstract void StartTerm();

        public abstract void FlushTermsBlock(int start, int count);

        public abstract void FinishTerm(TermStats stats);

        public abstract void SetField(FieldInfo fieldInfo);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        public abstract override void StartDoc(int docID, int freq);

        public abstract override void AddPosition(int position, Util.BytesRef payload, int startOffset, int endOffset);

        public abstract override void FinishDoc();
    }
}
