using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs
{
    public sealed class MappingMultiDocsAndPositionsEnum : DocsAndPositionsEnum
    {
        private MultiDocsAndPositionsEnum.EnumWithSlice[] subs;
        internal int numSubs;
        internal int upto;
        internal MergeState.DocMap currentMap;
        internal DocsAndPositionsEnum current;
        internal int currentBase;
        internal int doc = -1;
        private MergeState mergeState;

        public MappingMultiDocsAndPositionsEnum()
        {
        }

        internal MappingMultiDocsAndPositionsEnum Reset(MultiDocsAndPositionsEnum postingsEnum)
        {
            this.numSubs = postingsEnum.NumSubs;
            this.subs = postingsEnum.Subs;
            upto = -1;
            current = null;
            return this;
        }

        public MergeState MergeState
        {
            get { return mergeState; }
            set { mergeState = value; }
        }

        public int NumSubs
        {
            get { return numSubs; }
        }

        public MultiDocsAndPositionsEnum.EnumWithSlice[] Subs
        {
            get { return subs; }
        }

        public override int Freq
        {
            get { return current.Freq; }
        }

        public override int DocID
        {
            get { return current.DocID; }
        }

        public override int Advance(int target)
        {
            throw new NotSupportedException();
        }

        public override int NextDoc()
        {
            while (true)
            {
                if (current == null)
                {
                    if (upto == numSubs - 1)
                    {
                        return this.doc = NO_MORE_DOCS;
                    }
                    else
                    {
                        upto++;
                        int reader = subs[upto].slice.readerIndex;
                        current = subs[upto].docsAndPositionsEnum;
                        currentBase = mergeState.docBase[reader];
                        currentMap = mergeState.docMaps[reader];
                    }
                }

                int doc = current.NextDoc();
                if (doc != NO_MORE_DOCS)
                {
                    // compact deletions
                    doc = currentMap[doc];
                    if (doc == -1)
                    {
                        continue;
                    }
                    return this.doc = currentBase + doc;
                }
                else
                {
                    current = null;
                }
            }
        }

        public override int NextPosition()
        {
            return current.NextPosition();
        }

        public override int StartOffset
        {
            get { return current.StartOffset; }
        }

        public override int EndOffset
        {
            get { return current.EndOffset; }
        }

        public override BytesRef Payload
        {
            get { return current.Payload; }
        }

        public override long Cost
        {
            get
            {
                long cost = 0;
                foreach (MultiDocsAndPositionsEnum.EnumWithSlice enumWithSlice in subs)
                {
                    cost += enumWithSlice.docsAndPositionsEnum.Cost;
                }
                return cost;
            }
        }
    }
}
