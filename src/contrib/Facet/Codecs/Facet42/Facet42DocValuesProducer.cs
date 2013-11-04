using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Codecs.Facet42
{
    internal class Facet42DocValuesProducer : DocValuesProducer
    {
        private readonly IDictionary<int, Facet42BinaryDocValues> fields = new HashMap<int, Facet42BinaryDocValues>();

        internal Facet42DocValuesProducer(SegmentReadState state)
        {
            string fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix, Facet42DocValuesFormat.EXTENSION);
            IndexInput input = state.directory.OpenInput(fileName, state.context);
            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Facet42DocValuesFormat.CODEC, Facet42DocValuesFormat.VERSION_START, Facet42DocValuesFormat.VERSION_START);
                int fieldNumber = input.ReadVInt();
                while (fieldNumber != -1)
                {
                    fields[fieldNumber] = new Facet42BinaryDocValues(input);
                    fieldNumber = input.ReadVInt();
                }

                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(input);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
            }
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            throw new NotSupportedException(@"FacetsDocValues only implements binary");
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            return fields[field.number];
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            throw new NotSupportedException(@"FacetsDocValues only implements binary");
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            throw new NotSupportedException(@"FacetsDocValues only implements binary");
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
