using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    public class Lucene3xSegmentInfoFormat : SegmentInfoFormat
    {
        private readonly SegmentInfoReader reader = new Lucene3xSegmentInfoReader();

        /** This format adds optional per-segment String
         *  diagnostics storage, and switches userData to Map */
        public const int FORMAT_DIAGNOSTICS = -9;

        /** Each segment records whether it has term vectors */
        public const int FORMAT_HAS_VECTORS = -10;

        /** Each segment records the Lucene version that created it. */
        public const int FORMAT_3_1 = -11;

        /** Extension used for saving each SegmentInfo, once a 3.x
         *  index is first committed to with 4.0. */
        public const string UPGRADED_SI_EXTENSION = "si";
        public const string UPGRADED_SI_CODEC_NAME = "Lucene3xSegmentInfo";
        public const int UPGRADED_SI_VERSION_START = 0;
        public const int UPGRADED_SI_VERSION_CURRENT = UPGRADED_SI_VERSION_START;

        public override SegmentInfoReader SegmentInfoReader
        {
            get { return reader; }
        }

        public override SegmentInfoWriter SegmentInfoWriter
        {
            get { throw new NotSupportedException("this codec can only be used for reading"); }
        }

        // only for backwards compat
        public static readonly string DS_OFFSET_KEY = typeof(Lucene3xSegmentInfoFormat).Name + ".dsoffset";
        public static readonly string DS_NAME_KEY = typeof(Lucene3xSegmentInfoFormat).Name + ".dsname";
        public static readonly string DS_COMPOUND_KEY = typeof(Lucene3xSegmentInfoFormat).Name + ".dscompound";
        public static readonly string NORMGEN_KEY = typeof(Lucene3xSegmentInfoFormat).Name + ".normgen";
        public static readonly string NORMGEN_PREFIX = typeof(Lucene3xSegmentInfoFormat).Name + ".normfield";

        public static int GetDocStoreOffset(SegmentInfo si)
        {
            String v = si.GetAttribute(DS_OFFSET_KEY);
            return v == null ? -1 : int.Parse(v);
        }

        public static string GetDocStoreSegment(SegmentInfo si)
        {
            String v = si.GetAttribute(DS_NAME_KEY);
            return v == null ? si.name : v;
        }

        public static bool GetDocStoreIsCompoundFile(SegmentInfo si)
        {
            String v = si.GetAttribute(DS_COMPOUND_KEY);
            return v == null ? false : bool.Parse(v);
        }
    }
}
