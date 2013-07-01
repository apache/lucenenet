using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene41
{
    public sealed class Lucene41PostingsFormat : PostingsFormat
    {
        public const string DOC_EXTENSION = "doc";

        public const string POS_EXTENSION = "pos";

        public const string PAY_EXTENSION = "pay";

        private readonly int minTermBlockSize;
        private readonly int maxTermBlockSize;

        public const int BLOCK_SIZE = 128;

        public Lucene41PostingsFormat()
            : this(BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
        {
        }

        public Lucene41PostingsFormat(int minTermBlockSize, int maxTermBlockSize)
            : base("Lucene41")
        {
            this.minTermBlockSize = minTermBlockSize;
            //assert minTermBlockSize > 1;
            this.maxTermBlockSize = maxTermBlockSize;
            //assert minTermBlockSize <= maxTermBlockSize;
        }

        public override string ToString()
        {
            return Name + "(blocksize=" + BLOCK_SIZE + ")";
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase postingsWriter = new Lucene41PostingsWriter(state);

            bool success = false;
            try
            {
                FieldsConsumer ret = new BlockTreeTermsWriter(state,
                                                              postingsWriter,
                                                              minTermBlockSize,
                                                              maxTermBlockSize);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)postingsWriter);
                }
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase postingsReader = new Lucene41PostingsReader(state.directory,
                                                                state.fieldInfos,
                                                                state.segmentInfo,
                                                                state.context,
                                                                state.segmentSuffix);
            bool success = false;
            try
            {
                FieldsProducer ret = new BlockTreeTermsReader(state.directory,
                                                              state.fieldInfos,
                                                              state.segmentInfo,
                                                              postingsReader,
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
                    IOUtils.CloseWhileHandlingException((IDisposable)postingsReader);
                }
            }
        }
    }
}
