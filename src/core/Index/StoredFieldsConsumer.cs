using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal abstract class StoredFieldsConsumer
    {
        public abstract void AddField(int docID, IIndexableField field, FieldInfo fieldInfo);
        public abstract void Flush(SegmentWriteState state);
        public abstract void Abort();
        public abstract void StartDocument();
        public abstract void FinishDocument();
    }
}
