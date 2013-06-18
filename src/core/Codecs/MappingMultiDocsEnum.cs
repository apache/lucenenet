using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

using EnumWithSlice = Lucene.Net.Index.MultiDocsEnum.EnumWithSlice;

namespace Lucene.Net.Codecs
{
    public sealed class MappingMultiDocsEnum : DocsEnum
    {
        private MultiDocsEnum.EnumWithSlice[] subs;
        internal int numSubs;
        internal int upto;
        internal MergeState.DocMap currentMap;
        internal DocsEnum current;
        internal int currentBase;
        internal int doc = -1;
        private MergeState mergeState;

        public MappingMultiDocsEnum()
        {
        }

        internal MappingMultiDocsEnum Reset(MultiDocsEnum docsEnum)
        {
            this.numSubs = docsEnum.NumSubs;
            this.subs = docsEnum.Subs;
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

        public EnumWithSlice[] Subs
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
                        current = subs[upto].docsEnum;
                        currentBase = mergeState.docBase[reader];
                        currentMap = mergeState.docMaps[reader];
                        //assert currentMap.maxDoc() == subs[upto].slice.length: "readerIndex=" + reader + " subs.len=" + subs.length + " len1=" + currentMap.maxDoc() + " vs " + subs[upto].slice.length;
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

        public override long Cost
        {
            get
            {
                long cost = 0;
                foreach (EnumWithSlice enumWithSlice in subs)
                {
                    cost += enumWithSlice.docsEnum.Cost;
                }
                return cost;
            }
        }
    }
}
