using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Codecs.Facet42
{
    public class Facet42DocValuesConsumer : DocValuesConsumer
    {
        readonly IndexOutput output;
        readonly int maxDoc;
        readonly float acceptableOverheadRatio;

        public Facet42DocValuesConsumer(SegmentWriteState state)
            : this(state, PackedInts.DEFAULT)
        {
        }

        public Facet42DocValuesConsumer(SegmentWriteState state, float acceptableOverheadRatio)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            bool success = false;
            try
            {
                string fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix, Facet42DocValuesFormat.EXTENSION);
                output = state.directory.CreateOutput(fileName, state.context);
                CodecUtil.WriteHeader(output, Facet42DocValuesFormat.CODEC, Facet42DocValuesFormat.VERSION_CURRENT);
                maxDoc = state.segmentInfo.DocCount;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)this);
                }
            }
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long> values)
        {
            throw new NotSupportedException(@"FacetsDocValues can only handle binary fields");
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            output.WriteVInt(field.number);
            long totBytes = 0;
            foreach (BytesRef v in values)
            {
                totBytes += v.length;
            }

            if (totBytes > int.MaxValue)
            {
                throw new InvalidOperationException(@"too many facets in one segment: Facet42DocValues cannot handle more than 2 GB facet data per segment");
            }

            output.WriteVInt((int)totBytes);
            foreach (BytesRef v in values)
            {
                output.WriteBytes(v.bytes, v.offset, v.length);
            }

            PackedInts.Writer w = PackedInts.GetWriter(output, maxDoc + 1, PackedInts.BitsRequired(totBytes + 1), acceptableOverheadRatio);
            int address = 0;
            foreach (BytesRef v in values)
            {
                w.Add(address);
                address += v.length;
            }

            w.Add(address);
            w.Finish();
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrd)
        {
            throw new NotSupportedException(@"FacetsDocValues can only handle binary fields");
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
        {
            throw new NotSupportedException(@"FacetsDocValues can only handle binary fields");
        }

        protected override void Dispose(bool disposing)
        {
            bool success = false;
            try
            {
                output.WriteVInt(-1);
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(output);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)output);
                }
            }
        }
    }
}
