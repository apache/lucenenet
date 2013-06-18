using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;

namespace Lucene.Net.Index
{
    public class SegmentReadState
    {
        public readonly Directory directory;
        public readonly SegmentInfo segmentInfo;
        public readonly FieldInfos fieldInfos;
        public readonly IOContext context;
        public int termsIndexDivisor;
        public readonly string segmentSuffix;

        public SegmentReadState(Directory dir, SegmentInfo info,
            FieldInfos fieldInfos, IOContext context, int termsIndexDivisor)
            : this(dir, info, fieldInfos, context, termsIndexDivisor, "")
        {
        }

        public SegmentReadState(Directory dir,
                          SegmentInfo info,
                          FieldInfos fieldInfos,
                          IOContext context,
                          int termsIndexDivisor,
                          String segmentSuffix)
        {
            this.directory = dir;
            this.segmentInfo = info;
            this.fieldInfos = fieldInfos;
            this.context = context;
            this.termsIndexDivisor = termsIndexDivisor;
            this.segmentSuffix = segmentSuffix;
        }

        public SegmentReadState(SegmentReadState other,
                          String newSegmentSuffix)
        {
            this.directory = other.directory;
            this.segmentInfo = other.segmentInfo;
            this.fieldInfos = other.fieldInfos;
            this.context = other.context;
            this.termsIndexDivisor = other.termsIndexDivisor;
            this.segmentSuffix = newSegmentSuffix;
        }
    }
}
