using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class Lucene3xPostingsFormat : PostingsFormat
    {
        /** Extension of terms file */
        public const string TERMS_EXTENSION = "tis";

        /** Extension of terms index file */
        public const string TERMS_INDEX_EXTENSION = "tii";

        /** Extension of freq postings file */
        public const string FREQ_EXTENSION = "frq";

        /** Extension of prox postings file */
        public const string PROX_EXTENSION = "prx";

        public Lucene3xPostingsFormat()
            : base("Lucene3x")
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotSupportedException("this codec can only be used for reading");
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new Lucene3xFields(state.directory, state.fieldInfos, state.segmentInfo, state.context, state.termsIndexDivisor);
        }
    }
}
