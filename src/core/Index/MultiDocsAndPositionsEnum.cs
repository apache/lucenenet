using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    public sealed class MultiDocsAndPositionsEnum : DocsAndPositionsEnum
    {
        private readonly MultiTermsEnum parent;
        internal readonly DocsAndPositionsEnum[] subDocsAndPositionsEnum;
        private EnumWithSlice[] subs;
        internal int numSubs;
        internal int upto;
        internal DocsAndPositionsEnum current;
        internal int currentBase;
        internal int doc = -1;

        public MultiDocsAndPositionsEnum(MultiTermsEnum parent, int subReaderCount)
        {
            this.parent = parent;
            subDocsAndPositionsEnum = new DocsAndPositionsEnum[subReaderCount];
        }

        public bool CanReuse(MultiTermsEnum parent)
        {
            return this.parent == parent;
        }

        public MultiDocsAndPositionsEnum Reset(EnumWithSlice[] subs, int numSubs)
        {
            this.numSubs = numSubs;
            this.subs = new EnumWithSlice[subs.Length];
            for (int i = 0; i < subs.Length; i++)
            {
                this.subs[i] = new EnumWithSlice();
                this.subs[i].docsAndPositionsEnum = subs[i].docsAndPositionsEnum;
                this.subs[i].slice = subs[i].slice;
            }
            upto = -1;
            doc = -1;
            current = null;
            return this;
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
            get { return doc; }
        }

        public override int Advance(int target)
        {
            //assert target > doc;
            while (true)
            {
                if (current != null)
                {
                    int doc;
                    if (target < currentBase)
                    {
                        // target was in the previous slice but there was no matching doc after it
                        doc = current.NextDoc();
                    }
                    else
                    {
                        doc = current.Advance(target - currentBase);
                    }
                    if (doc == NO_MORE_DOCS)
                    {
                        current = null;
                    }
                    else
                    {
                        return this.doc = doc + currentBase;
                    }
                }
                else if (upto == numSubs - 1)
                {
                    return this.doc = NO_MORE_DOCS;
                }
                else
                {
                    upto++;
                    current = subs[upto].docsAndPositionsEnum;
                    currentBase = subs[upto].slice.start;
                }
            }
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
                        current = subs[upto].docsAndPositionsEnum;
                        currentBase = subs[upto].slice.start;
                    }
                }

                int doc = current.NextDoc();
                if (doc != NO_MORE_DOCS)
                {
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

        public sealed class EnumWithSlice
        {
            internal EnumWithSlice()
            {
            }

            public DocsAndPositionsEnum docsAndPositionsEnum;

            public ReaderSlice slice;

            public override String ToString()
            {
                return slice.ToString() + ":" + docsAndPositionsEnum;
            }
        }

        public override long Cost
        {
            get
            {
                long cost = 0;
                for (int i = 0; i < numSubs; i++)
                {
                    cost += subs[i].docsAndPositionsEnum.Cost;
                }
                return cost;
            }
        }

        public override string ToString()
        {
            return "MultiDocsAndPositionsEnum(" + String.Join(", ", (IEnumerable<EnumWithSlice>)Subs) + ")";
        }
    }
}
