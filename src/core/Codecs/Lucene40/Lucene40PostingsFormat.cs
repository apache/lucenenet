using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public class Lucene40PostingsFormat : PostingsFormat
    {
        protected readonly int minBlockSize;
        protected readonly int maxBlockSize;

        public Lucene40PostingsFormat()
            : this(BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
        {
        }

        private Lucene40PostingsFormat(int minBlockSize, int maxBlockSize)
            : base("Lucene40")
        {
            this.minBlockSize = minBlockSize;
            //assert minBlockSize > 1;
            this.maxBlockSize = maxBlockSize;
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotSupportedException("this codec can only be used for reading");
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase postings = new Lucene40PostingsReader(state.directory, state.fieldInfos, state.segmentInfo, state.context, state.segmentSuffix);

            bool success = false;
            try
            {
                FieldsProducer ret = new BlockTreeTermsReader(
                                                              state.directory,
                                                              state.fieldInfos,
                                                              state.segmentInfo,
                                                              postings,
                                                              state.context,
                                                              state.segmentSuffix,
                                                              state.termsIndexDivisor);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    postings.Dispose();
                }
            }
        }

        /** Extension of freq postings file */
        internal const string FREQ_EXTENSION = "frq";

        /** Extension of prox postings file */
        internal const string PROX_EXTENSION = "prx";

        public override string ToString()
        {
            return Name + "(minBlockSize=" + minBlockSize + " maxBlockSize=" + maxBlockSize + ")";
        }
    }
}
