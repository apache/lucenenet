using Lucene.Net.Codecs;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class NumericDocValuesWriter : DocValuesWriter
    {
        private const long MISSING = 0L;

        private AppendingLongBuffer pending;
        private Counter iwBytesUsed;
        private long bytesUsed;
        private FieldInfo fieldInfo;

        public NumericDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            pending = new AppendingLongBuffer();
            bytesUsed = pending.RamBytesUsed;
            this.fieldInfo = fieldInfo;
            this.iwBytesUsed = iwBytesUsed;
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public void AddValue(int docID, long value)
        {
            if (docID < pending.Size)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" appears more than once in this document (only one value is allowed per field)");
            }

            // Fill in any holes:
            for (int i = (int)pending.Size; i < docID; ++i)
            {
                pending.Add(MISSING);
            }

            pending.Add(value);

            UpdateBytesUsed();
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = pending.RamBytesUsed;
            iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
            bytesUsed = newBytesUsed;
        }

        internal override void Finish(int numDoc)
        {
        }

        internal override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.segmentInfo.DocCount;

            dvConsumer.AddNumericField(fieldInfo, GetNumericIterator(maxDoc));
        }

        internal override void Abort()
        {
        }

        private IEnumerable<long> GetNumericIterator(int maxDoc)
        {
            // .NET Port: using yield return instead of custom iterator type. Much less code.

            AppendingLongBuffer.Iterator iter = pending.GetIterator();
            int size = (int)pending.Size;
            int upto = 0;

            while (upto < maxDoc)
            {
                long value;
                if (upto < size)
                {
                    value = iter.Next();
                }
                else
                {
                    value = 0;
                }
                upto++;
                // TODO: make reusable Number
                yield return value;
            }
        }
    }
}
