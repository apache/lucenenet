using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public class Lucene40DocValuesFormat : DocValuesFormat
    {
        public Lucene40DocValuesFormat()
            : base("Lucene40")
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotSupportedException("this codec can only be used for reading");
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            String filename = IndexFileNames.SegmentFileName(state.segmentInfo.name,
                                                     "dv",
                                                     IndexFileNames.COMPOUND_FILE_EXTENSION);
            return new Lucene40DocValuesReader(state, filename, Lucene40FieldInfosReader.LEGACY_DV_TYPE_KEY);
        }

        // constants for VAR_INTS
        internal const String VAR_INTS_CODEC_NAME = "PackedInts";
        internal const int VAR_INTS_VERSION_START = 0;
        internal const int VAR_INTS_VERSION_CURRENT = VAR_INTS_VERSION_START;
        internal const byte VAR_INTS_PACKED = 0x00;
        internal const byte VAR_INTS_FIXED_64 = 0x01;

        // constants for FIXED_INTS_8, FIXED_INTS_16, FIXED_INTS_32, FIXED_INTS_64
        internal const String INTS_CODEC_NAME = "Ints";
        internal const int INTS_VERSION_START = 0;
        internal const int INTS_VERSION_CURRENT = INTS_VERSION_START;

        // constants for FLOAT_32, FLOAT_64
        internal const String FLOATS_CODEC_NAME = "Floats";
        internal const int FLOATS_VERSION_START = 0;
        internal const int FLOATS_VERSION_CURRENT = FLOATS_VERSION_START;

        // constants for BYTES_FIXED_STRAIGHT
        internal const String BYTES_FIXED_STRAIGHT_CODEC_NAME = "FixedStraightBytes";
        internal const int BYTES_FIXED_STRAIGHT_VERSION_START = 0;
        internal const int BYTES_FIXED_STRAIGHT_VERSION_CURRENT = BYTES_FIXED_STRAIGHT_VERSION_START;

        // constants for BYTES_VAR_STRAIGHT
        internal const String BYTES_VAR_STRAIGHT_CODEC_NAME_IDX = "VarStraightBytesIdx";
        internal const String BYTES_VAR_STRAIGHT_CODEC_NAME_DAT = "VarStraightBytesDat";
        internal const int BYTES_VAR_STRAIGHT_VERSION_START = 0;
        internal const int BYTES_VAR_STRAIGHT_VERSION_CURRENT = BYTES_VAR_STRAIGHT_VERSION_START;

        // constants for BYTES_FIXED_DEREF
        internal const String BYTES_FIXED_DEREF_CODEC_NAME_IDX = "FixedDerefBytesIdx";
        internal const String BYTES_FIXED_DEREF_CODEC_NAME_DAT = "FixedDerefBytesDat";
        internal const int BYTES_FIXED_DEREF_VERSION_START = 0;
        internal const int BYTES_FIXED_DEREF_VERSION_CURRENT = BYTES_FIXED_DEREF_VERSION_START;

        // constants for BYTES_VAR_DEREF
        internal const String BYTES_VAR_DEREF_CODEC_NAME_IDX = "VarDerefBytesIdx";
        internal const String BYTES_VAR_DEREF_CODEC_NAME_DAT = "VarDerefBytesDat";
        internal const int BYTES_VAR_DEREF_VERSION_START = 0;
        internal const int BYTES_VAR_DEREF_VERSION_CURRENT = BYTES_VAR_DEREF_VERSION_START;

        // constants for BYTES_FIXED_SORTED
        internal const String BYTES_FIXED_SORTED_CODEC_NAME_IDX = "FixedSortedBytesIdx";
        internal const String BYTES_FIXED_SORTED_CODEC_NAME_DAT = "FixedSortedBytesDat";
        internal const int BYTES_FIXED_SORTED_VERSION_START = 0;
        internal const int BYTES_FIXED_SORTED_VERSION_CURRENT = BYTES_FIXED_SORTED_VERSION_START;

        // constants for BYTES_VAR_SORTED
        // NOTE THIS IS NOT A BUG! 4.0 actually screwed this up (VAR_SORTED and VAR_DEREF have same codec header)
        internal const String BYTES_VAR_SORTED_CODEC_NAME_IDX = "VarDerefBytesIdx";
        internal const String BYTES_VAR_SORTED_CODEC_NAME_DAT = "VarDerefBytesDat";
        internal const int BYTES_VAR_SORTED_VERSION_START = 0;
        internal const int BYTES_VAR_SORTED_VERSION_CURRENT = BYTES_VAR_SORTED_VERSION_START;
    }
}
