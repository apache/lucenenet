/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    /// <summary> MultiPhraseQuery is a generalized version of PhraseQuery, with an added
    /// method <see cref="Add(Term[])" />.
    /// To use this class, to search for the phrase "Microsoft app*" first use
    /// add(Term) on the term "Microsoft", then find all terms that have "app" as
    /// prefix using IndexReader.Terms(Term), and use MultiPhraseQuery.add(Term[]
    /// terms) to add them to the query.
    /// 
    /// </summary>
    /// <version>  1.0
    /// </version>
    [Serializable]
    public class MultiPhraseQuery : Query
    {
        private string field;
        private List<Term[]> termArrays = new List<Term[]>();
        private List<int> positions = new List<int>();

        private int slop = 0;

        /// <summary>Gets or sets the phrase slop for this query.</summary>
        /// <seealso cref="PhraseQuery.Slop">
        /// </seealso>
        public virtual int Slop
        {
            get { return slop; }
            set { slop = value; }
        }

        /// <summary>Add a single term at the next position in the phrase.</summary>
        /// <seealso cref="PhraseQuery.Add(Term)">
        /// </seealso>
        public virtual void Add(Term term)
        {
            Add(new Term[] { term });
        }

        /// <summary>Add multiple terms at the next position in the phrase.  Any of the terms
        /// may match.
        /// 
        /// </summary>
        /// <seealso cref="PhraseQuery.Add(Term)">
        /// </seealso>
        public virtual void Add(Term[] terms)
        {
            var position = 0;
            if (positions.Count > 0)
                position = positions[positions.Count - 1] + 1;

            Add(terms, position);
        }

        /// <summary> Allows to specify the relative position of terms within the phrase.
        /// 
        /// </summary>
        /// <seealso cref="PhraseQuery.Add(Term, int)">
        /// </seealso>
        /// <param name="terms">
        /// </param>
        /// <param name="position">
        /// </param>
        public virtual void Add(Term[] terms, int position)
        {
            if (termArrays.Count == 0)
                field = terms[0].Field;

            foreach (var t in terms.Where(t => t.Field != field))
            {
                throw new ArgumentException("All phrase terms must be in the same field (" + field + "): " + t);
            }

            termArrays.Add(terms);
            positions.Add(position);
        }

        /// <summary> Returns a List&lt;Term[]&gt; of the terms in the multiphrase.
        /// Do not modify the List or its contents.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual IList<Term[]> GetTermArrays()
        {
            return termArrays.AsReadOnly();
        }

        /// <summary> Returns the relative positions of terms in this phrase.</summary>
        public virtual int[] GetPositions()
        {
            var result = new int[positions.Count];
            for (var i = 0; i < positions.Count; i++)
                result[i] = positions[i];
            return result;
        }

        // inherit javadoc
        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (var arr in termArrays)
            {
                terms.UnionWith(arr);
            }
        }


        [Serializable]
        private class MultiPhraseWeight : Weight
        {
            private MultiPhraseQuery parent;

            private readonly Similarity similarity;
            private readonly Similarity.SimWeight stats;
            private readonly IDictionary<Term, TermContext> termContexts = new HashMap<Term, TermContext>();


            public MultiPhraseWeight(MultiPhraseQuery parent, IndexSearcher searcher)
            {
                this.parent = parent;
                this.similarity = searcher.Similarity;

                // compute idf
                var allTermStats = new List<TermStatistics>();
                foreach (var terms in parent.termArrays)
                {
                    foreach (var term in terms)
                    {
                        var termContext = termContexts[term];
                        if (termContext == null)
                        {
                            termContext = TermContext.Build(context, term, true);
                            termContexts.Add(term, termContext);
                        }
                        allTermStats.Add(searcher.TermStatistics(term, termContext));
                    }
                }
                stats = similarity.ComputeWeight(parent.Boost,
                                                 searcher.CollectionStatistics(parent.field),
                                                 allTermStats.ToArray());
            }

            public override Query Query
            {
                get { return parent; }
            }

            public override float ValueForNormalization
            {
                get { return stats.ValueForNormalization; }
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                stats.Normalize(queryNorm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer,
                                          IBits acceptDocs)
            {
                //assert !termArrays.isEmpty();
                var reader = context.Reader;
                var liveDocs = acceptDocs;

                var postingsFreqs = new PhraseQuery.PostingsAndFreq[parent.termArrays.Count];

                var fieldTerms = reader.Terms(parent.field);
                if (fieldTerms == null)
                {
                    return null;
                }

                // Reuse single TermsEnum below:
                var termsEnum = fieldTerms.Iterator(null);

                for (var pos = 0; pos < postingsFreqs.Length; pos++)
                {
                    var terms = parent.termArrays[pos];

                    DocsAndPositionsEnum postingsEnum;
                    int docFreq;

                    if (terms.Length > 1)
                    {
                        postingsEnum = new UnionDocsAndPositionsEnum(liveDocs, context, terms, termContexts, termsEnum);

                        // coarse -- this overcounts since a given doc can
                        // have more than one term:
                        docFreq = 0;
                        for (var termIdx = 0; termIdx < terms.Length; termIdx++)
                        {
                            var term = terms[termIdx];
                            var termState = termContexts[term].Get(context.ord);
                            if (termState == null)
                            {
                                // Term not in reader
                                continue;
                            }
                            termsEnum.SeekExact(term.bytes, termState);
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
                        var term = terms[0];
                        var termState = termContexts[term].Get(context.ord);
                        if (termState == null)
                        {
                            // Term not in reader
                            return null;
                        }
                        termsEnum.SeekExact(term.bytes, termState);
                        postingsEnum = termsEnum.DocsAndPositions(liveDocs, null, DocsEnum.FLAG_NONE);

                        if (postingsEnum == null)
                        {
                            // term does exist, but has no positions
                            //assert termsEnum.docs(liveDocs, null, DocsEnum.FLAG_NONE) != null: "termstate found but no term exists in reader";
                            throw new InvalidOperationException("field \"" + term.Field +
                                                                "\" was indexed without position data; cannot run PhraseQuery (term=" +
                                                                term.Text + ")");
                        }

                        docFreq = termsEnum.DocFreq;
                    }

                    postingsFreqs[pos] = new PhraseQuery.PostingsAndFreq(postingsEnum, docFreq, parent.positions[pos],
                                                                         terms);
                }

                // sort by increasing docFreq order
                if (parent.slop == 0)
                {
                    ArrayUtil.MergeSort(postingsFreqs);
                }

                if (parent.slop == 0)
                {
                    var s = new ExactPhraseScorer(this, postingsFreqs, similarity.GetExactSimScorer(stats, context));
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
                    return new SloppyPhraseScorer(this, postingsFreqs, parent.slop,
                                                  similarity.GetSloppySimScorer(stats, context));
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                var scorer = Scorer(context, true, false, context.Reader.LiveDocs);
                if (scorer != null)
                {
                    var newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        var freq = parent.slop == 0 ? scorer.Freq : ((SloppyPhraseScorer)scorer).SloppyFreq;
                        var docScorer = similarity.GetSloppySimScorer(stats, context);
                        var result = new ComplexExplanation
                            {
                                Description =
                                    "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name +
                                    "], result of:"
                            };
                        var scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
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
            if (!termArrays.Any())
            {
                var bq = new BooleanQuery();
                bq.Boost = Boost);
                return bq;
            }
            else if (termArrays.Count == 1)
            {                 // optimize one-term case
                var terms = termArrays[0];
                var boq = new BooleanQuery(true);
                foreach (var t in terms)
                {
                    boq.Add(new TermQuery(t), BooleanClause.Occur.SHOULD);
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

        /// <summary>Prints a user-readable version of this query. </summary>
        public override string ToString(string f)
        {
            var buffer = new StringBuilder();
            if (field == null || !field.Equals(f))
            {
                buffer.Append(field);
                buffer.Append(":");
            }

            buffer.Append("\"");
            var k = 0;
            var i = termArrays.GetEnumerator();
            var lastPos = -1;
            var first = true;
            while (i.MoveNext())
            {
                var terms = i.Current;
                var position = positions[k];
                if (first)
                {
                    first = false;
                }
                else
                {
                    buffer.Append(" ");
                    for (var j = 1; j < (position - lastPos); j++)
                    {
                        buffer.Append("? ");
                    }
                }
                if (terms.Length > 1)
                {
                    buffer.Append("(");
                    for (var j = 0; j < terms.Length; j++)
                    {
                        buffer.Append(terms[j].Text);
                        if (j < terms.Length - 1)
                            buffer.Append(" ");
                    }
                    buffer.Append(")");
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
                buffer.Append("~");
                buffer.Append(slop);
            }

            buffer.Append(ToStringUtils.Boost(Boost));

            return buffer.ToString();
        }


        /// <summary>Returns true if <c>o</c> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is MultiPhraseQuery)) return false;
            var other = (MultiPhraseQuery)o;
            return Boost == other.Boost
              && slop == other.slop
              && TermArraysEquals(termArrays, other.termArrays)
              && positions.Equals(other.positions);
        }

        /// <summary>Returns a hash code value for this object.</summary>
        public override int GetHashCode()
        {
            return Number.FloatToIntBits(Boost)
              ^ slop
              ^ TermArraysHashCode()
              ^ positions.GetHashCode()
              ^ 0x4AC65113;
        }

        // Breakout calculation of the termArrays hashcode
        private int TermArraysHashCode()
        {
            var hashCode = 1;
            foreach (var termArray in termArrays)
            {
                hashCode = 31 * hashCode
                    + (termArray == null ? 0 : termArray.GetHashCode());
            }
            return hashCode;
        }

        // Breakout calculation of the termArrays Equals
        private bool TermArraysEquals(List<Term[]> termArrays1, List<Term[]> termArrays2)
        {
            if (termArrays1.Count != termArrays2.Count)
            {
                return false;
            }
            var iterator1 = termArrays1.GetEnumerator();
            var iterator2 = termArrays2.GetEnumerator();
            while (iterator1.MoveNext())
            {
                var termArray1 = iterator1.Current;
                var termArray2 = iterator2.Current;
                if (!(termArray1 == null ? termArray2 == null : TermEquals(termArray1, termArray2)))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool TermEquals(Array array1, Array array2)
        {
            var result = false;
            if ((array1 == null) && (array2 == null))
                result = true;
            else if ((array1 != null) && (array2 != null))
            {
                if (array1.Length == array2.Length)
                {
                    var length = array1.Length;
                    result = true;
                    for (var index = 0; index < length; index++)
                    {
                        if (!(array1.GetValue(index).Equals(array2.GetValue(index))))
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }
            return result;
        }
    }

    internal class UnionDocsAndPositionsEnum : DocsAndPositionsEnum
    {

        private sealed class DocsQueue : Util.PriorityQueue<DocsAndPositionsEnum>
        {
            internal DocsQueue(ICollection<DocsAndPositionsEnum> docsEnums)
                : base(docsEnums.Count)
            {

                var i = docsEnums.GetEnumerator();
                while (i.MoveNext())
                {
                    var postings = i.Current;
                    if (postings.NextDoc() != NO_MORE_DOCS)
                    {
                        Add(postings);
                    }
                }
            }

            public override bool LessThan(DocsAndPositionsEnum a, DocsAndPositionsEnum b)
            {
                return a.DocID < b.DocID;
            }
        }

        private sealed class IntQueue
        {
            private int _arraySize = 16;
            private int _index = 0;
            private int _lastIndex = 0;
            private int[] _array;

            internal IntQueue()
            {
                _array = new int[_arraySize];
            }

            internal void Add(int i)
            {
                if (_lastIndex == _arraySize)
                    GrowArray();

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

            internal int Size
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
        private DocsQueue _queue;
        private IntQueue _posList;
        private long cost;

        public UnionDocsAndPositionsEnum(IBits liveDocs, AtomicReaderContext context, Term[] terms, IDictionary<Term, TermContext> termContexts, TermsEnum termsEnum)
        {
            ICollection<DocsAndPositionsEnum> docsEnums = new LinkedList<DocsAndPositionsEnum>();
            foreach (var term in terms)
            {
                var termState = termContexts[term].Get(context.ord);
                if (termState == null)
                {
                    // Term doesn't exist in reader
                    continue;
                }
                termsEnum.SeekExact(term.bytes, termState);
                var postings = termsEnum.DocsAndPositions(liveDocs, null, FLAG_NONE);
                if (postings == null)
                {
                    // term does exist, but has no positions
                    throw new InvalidOperationException("field \"" + term.Field + "\" was indexed without position data; cannot run PhraseQuery (term=" + term.Text + ")");
                }
                cost += postings.Cost;
                docsEnums.Add(postings);
            }

            _queue = new DocsQueue(docsEnums);
            _posList = new IntQueue();
        }

        public override sealed int NextDoc()
        {
            if (_queue.Size == 0)
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

                var freq = postings.Freq;
                for (var i = 0; i < freq; i++)
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
            } while (_queue.Size > 0 && _queue.Top().DocID == _doc);

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
            get { return null; }
        }

        public override sealed int Advance(int target)
        {
            while (_queue.Top() != null && target > _queue.Top().DocID)
            {
                var postings = _queue.Pop();
                if (postings.Advance(target) != NO_MORE_DOCS)
                {
                    _queue.Add(postings);
                }
            }
            return NextDoc();
        }

        public override int Freq
        {
            get { return _freq; }
        }

        public override int DocID
        {
            get { return _doc; }
        }

        public override long Cost
        {
            get { return cost; }
        }
    }
}