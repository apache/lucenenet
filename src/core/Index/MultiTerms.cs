using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class MultiTerms : Terms
    {
        private readonly Terms[] subs;
        private readonly ReaderSlice[] subSlices;
        private readonly IComparer<BytesRef> termComp;
        private readonly bool hasOffsets;
        private readonly bool hasPositions;
        private readonly bool hasPayloads;

        public MultiTerms(Terms[] subs, ReaderSlice[] subSlices)
        {
            this.subs = subs;
            this.subSlices = subSlices;

            IComparer<BytesRef> _termComp = null;
            //assert subs.length > 0 : "inefficient: don't use MultiTerms over one sub";
            bool _hasOffsets = true;
            bool _hasPositions = true;
            bool _hasPayloads = false;
            for (int i = 0; i < subs.Length; i++)
            {
                if (_termComp == null)
                {
                    _termComp = subs[i].Comparator;
                }
                else
                {
                    // We cannot merge sub-readers that have
                    // different TermComps
                    IComparer<BytesRef> subTermComp = subs[i].Comparator;
                    if (subTermComp != null && !subTermComp.Equals(_termComp))
                    {
                        throw new InvalidOperationException("sub-readers have different BytesRef.Comparators; cannot merge");
                    }
                }
                _hasOffsets &= subs[i].HasOffsets;
                _hasPositions &= subs[i].HasPositions;
                _hasPayloads |= subs[i].HasPayloads;
            }

            termComp = _termComp;
            hasOffsets = _hasOffsets;
            hasPositions = _hasPositions;
            hasPayloads = hasPositions && _hasPayloads; // if all subs have pos, and at least one has payloads.
        }

        public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
        {
            IList<MultiTermsEnum.TermsEnumIndex> termsEnums = new List<MultiTermsEnum.TermsEnumIndex>();
            for (int i = 0; i < subs.Length; i++)
            {
                TermsEnum termsEnum = subs[i].Intersect(compiled, startTerm);
                if (termsEnum != null)
                {
                    termsEnums.Add(new MultiTermsEnum.TermsEnumIndex(termsEnum, i));
                }
            }

            if (termsEnums.Count > 0)
            {
                return new MultiTermsEnum(subSlices).Reset(termsEnums.ToArray());
            }
            else
            {
                return TermsEnum.EMPTY;
            }
        }

        public override TermsEnum Iterator(TermsEnum reuse)
        {
            IList<MultiTermsEnum.TermsEnumIndex> termsEnums = new List<MultiTermsEnum.TermsEnumIndex>();
            for (int i = 0; i < subs.Length; i++)
            {
                TermsEnum termsEnum = subs[i].Iterator(null);
                if (termsEnum != null)
                {
                    termsEnums.Add(new MultiTermsEnum.TermsEnumIndex(termsEnum, i));
                }
            }

            if (termsEnums.Count > 0)
            {
                return new MultiTermsEnum(subSlices).Reset(termsEnums.ToArray());
            }
            else
            {
                return TermsEnum.EMPTY;
            }
        }

        public override long Size
        {
            get { return -1; }
        }

        public override long SumTotalTermFreq
        {
            get
            {
                long sum = 0;
                foreach (Terms terms in subs)
                {
                    long v = terms.SumTotalTermFreq;
                    if (v == -1)
                    {
                        return -1;
                    }
                    sum += v;
                }
                return sum;
            }
        }

        public override long SumDocFreq
        {
            get
            {
                long sum = 0;
                foreach (Terms terms in subs)
                {
                    long v = terms.SumDocFreq;
                    if (v == -1)
                    {
                        return -1;
                    }
                    sum += v;
                }
                return sum;
            }
        }

        public override int DocCount
        {
            get
            {
                int sum = 0;
                foreach (Terms terms in subs)
                {
                    int v = terms.DocCount;
                    if (v == -1)
                    {
                        return -1;
                    }
                    sum += v;
                }
                return sum;
            }
        }

        public override IComparer<BytesRef> Comparator
        {
            get { return termComp; }
        }

        public override bool HasOffsets
        {
            get { return hasOffsets; }
        }

        public override bool HasPositions
        {
            get { return hasPositions; }
        }

        public override bool HasPayloads
        {
            get { return hasPayloads; }
        }
    }
}
