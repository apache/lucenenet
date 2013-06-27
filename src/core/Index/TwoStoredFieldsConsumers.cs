using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class TwoStoredFieldsConsumers : StoredFieldsConsumer
    {
        private readonly StoredFieldsConsumer first;
        private readonly StoredFieldsConsumer second;

        public TwoStoredFieldsConsumers(StoredFieldsConsumer first, StoredFieldsConsumer second)
        {
            this.first = first;
            this.second = second;
        }

        public override void AddField(int docID, IIndexableField field, FieldInfo fieldInfo)
        {
            first.AddField(docID, field, fieldInfo);
            second.AddField(docID, field, fieldInfo);
        }

        public override void Flush(SegmentWriteState state)
        {
            first.Flush(state);
            second.Flush(state);
        }

        public override void Abort()
        {
            try
            {
                first.Abort();
            }
            catch
            {
            }
            try
            {
                second.Abort();
            }
            catch
            {
            }
        }

        public override void StartDocument()
        {
            first.StartDocument();
            second.StartDocument();
        }

        public override void FinishDocument()
        {
            first.FinishDocument();
            second.FinishDocument();
        }
    }
}
