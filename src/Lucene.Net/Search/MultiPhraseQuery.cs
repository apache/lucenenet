using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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

    using J2N.Collections.Generic.Extensions;
    using Lucene.Net.Util;
    using System.Collections;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using IBits = Lucene.Net.Util.IBits;
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
    /// <see cref="MultiPhraseQuery"/> is a generalized version of <see cref="PhraseQuery"/>, with an added
    /// method <see cref="Add(Term[])"/>.
    /// <para/>
    /// To use this class, to search for the phrase "Microsoft app*" first use
    /// <see cref="Add(Term)"/> on the term "Microsoft", then find all terms that have "app" as
    /// prefix using <c>MultiFields.GetFields(IndexReader).GetTerms(string)</c>, and use <see cref="MultiPhraseQuery.Add(Term[])"/>
    /// to add them to the query.
    /// <para/>
    /// Collection initializer note: To create and populate a <see cref="MultiPhraseQuery"/>
    /// in a single statement, you can use the following example as a guide:
    /// 
    /// <code>
    /// var multiPhraseQuery = new MultiPhraseQuery() {
    ///     new Term("field", "microsoft"), 
    ///     new Term("field", "office")
    /// };
    /// </code>
    /// Note that as long as you specify all of the parameters, you can use either
    /// <see cref="Add(Term)"/>, <see cref="Add(Term[])"/>, or <see cref="Add(Term[], int)"/>
    /// as the method to use to initialize. If there are multiple parameters, each parameter set
    /// must be surrounded by curly braces.
    /// </summary>
    public class MultiPhraseQuery : Query, IEnumerable<Term[]> // LUCENENET specific - implemented IEnumerable<Term[]>, which allows for use of collection initializer. See: https://stackoverflow.com/a/9195144
    {
        private string field;
        private readonly IList<Term[]> termArrays = new JCG.List<Term[]>(); // LUCENENET: marked readonly
        private readonly IList<int> positions = new JCG.List<int>();

        private int slop = 0;

        /// <summary>
        /// Sets the phrase slop for this query. </summary>
        /// <seealso cref="PhraseQuery.Slop"/>
        public virtual int Slop
        {
            get => slop;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Slop), "slop value cannot be negative"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

                slop = value;
            }
        }

        /// <summary>
        /// Add a single term at the next position in the phrase. </summary>
        /// <seealso cref="PhraseQuery.Add(Term)"/>
        public virtual void Add(Term term)
        {
            Add(new Term[] { term });
        }

        /// <summary>
        /// Add multiple terms at the next position in the phrase.  Any of the terms
        /// may match.
        /// </summary>
        /// <seealso cref="PhraseQuery.Add(Term)"/>
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
        /// <seealso cref="PhraseQuery.Add(Term, int)"/>
        public virtual void Add(Term[] terms, int position)
        {
            if (termArrays.Count == 0)
            {
                field = terms[0].Field;
            }

            for (var i = 0; i < terms.Length; i++)
            {
                if (!terms[i].Field.Equals(field, StringComparison.Ordinal))
                {
                    throw new ArgumentException("All phrase terms must be in the same field (" + field + "): " + terms[i]);
                }
            }

            termArrays.Add(terms);
            positions.Add(position);
        }

        /// <summary>
        /// Returns a List of the terms in the multiphrase.
        /// Do not modify the List or its contents.
        /// </summary>
        public virtual IList<Term[]> GetTermArrays() // LUCENENET TODO: API - make into a property
        {
            return termArrays.AsReadOnly();
        }

        /// <summary>
        /// Returns the relative positions of terms in this phrase.
        /// </summary>
        public virtual int[] GetPositions()
        {
            var result = new int[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                result[i] = (int)positions[i];
            }
            return result;
        }

        /// <summary>
        /// Expert: adds all terms occurring in this query to the terms set. Only
        /// works if this query is in its rewritten (<see cref="Rewrite(IndexReader)"/>) form.
        /// </summary>
        /// <exception cref="InvalidOperationException"> If this query is not yet rewritten </exception>
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
            private readonly MultiPhraseQuery outerInstance;

            private readonly Similarity similarity;
            private readonly Similarity.SimWeight stats; 
            private readonly IDictionary<Term, TermContext> termContexts = new Dictionary<Term, TermContext>();

            public MultiPhraseWeight(MultiPhraseQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                this.similarity = searcher.Similarity;
                IndexReaderContext context = searcher.TopReaderContext;

                // compute idf
                var allTermStats = new JCG.List<TermStatistics>();
                foreach (Term[] terms in outerInstance.termArrays)
                {
                    foreach (Term term in terms)
                    {
                        if (!termContexts.TryGetValue(term, out TermContext termContext) || termContext is null)
                        {
                            termContext = TermContext.Build(context, term);
                            termContexts[term] = termContext;
                        }
                        allTermStats.Add(searcher.TermStatistics(term, termContext));
                    }
                }
                stats = similarity.ComputeWeight(outerInstance.Boost, searcher.CollectionStatistics(outerInstance.field), allTermStats.ToArray());
            }

            public override Query Query => outerInstance;

            public override float GetValueForNormalization()
            {
                return stats.GetValueForNormalization();
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                stats.Normalize(queryNorm, topLevelBoost);
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.termArrays.Count > 0);
                AtomicReader reader = (context.AtomicReader);
                IBits liveDocs = acceptDocs;

                PhraseQuery.PostingsAndFreq[] postingsFreqs = new PhraseQuery.PostingsAndFreq[outerInstance.termArrays.Count];

                Terms fieldTerms = reader.GetTerms(outerInstance.field);
                if (fieldTerms is null)
                {
                    return null;
                }

                // Reuse single TermsEnum below:
                TermsEnum termsEnum = fieldTerms.GetEnumerator();

                for (int pos = 0; pos < postingsFreqs.Length; pos++)
                {
                    Term[] terms = outerInstance.termArrays[pos];

                    DocsAndPositionsEnum postingsEnum;
                    int docFreq;

                    if (terms.Length > 1)
                    {
                        postingsEnum = new UnionDocsAndPositionsEnum(liveDocs, context, terms, termContexts, termsEnum);

                        // coarse -- this overcounts since a given doc can
                        // have more than one term:
                        docFreq = 0;
                        for (int termIdx = 0; termIdx < terms.Length; termIdx++)
                        {
                            Term term = terms[termIdx];
                            TermState termState = termContexts[term].Get(context.Ord);
                            if (termState is null)
                            {
                                // Term not in reader
                                continue;
                            }
                            termsEnum.SeekExact(term.Bytes, termState);
                            docFreq += termsEnum.DocFreq;
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
                        TermState termState = termContexts[term].Get(context.Ord);
                        if (termState is null)
                        {
                            // Term not in reader
                            return null;
                        }
                        termsEnum.SeekExact(term.Bytes, termState);
                        postingsEnum = termsEnum.DocsAndPositions(liveDocs, null, DocsAndPositionsFlags.NONE);

                        if (postingsEnum is null)
                        {
                            // term does exist, but has no positions
                            if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum.Docs(liveDocs, null, DocsFlags.NONE) != null, "termstate found but no term exists in reader");
                            throw IllegalStateException.Create("field \"" + term.Field + "\" was indexed without position data; cannot run PhraseQuery (term=" + term.Text + ")");
                        }

                        docFreq = termsEnum.DocFreq;
                    }

                    postingsFreqs[pos] = new PhraseQuery.PostingsAndFreq(postingsEnum, docFreq, (int)outerInstance.positions[pos], terms);
                }

                // sort by increasing docFreq order
                if (outerInstance.slop == 0)
                {
                    ArrayUtil.TimSort(postingsFreqs);
                }

                if (outerInstance.slop == 0)
                {
                    ExactPhraseScorer s = new ExactPhraseScorer(this, postingsFreqs, similarity.GetSimScorer(stats, context));
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
                    return new SloppyPhraseScorer(this, postingsFreqs, outerInstance.slop, similarity.GetSimScorer(stats, context));
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Scorer scorer = GetScorer(context, (context.AtomicReader).LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = outerInstance.slop == 0 ? scorer.Freq : ((SloppyPhraseScorer)scorer).SloppyFreq;
                        SimScorer docScorer = similarity.GetSimScorer(stats, context);
                        ComplexExplanation result = new ComplexExplanation();
                        result.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:";
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
            if (field is null || !field.Equals(f, StringComparison.Ordinal))
            {
                buffer.Append(field);
                buffer.Append(':');
            }

            buffer.Append("\"");
            int k = 0;
            int lastPos = -1;
            bool first = true;
            foreach (Term[] terms in termArrays)
            {
                int position = positions[k];
                if (first)
                {
                    first = false;
                }
                else
                {
                    buffer.Append(' ');
                    for (int j = 1; j < (position - lastPos); j++)
                    {
                        buffer.Append("? ");
                    }
                }
                if (terms.Length > 1)
                {
                    buffer.Append('(');
                    for (int j = 0; j < terms.Length; j++)
                    {
                        buffer.Append(terms[j].Text);
                        if (j < terms.Length - 1)
                        {
                            buffer.Append(' ');
                        }
                    }
                    buffer.Append(')');
                }
                else
                {
                    buffer.Append(terms[0].Text);
                }
                lastPos = position;
                ++k;
            }
            buffer.Append("\"");

            if (slop != 0)
            {
                buffer.Append('~');
                buffer.Append(slop);
            }

            buffer.Append(ToStringUtils.Boost(Boost));

            return buffer.ToString();
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="o"/> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is MultiPhraseQuery))
            {
                return false;
            }
            MultiPhraseQuery other = (MultiPhraseQuery)o;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost)
                && this.slop == other.slop
                && TermArraysEquals(this.termArrays, other.termArrays)
                && this.positions.Equals(other.positions);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            //If this doesn't work hash all elements of positions. This was used to reduce time overhead
            return J2N.BitConversion.SingleToInt32Bits(Boost) 
                ^ slop 
                ^ TermArraysHashCode() 
                ^ ((positions.Count == 0) ? 0 : positions.GetHashCode() 
                ^ 0x4AC65113);
        }

        // Breakout calculation of the termArrays hashcode
        private int TermArraysHashCode()
        {
            int hashCode = 1;
            foreach (Term[] termArray in termArrays)
            {
                hashCode = 31 * hashCode 
                    + (termArray is null ? 0 : Arrays.GetHashCode(termArray));
            }
            return hashCode;
        }

        // Breakout calculation of the termArrays equals
        private static bool TermArraysEquals(IList<Term[]> termArrays1, IList<Term[]> termArrays2) // LUCENENET: CA1822: Mark members as static
        {
            if (termArrays1.Count != termArrays2.Count)
            {
                return false;
            }
            using (IEnumerator<Term[]> iterator1 = termArrays1.GetEnumerator())
            using (IEnumerator<Term[]> iterator2 = termArrays2.GetEnumerator())
            {
                while (iterator1.MoveNext())
                {
                    Term[] termArray1 = iterator1.Current;
                    iterator2.MoveNext();
                    Term[] termArray2 = iterator2.Current;
                    if (!(termArray1 is null ? termArray2 is null : Arrays.Equals(termArray1, termArray2)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="termArrays"/> collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="termArrays"/> collection.</returns>
        // LUCENENET specific
        public IEnumerator<Term[]> GetEnumerator()
        {
            return termArrays.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="termArrays"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="termArrays"/> collection.</returns>
        // LUCENENET specific
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Takes the logical union of multiple <see cref="DocsEnum"/> iterators.
    /// </summary>

    // TODO: if ever we allow subclassing of the *PhraseScorer
    internal class UnionDocsAndPositionsEnum : DocsAndPositionsEnum
    {
        private sealed class DocsQueue : Util.PriorityQueue<DocsAndPositionsEnum>
        {
            internal DocsQueue(ICollection<DocsAndPositionsEnum> docsEnums)
                : base(docsEnums.Count)
            {
                foreach (DocsAndPositionsEnum postings in docsEnums)
                {
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

        /// <summary>
        /// NOTE: This was IntQueue in Lucene
        /// </summary>
        private sealed class Int32Queue
        {
            public Int32Queue()
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

            internal int Count => (_lastIndex - _index); // LUCENENET NOTE: This was size() in Lucene.

            private void GrowArray()
            {
                var newArray = new int[_arraySize * 2];
                Arrays.Copy(_array, 0, newArray, 0, _arraySize);
                _array = newArray;
                _arraySize *= 2;
            }
        }

        private int _doc;
        private int _freq;
        private readonly DocsQueue _queue;
        private readonly Int32Queue _posList;
        private readonly long _cost;

        public UnionDocsAndPositionsEnum(IBits liveDocs, AtomicReaderContext context, Term[] terms, IDictionary<Term, TermContext> termContexts, TermsEnum termsEnum)
        {
            ICollection<DocsAndPositionsEnum> docsEnums = new LinkedList<DocsAndPositionsEnum>();
            for (int i = 0; i < terms.Length; i++)
            {
                Term term = terms[i];
                TermState termState = termContexts[term].Get(context.Ord);
                if (termState is null)
                {
                    // Term doesn't exist in reader
                    continue;
                }
                termsEnum.SeekExact(term.Bytes, termState);
                DocsAndPositionsEnum postings = termsEnum.DocsAndPositions(liveDocs, null, DocsAndPositionsFlags.NONE);
                if (postings is null)
                {
                    // term does exist, but has no positions
                    throw IllegalStateException.Create("field \"" + term.Field + "\" was indexed without position data; cannot run PhraseQuery (term=" + term.Text + ")");
                }
                _cost += postings.GetCost();
                docsEnums.Add(postings);
            }

            _queue = new DocsQueue(docsEnums);
            _posList = new Int32Queue();
        }

        public override sealed int NextDoc()
        {
            if (_queue.Count == 0)
            {
                return NO_MORE_DOCS;
            }

            // TODO: move this init into positions(): if the search
            // doesn't need the positions for this doc then don't
            // waste CPU merging them:
            _posList.Clear();
            _doc = _queue.Top.DocID;

            // merge sort all positions together
            DocsAndPositionsEnum postings;
            do
            {
                postings = _queue.Top;

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
            } while (_queue.Count > 0 && _queue.Top.DocID == _doc);

            _posList.Sort();
            _freq = _posList.Count;

            return _doc;
        }

        public override int NextPosition()
        {
            return _posList.Next();
        }

        public override int StartOffset => -1;

        public override int EndOffset => -1;

        public override BytesRef GetPayload()
        {
            return null;
        }

        public override sealed int Advance(int target)
        {
            while (_queue.Top != null && target > _queue.Top.DocID)
            {
                DocsAndPositionsEnum postings = _queue.Pop();
                if (postings.Advance(target) != NO_MORE_DOCS)
                {
                    _queue.Add(postings);
                }
            }
            return NextDoc();
        }

        public override sealed int Freq => _freq;

        public override sealed int DocID => _doc;

        public override long GetCost()
        {
            return _cost;
        }
    }
}