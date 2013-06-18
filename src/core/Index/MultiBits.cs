using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    internal sealed class MultiBits : IBits
    {
        private readonly IBits[] subs;

        // length is 1+subs.length (the last entry has the maxDoc):
        private readonly int[] starts;

        private readonly bool defaultValue;

        public MultiBits(IBits[] subs, int[] starts, bool defaultValue)
        {
            //assert starts.length == 1+subs.length;
            this.subs = subs;
            this.starts = starts;
            this.defaultValue = defaultValue;
        }

        private bool CheckLength(int reader, int doc)
        {
            int length = starts[1 + reader] - starts[reader];
            //assert doc - starts[reader] < length: "doc=" + doc + " reader=" + reader + " starts[reader]=" + starts[reader] + " length=" + length;
            return true;
        }

        public bool this[int doc]
        {
            get
            {
                int reader = ReaderUtil.SubIndex(doc, starts);
                //assert reader != -1;
                IBits bits = subs[reader];
                if (bits == null)
                {
                    return defaultValue;
                }
                else
                {
                    //assert checkLength(reader, doc);
                    return bits[doc - starts[reader]];
                }
            }
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append(subs.Length + " subs: ");
            for (int i = 0; i < subs.Length; i++)
            {
                if (i != 0)
                {
                    b.Append("; ");
                }
                if (subs[i] == null)
                {
                    b.Append("s=" + starts[i] + " l=null");
                }
                else
                {
                    b.Append("s=" + starts[i] + " l=" + subs[i].Length + " b=" + subs[i]);
                }
            }
            b.Append(" end=" + starts[subs.Length]);
            return b.ToString();
        }

        public sealed class SubResult
        {
            public bool matches;
            public IBits result;
        }

        public SubResult GetMatchingSub(ReaderSlice slice)
        {
            int reader = ReaderUtil.SubIndex(slice.start, starts);
            //assert reader != -1;
            //assert reader < subs.length: "slice=" + slice + " starts[-1]=" + starts[starts.length-1];
            SubResult subResult = new SubResult();
            if (starts[reader] == slice.start && starts[1 + reader] == slice.start + slice.length)
            {
                subResult.matches = true;
                subResult.result = subs[reader];
            }
            else
            {
                subResult.matches = false;
            }
            return subResult;
        }

        public int Length
        {
            get { return starts[starts.Length - 1]; }
        }
    }
}
