using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using SimScorer = Lucene.Net.Search.Similarities.Similarity.SimScorer;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// MultiPhraseQuery is a generalized version of PhraseQuery, with an added
    /// method <seealso cref="#add(Term[])"/>.
    /// To use this class, to search for the phrase "Microsoft app*" first use
    /// add(Term) on the term "Microsoft", then find all terms that have "app" as
    /// prefix using IndexReader.terms(Term), and use MultiPhraseQuery.add(Term[]
    /// terms) to add them to the query.
    ///
    /// </summary>
    public class MultiPhraseQuery : Query
    {
        private string Field; // LUCENENET TODO: Rename (private)
        private List<Term[]> termArrays = new List<Term[]>();
        private readonly List<int> positions = new List<int>();

        private int slop = 0;

        /// <summary>
        /// Sets the phrase slop for this query. </summary>
        /// <seealso cref= PhraseQuery#setSlop(int) </seealso>
        public virtual int Slop
        {
            set
            {
                if (value < 0)
                {
                    throw new System.ArgumentException("slop value cannot be negative");
                }
                slop = value;
            }
            get
            {
                return slop;
            }
        }

        /// <summary>
        /// Add a single term at the next position in the phrase. </summary>
        /// <seealso cref= PhraseQuery#add(Term) </seealso>
        public virtual void Add(Term term)
        {
            Add(new Term[] { term });
        }

        /// <summary>
        /// Add multiple terms at the next position in the phrase.  Any of the terms
        /// may match.
        /// </summary>
        /// <seealso cref= PhraseQuery#add(Term) </seealso>
        public virtual void Add(Term[] terms)
        {
            int position = 0;
            if (positions.Count > 0)
            {
                position = (int)positions[positions.Count - 1] + 1;
            }

            Add(terms, position);
        }

        /// <summary>
        /// Allows to specify the relative position of terms within the phrase.
        /// </summary>
        /// <seealso cref= PhraseQuery#add(Term, int) </seealso>
        public virtual void Add(Term[] terms, int position)
        {
            if (termArrays.Count == 0)
            {
                Field = terms[0].Field;
            }

            for (var i = 0; i < terms.Length; i++)
            {
                if (!terms[i].Field.Equals(Field))
                {
                    throw new System.ArgumentException("All phrase terms must be in the same field (" + Field + "): " + terms[i]);
                }
            }

            termArrays.Add(terms);
            positions.Add(Convert.ToInt32(position));
        }

        /// <summary>
        /// Returns a List of the terms in the multiphrase.
        /// Do not modify the List or its contents.
        /// </summary>
        public virtual IList<Term[]> TermArrays // LUCENENET TODO: Change to GetTermArrays() (conversion)
        {
            get
            {
                return termArrays.AsReadOnly();// Collections.unmodifiableList(TermArrays_Renamed);
            }
        }

        /// <summary>
        /// Returns the relative positions of terms in this phrase.
        /// </summary>
        public virtual int[] Positions // LUCENENET TODO: Change to GetPositions() (array)
        {
            get
            {
                var result = new int[positions.Count];
                for (int i = 0; i < positions.Count; i++)
                {
                    result[i] = (int)positions[i];
                }
                return result;
            }
        }

        // inherit javadoc
        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (Term[] arr in termArrays)
            {
                foreach (Term term in arr)
                {
                    terms.Add(term);
                }
            }
        }

        private class MultiPhraseWeight : Weight
        {
            private readonly MultiPhraseQuery OuterInstance; // LUCENENET TODO: Rename (private)

            private readonly Similarity Similarity; // LUCENENET TODO: Rename (private)
            private readonly Similarity.SimWeight Stats; // LUCENENET TODO: Rename (private)
            private readonly IDictionary<Term, TermContext> TermContexts = new Dictionary<Term, TermContext>(); // LUCENENET TODO: Rename (private)

            public MultiPhraseWeight(MultiPhraseQuery outerInstance, IndexSearcher searcher)
            {
                this.OuterInstance = outerInstance;
                this.Similarity = searcher.Similarity;
                IndexReaderContext context = searcher.TopReaderContext;

                // compute idf
                var allTermStats = new List<TermStatistics>();
                foreach (Term[] terms in outerInstance.termArrays)
                {
                    foreach (Term term in terms)
                    {
                        TermContext termContext;
                        TermContexts.TryGetValue(term, out termContext);
                        if (termContext == null)
                        {
                            termContext = TermContext.Build(context, term);
                            TermContexts[term] = termContext;
                        }
                        allTermStats.Add(searcher.TermStatistics(term, termContext));
                    }
                }
                Stats = Similarity.ComputeWeight(outerInstance.Boost, searcher.CollectionStatistics(outerInstance.Field), allTermStats.ToArray());
            }

            public override Query Query
            {
                get
                {
                    return OuterInstance;
                }
            }

            public override float GetValueForNormalization()
            {
                return Stats.GetValueForNormalization();
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                Stats.Normalize(queryNorm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                Debug.Assert(OuterInstance.termArrays.Count > 0);
                AtomicReader reader = (context.AtomicReader);
                Bits liveDocs = acceptDocs;

                PhraseQuery.PostingsAndFreq[] postingsFreqs = new PhraseQuery.PostingsAndFreq[OuterInstance.termArrays.Count];

                Terms fieldTerms = reader.Terms(OuterInstance.Field);
                if (fieldTerms == null)
                {
                    return null;
                }

                // Reuse single TermsEnum below:
                TermsEnum termsEnum = fieldTerms.Iterator(null);

                for (int pos = 0; pos < postingsFreqs.Length; pos++)
                {
                    Term[] terms = OuterInstance.termArrays[pos];

                    DocsAndPositionsEnum postingsEnum;
                    int docFreq;

                    if (terms.Length > 1)
                    {
                        postingsEnum = new UnionDocsAndPositionsEnum(liveDocs, context, terms, TermContexts, termsEnum);

                        // coarse -- this overcounts since a given doc can
                        // have more than one term:
                        docFreq = 0;
                        for (int termIdx = 0; termIdx < terms.Length; termIdx++)
                        {
                            Term term = terms[termIdx];
                            TermState termState = TermContexts[term].Get(context.Ord);
                            if (termState == null)
                            {
                                // Term not in reader
                                continue;
                            }
                            termsEnum.SeekExact(term.Bytes, termState);
                            docFreq += termsEnum.DocFreq();
                        }

                        if (docFreq == 0)
                        {
                            // None of the terms are in this reader
                            return null;
                        }
                    }
                    else
                    {
                        Term term = terms[0];
                        TermState termState = TermContexts[term].Get(context.Ord);
                        if (termState == null)
                        {
                            // Term not in reader
                            return null;
                        }
                        termsEnum.SeekExact(term.Bytes, termState);
                        postingsEnum = termsEnum.DocsAndPositions(liveDocs, null, DocsEnum.FLAG_NONE);

                        if (postingsEnum == null)
                        {
                            // term does exist, but has no positions
                            Debug.Assert(termsEnum.Docs(liveDocs, null, DocsEnum.FLAG_NONE) != null, "termstate found but no term exists in reader");
                            throw new InvalidOperationException("field \"" + term.Field + "\" was indexed without position data; cannot run PhraseQuery (term=" + term.Text() + ")");
                        }

                        docFreq = termsEnum.DocFreq();
                    }

                    postingsFreqs[pos] = new PhraseQuery.PostingsAndFreq(postingsEnum, docFreq, (int)OuterInstance.positions[pos], terms);
                }

                // sort by increasing docFreq order
                if (OuterInstance.slop == 0)
                {
                    ArrayUtil.TimSort(postingsFreqs);
                }

                if (OuterInstance.slop == 0)
                {
                    ExactPhraseScorer s = new ExactPhraseScorer(this, postingsFreqs, Similarity.DoSimScorer(Stats, context));
                    if (s.noDocs)
                    {
                        return null;
                    }
                    else
                    {
                        return s;
                    }
                }
                else
                {
                    return new SloppyPhraseScorer(this, postingsFreqs, OuterInstance.slop, Similarity.DoSimScorer(Stats, context));
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Scorer scorer = Scorer(context, (context.AtomicReader).LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = OuterInstance.slop == 0 ? scorer.Freq : ((SloppyPhraseScorer)scorer).SloppyFreq;
                        SimScorer docScorer = Similarity.DoSimScorer(Stats, context);
                        ComplexExplanation result = new ComplexExplanation();
                        result.Description = "weight(" + Query + " in " + doc + ") [" + Similarity.GetType().Name + "], result of:";
                        Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                        result.AddDetail(scoreExplanation);
                        result.Value = scoreExplanation.Value;
                        result.Match = true;
                        return result;
                    }
                }

                return new ComplexExplanation(false, 0.0f, "no matching term");
            }
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (termArrays.Count == 0)
            {
                BooleanQuery bq = new BooleanQuery();
                bq.Boost = Boost;
                return bq;
            } // optimize one-term case
            else if (termArrays.Count == 1)
            {
                Term[] terms = termArrays[0];
                BooleanQuery boq = new BooleanQuery(true);
                for (int i = 0; i < terms.Length; i++)
                {
                    boq.Add(new TermQuery(terms[i]), Occur.SHOULD);
                }
                boq.Boost = Boost;
                return boq;
            }
            else
            {
                return this;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new MultiPhraseWeight(this, searcher);
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override sealed string ToString(string f)
        {
            StringBuilder buffer = new StringBuilder();
            if (Field == null || !Field.Equals(f))
            {
                buffer.Append(Field);
                buffer.Append(":");
            }

            buffer.Append("\"");
            int k = 0;
            IEnumerator<Term[]> i = termArrays.GetEnumerator();
            int? lastPos = -1;
            bool first = true;
            while (i.MoveNext())
            {
                Term[] terms = i.Current;
                int? position = positions[k];
                if (first)
                {
                    first = false;
                }
                else
                {
                    buffer.Append(" ");
                    for (int j = 1; j < (position - lastPos); j++)
                    {
                        buffer.Append("? ");
                    }
                }
                if (terms.Length > 1)
                {
                    buffer.Append("(");
                    for (int j = 0; j < terms.Length; j++)
                    {
                        buffer.Append(terms[j].Text());
                        if (j < terms.Length - 1)
                        {
                            buffer.Append(" ");
                        }
                    }
                    buffer.Append(")");
                }
                else
                {
                    buffer.Append(terms[0].Text());
                }
                lastPos = position;
                ++k;
            }
            buffer.Append("\"");

            if (slop != 0)
            {
                buffer.Append("~");
                buffer.Append(slop);
            }

            buffer.Append(ToStringUtils.Boost(Boost));

            return buffer.ToString();
        }

        /// <summary>
        /// Returns true if <code>o</code> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is MultiPhraseQuery))
            {
                return false;
            }
            MultiPhraseQuery other = (MultiPhraseQuery)o;
            return this.Boost == other.Boost 
                && this.slop == other.slop 
                && TermArraysEquals(this.termArrays, other.termArrays) 
                && this.positions.SequenceEqual(other.positions);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode() // LUCENENET TODO: Check this algorithm - it may not be working correctly
        {
            //If this doesn't work hash all elements of positions. This was used to reduce time overhead
            return Number.FloatToIntBits(Boost) 
                ^ slop 
                ^ TermArraysHashCode() 
                ^ ((positions.Count == 0) ? 0 : HashHelpers.CombineHashCodes(positions.First().GetHashCode(), positions.Last().GetHashCode(), positions.Count) 
                ^ 0x4AC65113);
        }

        // Breakout calculation of the termArrays hashcode
        private int TermArraysHashCode()
        {
            int hashCode = 1;
            foreach (Term[] termArray in termArrays)
            {
                hashCode = 31 * hashCode 
                    + (termArray == null ? 0 : Arrays.GetHashCode(termArray));
            }
            return hashCode;
        }

        // Breakout calculation of the termArrays equals
        private bool TermArraysEquals(IList<Term[]> termArrays1, IList<Term[]> termArrays2)
        {
            if (termArrays1.Count != termArrays2.Count)
            {
                return false;
            }
            IEnumerator<Term[]> iterator1 = termArrays1.GetEnumerator();
            IEnumerator<Term[]> iterator2 = termArrays2.GetEnumerator();
            while (iterator1.MoveNext())
            {
                Term[] termArray1 = iterator1.Current;
                iterator2.MoveNext();
                Term[] termArray2 = iterator2.Current;
                if (!(termArray1 == null ? termArray2 == null : Arrays.Equals(termArray1, termArray2)))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Takes the logical union of multiple DocsEnum iterators.
    /// </summary>

    // TODO: if ever we allow subclassing of the *PhraseScorer
    internal class UnionDocsAndPositionsEnum : DocsAndPositionsEnum
    {
        private sealed class DocsQueue : Util.PriorityQueue<DocsAndPositionsEnum>
        {
            internal DocsQueue(ICollection<DocsAndPositionsEnum> docsEnums)
                : base(docsEnums.Count)
            {
                IEnumerator<DocsAndPositionsEnum> i = docsEnums.GetEnumerator();
                while (i.MoveNext())
                {
                    DocsAndPositionsEnum postings = i.Current;
                    if (postings.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        Add(postings);
                    }
                }
            }

            protected internal override bool LessThan(DocsAndPositionsEnum a, DocsAndPositionsEnum b)
            {
                return a.DocID < b.DocID;
            }
        }

        private sealed class IntQueue
        {
            public IntQueue()
            {
                InitializeInstanceFields();
            }

            internal void InitializeInstanceFields()
            {
                _array = new int[_arraySize];
            }

            private int _arraySize = 16;
            private int _index = 0;
            private int _lastIndex = 0;
            private int[] _array;

            internal void Add(int i)
            {
                if (_lastIndex == _arraySize)
                {
                    GrowArray();
                }

                _array[_lastIndex++] = i;
            }

            internal int Next()
            {
                return _array[_index++];
            }

            internal void Sort()
            {
                Array.Sort(_array, _index, _lastIndex);
            }

            internal void Clear()
            {
                _index = 0;
                _lastIndex = 0;
            }

            internal int Size // LUCENENET TODO: rename Count
            {
                get { return (_lastIndex - _index); }
            }

            private void GrowArray()
            {
                var newArray = new int[_arraySize * 2];
                Array.Copy(_array, 0, newArray, 0, _arraySize);
                _array = newArray;
                _arraySize *= 2;
            }
        }

        private int _doc;
        private int _freq;
        private readonly DocsQueue _queue;
        private readonly IntQueue _posList;
        private readonly long _cost;

        public UnionDocsAndPositionsEnum(Bits liveDocs, AtomicReaderContext context, Term[] terms, IDictionary<Term, TermContext> termContexts, TermsEnum termsEnum)
        {
            ICollection<DocsAndPositionsEnum> docsEnums = new LinkedList<DocsAndPositionsEnum>();
            for (int i = 0; i < terms.Length; i++)
            {
                Term term = terms[i];
                TermState termState = termContexts[term].Get(context.Ord);
                if (termState == null)
                {
                    // Term doesn't exist in reader
                    continue;
                }
                termsEnum.SeekExact(term.Bytes, termState);
                DocsAndPositionsEnum postings = termsEnum.DocsAndPositions(liveDocs, null, DocsEnum.FLAG_NONE);
                if (postings == null)
                {
                    // term does exist, but has no positions
                    throw new InvalidOperationException("field \"" + term.Field + "\" was indexed without position data; cannot run PhraseQuery (term=" + term.Text() + ")");
                }
                _cost += postings.Cost();
                docsEnums.Add(postings);
            }

            _queue = new DocsQueue(docsEnums);
            _posList = new IntQueue();
        }

        public override sealed int NextDoc()
        {
            if (_queue.Size() == 0)
            {
                return NO_MORE_DOCS;
            }

            // TODO: move this init into positions(): if the search
            // doesn't need the positions for this doc then don't
            // waste CPU merging them:
            _posList.Clear();
            _doc = _queue.Top().DocID;

            // merge sort all positions together
            DocsAndPositionsEnum postings;
            do
            {
                postings = _queue.Top();

                int freq = postings.Freq;
                for (int i = 0; i < freq; i++)
                {
                    _posList.Add(postings.NextPosition());
                }

                if (postings.NextDoc() != NO_MORE_DOCS)
                {
                    _queue.UpdateTop();
                }
                else
                {
                    _queue.Pop();
                }
            } while (_queue.Size() > 0 && _queue.Top().DocID == _doc);

            _posList.Sort();
            _freq = _posList.Size;

            return _doc;
        }

        public override int NextPosition()
        {
            return _posList.Next();
        }

        public override int StartOffset
        {
            get { return -1; }
        }

        public override int EndOffset
        {
            get { return -1; }
        }

        public override BytesRef Payload
        {
            get
            {
                return null;
            }
        }

        public override sealed int Advance(int target)
        {
            while (_queue.Top() != null && target > _queue.Top().DocID)
            {
                DocsAndPositionsEnum postings = _queue.Pop();
                if (postings.Advance(target) != NO_MORE_DOCS)
                {
                    _queue.Add(postings);
                }
            }
            return NextDoc();
        }

        public override sealed int Freq
        {
            get { return _freq; }
        }

        public override sealed int DocID
        {
            get { return _doc; }
        }

        public override long Cost()
        {
            return _cost;
        }
    }
}