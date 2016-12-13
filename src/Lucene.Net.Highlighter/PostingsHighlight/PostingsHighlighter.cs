using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;

namespace Lucene.Net.Search.PostingsHighlight
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

    /// <summary>
    /// Simple highlighter that does not analyze fields nor use
    /// term vectors. Instead it requires 
    /// <see cref="IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/>.
    /// <para/>
    /// PostingsHighlighter treats the single original document as the whole corpus, and then scores individual
    /// passages as if they were documents in this corpus. It uses a <see cref="BreakIterator"/> to find 
    /// passages in the text; by default it breaks using <see cref="IcuBreakIterator"/> (for sentence breaking). 
    /// It then iterates in parallel (merge sorting by offset) through
    /// the positions of all terms from the query, coalescing those hits that occur in a single passage
    /// into a <see cref="Passage"/>, and then scores each Passage using a separate <see cref="PassageScorer"/>.
    /// Passages are finally formatted into highlighted snippets with a <see cref="PassageFormatter"/>.
    /// <para/>
    /// You can customize the behavior by subclassing this highlighter, some important hooks:
    /// <list type="bullet">
    ///     <item><see cref="GetBreakIterator(string)"/>: Customize how the text is divided into passages.</item>
    ///     <item><see cref="GetScorer(string)"/>: Customize how passages are ranked.</item>
    ///     <item><see cref="GetFormatter(string)"/>: Customize how snippets are formatted.</item>
    ///     <item><see cref="GetIndexAnalyzer(string)"/>: Enable highlighting of MultiTermQuerys such as <see cref="WildcardQuery"/>.</item>
    /// </list>
    /// <para/>
    /// <b>WARNING</b>: The code is very new and probably still has some exciting bugs!
    /// <para/>
    /// Example usage:
    /// <code>
    ///     // configure field with offsets at index time
    ///     FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
    ///     offsetsType.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
    ///     Field body = new Field("body", "foobar", offsetsType);
    ///     
    ///     // retrieve highlights at query time 
    ///     PostingsHighlighter highlighter = new PostingsHighlighter();
    ///     Query query = new TermQuery(new Term("body", "highlighting"));
    ///     TopDocs topDocs = searcher.Search(query, n);
    ///     string highlights[] = highlighter.Highlight("body", query, searcher, topDocs);
    /// </code>
    /// <para/>
    /// This is thread-safe, and can be used across different readers.
    /// @lucene.experimental
    /// </summary>
    public class PostingsHighlighter
    {
        // TODO: maybe allow re-analysis for tiny fields? currently we require offsets,
        // but if the analyzer is really fast and the field is tiny, this might really be
        // unnecessary.

        /// <summary>for rewriting: we don't want slow processing from MTQs</summary>
        private static readonly IndexReader EMPTY_INDEXREADER = new MultiReader();

        /// <summary>
        /// Default maximum content size to process. Typically snippets
        /// closer to the beginning of the document better summarize its content
        /// </summary>
        public static readonly int DEFAULT_MAX_LENGTH = 10000;

        private readonly int maxLength;

        /// <summary>
        /// Set the first time <see cref="GetFormatter(string)"/> is called,
        /// and then reused.
        /// </summary>
        private PassageFormatter defaultFormatter;

        /** Set the first time {@link #getScorer} is called,
         *  and then reused. */
        private PassageScorer defaultScorer;

        /// <summary>
        /// Creates a new highlighter with <see cref="DEFAULT_MAX_LENGTH"/>.
        /// </summary>
        public PostingsHighlighter()
            : this(DEFAULT_MAX_LENGTH)
        {
        }

        /// <summary>
        /// Creates a new highlighter, specifying maximum content length.
        /// </summary>
        /// <param name="maxLength">maximum content size to process.</param>
        /// <exception cref="ArgumentException">if <paramref name="maxLength"/> is negative or <c>int.MaxValue</c></exception>
        public PostingsHighlighter(int maxLength)
        {
            if (maxLength < 0 || maxLength == int.MaxValue)
            {
                // two reasons: no overflow problems in BreakIterator.preceding(offset+1),
                // our sentinel in the offsets queue uses this value to terminate.
                throw new ArgumentException("maxLength must be < Integer.MAX_VALUE");
            }
            this.maxLength = maxLength;
        }

        /// <summary>
        /// Returns the <see cref="BreakIterator"/> to use for
        /// dividing text into passages.  This instantiates an
        /// <see cref="IcuBreakIterator"/> by default;
        /// subclasses can override to customize.
        /// </summary>
        protected virtual BreakIterator GetBreakIterator(string field)
        {
            return new IcuBreakIterator(Icu.BreakIterator.UBreakIteratorType.SENTENCE, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the <see cref="PassageFormatter"/> to use for
        /// formatting passages into highlighted snippets.  This
        /// returns a new <see cref="PassageFormatter"/> by default;
        /// subclasses can override to customize.
        /// </summary>
        protected virtual PassageFormatter GetFormatter(string field)
        {
            if (defaultFormatter == null)
            {
                defaultFormatter = new DefaultPassageFormatter();
            }
            return defaultFormatter;
        }

        /// <summary>
        /// Returns the <see cref="PassageScorer"/> to use for
        /// ranking passages.  This
        /// returns a new <see cref="PassageScorer"/> by default;
        /// subclasses can override to customize.
        /// </summary>
        protected virtual PassageScorer GetScorer(string field)
        {
            if (defaultScorer == null)
            {
                defaultScorer = new PassageScorer();
            }
            return defaultScorer;
        }

        /// <summary>
        /// Highlights the top passages from a single field.
        /// </summary>
        /// <param name="field">field name to highlight. Must have a stored string value and also be indexed with offsets.</param>
        /// <param name="query">query to highlight.</param>
        /// <param name="searcher">searcher that was previously used to execute the query.</param>
        /// <param name="topDocs">TopDocs containing the summary result documents to highlight.</param>
        /// <returns>
        /// Array of formatted snippets corresponding to the documents in <paramref name="topDocs"/>.
        /// If no highlights were found for a document, the
        /// first sentence for the field will be returned.
        /// </returns>
        /// <exception cref="IOException">if an I/O error occurred during processing</exception>
        /// <exception cref="ArgumentException">if <paramref name="field"/> was indexed without <see cref="IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/></exception>
        public virtual string[] Highlight(string field, Query query, IndexSearcher searcher, TopDocs topDocs)
        {
            return Highlight(field, query, searcher, topDocs, 1);
        }

        /// <summary>
        /// Highlights the top-N passages from a single field.
        /// </summary>
        /// <param name="field">
        /// field name to highlight.
        /// Must have a stored string value and also be indexed with offsets.
        /// </param>
        /// <param name="query">query to highlight.</param>
        /// <param name="searcher">searcher that was previously used to execute the query.</param>
        /// <param name="topDocs">TopDocs containing the summary result documents to highlight.</param>
        /// <param name="maxPassages">The maximum number of top-N ranked passages used to form the highlighted snippets.</param>
        /// <returns>
        /// Array of formatted snippets corresponding to the documents in <paramref name="topDocs"/>.
        /// If no highlights were found for a document, the
        /// first <paramref name="maxPassages"/> sentences from the
        /// field will be returned.
        /// </returns>
        /// <exception cref="IOException">if an I/O error occurred during processing</exception>
        /// <exception cref="ArgumentException">Illegal if <paramref name="field"/> was indexed without <see cref="IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/></exception>
        public virtual string[] Highlight(string field, Query query, IndexSearcher searcher, TopDocs topDocs, int maxPassages)
        {
            IDictionary<string, string[]> res = HighlightFields(new string[] { field }, query, searcher, topDocs, new int[] { maxPassages });
            string[] result;
            res.TryGetValue(field, out result);
            return result;
        }

        /// <summary>
        /// Highlights the top passages from multiple fields.
        /// <para/>
        /// Conceptually, this behaves as a more efficient form of:
        /// <code>
        /// IDictionary&lt;string, string[]&gt; m = new Dictionary&lt;string, string[]&gt;();
        /// foreach (string field in fields)
        /// {
        ///     m[field] = Highlight(field, query, searcher, topDocs);
        /// }
        /// return m;
        /// </code>
        /// </summary>
        /// <param name="fields">field names to highlight. Must have a stored string value and also be indexed with offsets.</param>
        /// <param name="query">query to highlight.</param>
        /// <param name="searcher">searcher that was previously used to execute the query.</param>
        /// <param name="topDocs">TopDocs containing the summary result documents to highlight.</param>
        /// <returns>
        /// <see cref="IDictionary{string, string[]}"/> keyed on field name, containing the array of formatted snippets 
        /// corresponding to the documents in <paramref name="topDocs"/>.
        /// If no highlights were found for a document, the
        /// first sentence from the field will be returned.
        /// </returns>
        /// <exception cref="IOException">if an I/O error occurred during processing</exception>
        /// <exception cref="ArgumentException">if <paramref name="field"/> was indexed without <see cref="IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/></exception>
        public virtual IDictionary<string, string[]> HighlightFields(string[] fields, Query query, IndexSearcher searcher, TopDocs topDocs)
        {
            int[] maxPassages = new int[fields.Length];
            Arrays.Fill(maxPassages, 1);
            return HighlightFields(fields, query, searcher, topDocs, maxPassages);
        }

        /// <summary>
        /// Highlights the top-N passages from multiple fields.
        /// <para/>
        /// Conceptually, this behaves as a more efficient form of:
        /// <code>
        /// IDictionary&lt;string, string[]&gt; m = new Dictionary&lt;string, string[]&gt;();
        /// foreach (string field in fields)
        /// {
        ///     m[field] = Highlight(field, query, searcher, topDocs, maxPassages);
        /// }
        /// return m;
        /// </code>
        /// </summary>
        /// <param name="fields">field names to highlight. Must have a stored string value and also be indexed with offsets.</param>
        /// <param name="query">query to highlight.</param>
        /// <param name="searcher">searcher that was previously used to execute the query.</param>
        /// <param name="topDocs">TopDocs containing the summary result documents to highlight.</param>
        /// <param name="maxPassages">The maximum number of top-N ranked passages per-field used to form the highlighted snippets.</param>
        /// <returns>
        /// <see cref="IDictionary{string, string[]}"/> keyed on field name, containing the array of formatted snippets
        /// corresponding to the documents in <paramref name="topDocs"/>.
        /// If no highlights were found for a document, the
        /// first <paramref name="maxPassages"/> sentences from the
        /// field will be returned.
        /// </returns>
        /// <exception cref="IOException">if an I/O error occurred during processing</exception>
        /// <exception cref="ArgumentException">if <paramref name="field"/> was indexed without <see cref="IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/></exception>
        public virtual IDictionary<string, string[]> HighlightFields(string[] fields, Query query, IndexSearcher searcher, TopDocs topDocs, int[] maxPassages)
        {
            ScoreDoc[] scoreDocs = topDocs.ScoreDocs;
            int[] docids = new int[scoreDocs.Length];
            for (int i = 0; i < docids.Length; i++)
            {
                docids[i] = scoreDocs[i].Doc;
            }

            return HighlightFields(fields, query, searcher, docids, maxPassages);
        }

        /// <summary>
        /// Highlights the top-N passages from multiple fields,
        /// for the provided int[] docids.
        /// </summary>
        /// <param name="fieldsIn">field names to highlight. Must have a stored string value and also be indexed with offsets.</param>
        /// <param name="query">query to highlight.</param>
        /// <param name="searcher">searcher that was previously used to execute the query.</param>
        /// <param name="docidsIn">containing the document IDs to highlight.</param>
        /// <param name="maxPassagesIn">The maximum number of top-N ranked passages per-field used to form the highlighted snippets.</param>
        /// <returns>
        /// <see cref="IDictionary{string, string[]}"/> keyed on field name, containing the array of formatted snippets 
        /// corresponding to the documents in <paramref name="docidsIn"/>.
        /// If no highlights were found for a document, the
        /// first <paramref name="maxPassages"/> from the field will
        /// be returned.
        /// </returns>
        /// <exception cref="IOException">if an I/O error occurred during processing</exception>
        /// <exception cref="ArgumentException">if <paramref name="field"/> was indexed without <see cref="IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/></exception>
        public virtual IDictionary<string, string[]> HighlightFields(string[] fieldsIn, Query query, IndexSearcher searcher, int[] docidsIn, int[] maxPassagesIn)
        {
            IDictionary<string, string[]> snippets = new Dictionary<string, string[]>();
            foreach (var ent in HighlightFieldsAsObjects(fieldsIn, query, searcher, docidsIn, maxPassagesIn))
            {
                object[] snippetObjects = ent.Value;
                string[] snippetStrings = new string[snippetObjects.Length];
                snippets[ent.Key] = snippetStrings;
                for (int i = 0; i < snippetObjects.Length; i++)
                {
                    object snippet = snippetObjects[i];
                    if (snippet != null)
                    {
                        snippetStrings[i] = snippet.ToString();
                    }
                }
            }

            return snippets;
        }

        internal class InPlaceMergeSorterAnonymousHelper : InPlaceMergeSorter
        {
            private readonly string[] fields;
            private readonly int[] maxPassages;
            public InPlaceMergeSorterAnonymousHelper(string[] fields, int[] maxPassages)
            {
                this.fields = fields;
                this.maxPassages = maxPassages;
            }

            protected override void Swap(int i, int j)
            {
                string tmp = fields[i];
                fields[i] = fields[j];
                fields[j] = tmp;
                int tmp2 = maxPassages[i];
                maxPassages[i] = maxPassages[j];
                maxPassages[j] = tmp2;
            }

            protected override int Compare(int i, int j)
            {
                return fields[i].CompareToOrdinal(fields[j]);
            }
        }

        /// <summary>
        /// Expert: highlights the top-N passages from multiple fields,
        /// for the provided int[] docids, to custom object as
        /// returned by the <see cref="PassageFormatter"/>.  Use
        /// this API to render to something other than <see cref="string"/>.
        /// </summary>
        /// <param name="fieldsIn">field names to highlight. Must have a stored string value and also be indexed with offsets.</param>
        /// <param name="query">query to highlight.</param>
        /// <param name="searcher">searcher that was previously used to execute the query.</param>
        /// <param name="docidsIn">containing the document IDs to highlight.</param>
        /// <param name="maxPassagesIn">The maximum number of top-N ranked passages per-field used to form the highlighted snippets.</param>
        /// <returns>
        /// <see cref="IDictionary{string, object[]}"/> keyed on field name, containing the array of formatted snippets
        /// corresponding to the documents in <paramref name="docidsIn"/>.
        /// If no highlights were found for a document, the
        /// first <paramref name="maxPassagesIn"/> from the field will
        /// be returned.
        /// </returns>
        /// <exception cref="IOException">if an I/O error occurred during processing</exception>
        /// <exception cref="ArgumentException">if <paramref name="field"/> was indexed without <see cref="IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/></exception>
        protected internal virtual IDictionary<string, object[]> HighlightFieldsAsObjects(string[] fieldsIn, Query query, IndexSearcher searcher, int[] docidsIn, int[] maxPassagesIn)
        {
            if (fieldsIn.Length < 1)
            {
                throw new ArgumentException("fieldsIn must not be empty");
            }
            if (fieldsIn.Length != maxPassagesIn.Length)
            {
                throw new ArgumentException("invalid number of maxPassagesIn");
            }
            IndexReader reader = searcher.IndexReader;
            Query rewritten = Rewrite(query);
            SortedSet<Term> queryTerms = new SortedSet<Term>();//new TreeSet<>();
            rewritten.ExtractTerms(queryTerms);

            IndexReaderContext readerContext = reader.Context;
            IList<AtomicReaderContext> leaves = readerContext.Leaves;

            // Make our own copies because we sort in-place:
            int[] docids = new int[docidsIn.Length];
            System.Array.Copy(docidsIn, 0, docids, 0, docidsIn.Length);
            string[] fields = new string[fieldsIn.Length];
            System.Array.Copy(fieldsIn, 0, fields, 0, fieldsIn.Length);
            int[] maxPassages = new int[maxPassagesIn.Length];
            System.Array.Copy(maxPassagesIn, 0, maxPassages, 0, maxPassagesIn.Length);

            // sort for sequential io
            ArrayUtil.TimSort(docids);
            new InPlaceMergeSorterAnonymousHelper(fields, maxPassages).Sort(0, fields.Length);

            // pull stored data:
            string[][] contents = LoadFieldValues(searcher, fields, docids, maxLength);

            IDictionary<string, object[]> highlights = new Dictionary<string, object[]>();
            for (int i = 0; i < fields.Length; i++)
            {
                string field = fields[i];
                int numPassages = maxPassages[i];
                Term floor = new Term(field, "");
                Term ceiling = new Term(field, UnicodeUtil.BIG_TERM);
                SortedSet<Term> fieldTerms = queryTerms.GetViewBetween(floor, ceiling); //SubSet(floor, ceiling);
                // TODO: should we have some reasonable defaults for term pruning? (e.g. stopwords)

                // Strip off the redundant field:
                BytesRef[] terms = new BytesRef[fieldTerms.Count];
                int termUpto = 0;
                foreach (Term term in fieldTerms)
                {
                    terms[termUpto++] = term.Bytes;
                }
                IDictionary<int, object> fieldHighlights = HighlightField(field, contents[i], GetBreakIterator(field), terms, docids, leaves, numPassages, query);

                object[] result = new object[docids.Length];
                for (int j = 0; j < docidsIn.Length; j++)
                {
                    //result[j] = fieldHighlights.get(docidsIn[j]);
                    fieldHighlights.TryGetValue(docidsIn[j], out result[j]);
                }
                highlights[field] = result;
            }
            return highlights;
        }

        /// <summary>
        /// Loads the string values for each field X docID to be
        /// highlighted.  By default this loads from stored
        /// fields, but a subclass can change the source.  This
        /// method should allocate the string[fields.length][docids.length]
        /// and fill all values.  The returned strings must be
        /// identical to what was indexed.
        /// </summary>
        protected virtual string[][] LoadFieldValues(IndexSearcher searcher, string[] fields, int[] docids, int maxLength)
        {
            string[][] contents = RectangularArrays.ReturnRectangularStringArray(fields.Length, docids.Length);
            char[] valueSeparators = new char[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                valueSeparators[i] = GetMultiValuedSeparator(fields[i]);
            }
            LimitedStoredFieldVisitor visitor = new LimitedStoredFieldVisitor(fields, valueSeparators, maxLength);
            for (int i = 0; i < docids.Length; i++)
            {
                searcher.Doc(docids[i], visitor);
                for (int j = 0; j < fields.Length; j++)
                {
                    contents[j][i] = visitor.GetValue(j).ToString();
                }
                visitor.Reset();
            }
            return contents;
        }

        /// <summary>
        /// Returns the logical separator between values for multi-valued fields.
        /// The default value is a space character, which means passages can span across values,
        /// but a subclass can override, for example with <c>U+2029 PARAGRAPH SEPARATOR (PS)</c>
        /// if each value holds a discrete passage for highlighting.
        /// </summary>
        protected virtual char GetMultiValuedSeparator(string field)
        {
            return ' ';
        }

        /// <summary>
        /// Returns the analyzer originally used to index the content for <paramref name="field"/>.
        /// <para/>
        /// This is used to highlight some <see cref="MultiTermQuery"/>s.
        /// </summary>
        /// <param name="field"></param>
        /// <returns><see cref="Analyzer"/> or null (the default, meaning no special multi-term processing)</returns>
        protected virtual Analyzer GetIndexAnalyzer(string field)
        {
            return null;
        }

        private IDictionary<int, object> HighlightField(string field, string[] contents, BreakIterator bi, BytesRef[] terms, int[] docids, IList<AtomicReaderContext> leaves, int maxPassages, Query query)
        {
            IDictionary<int, object> highlights = new Dictionary<int, object>();

            PassageFormatter fieldFormatter = GetFormatter(field);
            if (fieldFormatter == null)
            {
                throw new NullReferenceException("PassageFormatter cannot be null");
            }

            // check if we should do any multiterm processing
            Analyzer analyzer = GetIndexAnalyzer(field);
            CharacterRunAutomaton[] automata = new CharacterRunAutomaton[0];
            if (analyzer != null)
            {
                automata = MultiTermHighlighting.ExtractAutomata(query, field);
            }

            // resize 'terms', where the last term is the multiterm matcher
            if (automata.Length > 0)
            {
                BytesRef[] newTerms = new BytesRef[terms.Length + 1];
                System.Array.Copy(terms, 0, newTerms, 0, terms.Length);
                terms = newTerms;
            }

            // we are processing in increasing docid order, so we only need to reinitialize stuff on segment changes
            // otherwise, we will just advance() existing enums to the new document in the same segment.
            DocsAndPositionsEnum[] postings = null;
            TermsEnum termsEnum = null;
            int lastLeaf = -1;

            for (int i = 0; i < docids.Length; i++)
            {
                string content = contents[i];
                if (content.Length == 0)
                {
                    continue; // nothing to do
                }
                bi.SetText(content);
                int doc = docids[i];
                int leaf = ReaderUtil.SubIndex(doc, leaves);
                AtomicReaderContext subContext = leaves[leaf];
                AtomicReader r = subContext.AtomicReader;

                Debug.Assert(leaf >= lastLeaf); // increasing order

                // if the segment has changed, we must initialize new enums.
                if (leaf != lastLeaf)
                {
                    Terms t = r.Terms(field);
                    if (t != null)
                    {
                        termsEnum = t.Iterator(null);
                        postings = new DocsAndPositionsEnum[terms.Length];
                    }
                }
                if (termsEnum == null)
                {
                    continue; // no terms for this field, nothing to do
                }

                // if there are multi-term matches, we have to initialize the "fake" enum for each document
                if (automata.Length > 0)
                {
                    DocsAndPositionsEnum dp = MultiTermHighlighting.GetDocsEnum(analyzer.TokenStream(field, content), automata);
                    dp.Advance(doc - subContext.DocBase);
                    postings[terms.Length - 1] = dp; // last term is the multiterm matcher
                }

                Passage[] passages = HighlightDoc(field, terms, content.Length, bi, doc - subContext.DocBase, termsEnum, postings, maxPassages);

                if (passages.Length == 0)
                {
                    // no passages were returned, so ask for a default summary
                    passages = GetEmptyHighlight(field, bi, maxPassages);
                }

                if (passages.Length > 0)
                {
                    highlights[doc] = fieldFormatter.Format(passages, content);
                }

                lastLeaf = leaf;
            }

            return highlights;
        }

        internal class HighlightDocComparerAnonymousHelper1 : IComparer<Passage>
        {
            public int Compare(Passage left, Passage right)
            {
                if (left.score < right.score)
                {
                    return -1;
                }
                else if (left.score > right.score)
                {
                    return 1;
                }
                else
                {
                    return left.startOffset - right.startOffset;
                }
            }
        }

        internal class HighlightDocComparerAnonymousHelper2 : IComparer<Passage>
        {
            public int Compare(Passage left, Passage right)
            {
                return left.startOffset - right.startOffset;
            }
        }

        // algorithm: treat sentence snippets as miniature documents
        // we can intersect these with the postings lists via BreakIterator.preceding(offset),s
        // score each sentence as norm(sentenceStartOffset) * sum(weight * tf(freq))
        private Passage[] HighlightDoc(string field, BytesRef[] terms, int contentLength, BreakIterator bi, int doc,
            TermsEnum termsEnum, DocsAndPositionsEnum[] postings, int n)
        {
            PassageScorer scorer = GetScorer(field);
            if (scorer == null)
            {
                throw new NullReferenceException("PassageScorer cannot be null");
            }
            Support.PriorityQueue<OffsetsEnum> pq = new Support.PriorityQueue<OffsetsEnum>();
            float[] weights = new float[terms.Length];
            // initialize postings
            for (int i = 0; i < terms.Length; i++)
            {
                DocsAndPositionsEnum de = postings[i];
                int pDoc;
                if (de == EMPTY)
                {
                    continue;
                }
                else if (de == null)
                {
                    postings[i] = EMPTY; // initially
                    if (!termsEnum.SeekExact(terms[i]))
                    {
                        continue; // term not found
                    }
                    de = postings[i] = termsEnum.DocsAndPositions(null, null, DocsAndPositionsEnum.FLAG_OFFSETS);
                    if (de == null)
                    {
                        // no positions available
                        throw new ArgumentException("field '" + field + "' was indexed without offsets, cannot highlight");
                    }
                    pDoc = de.Advance(doc);
                }
                else
                {
                    pDoc = de.DocID();
                    if (pDoc < doc)
                    {
                        pDoc = de.Advance(doc);
                    }
                }

                if (doc == pDoc)
                {
                    weights[i] = scorer.Weight(contentLength, de.Freq());
                    de.NextPosition();
                    pq.Add(new OffsetsEnum(de, i));
                }
            }

            pq.Add(new OffsetsEnum(EMPTY, int.MaxValue)); // a sentinel for termination

            Support.PriorityQueue<Passage> passageQueue = new Support.PriorityQueue<Passage>(n, new HighlightDocComparerAnonymousHelper1());
            Passage current = new Passage();

            OffsetsEnum off;
            while ((off = pq.Poll()) != null)
            {
                DocsAndPositionsEnum dp = off.dp;
                int start = dp.StartOffset();
                if (start == -1)
                {
                    throw new ArgumentException("field '" + field + "' was indexed without offsets, cannot highlight");
                }
                int end = dp.EndOffset();
                // LUCENE-5166: this hit would span the content limit... however more valid 
                // hits may exist (they are sorted by start). so we pretend like we never 
                // saw this term, it won't cause a passage to be added to passageQueue or anything.
                Debug.Assert(EMPTY.StartOffset() == int.MaxValue);
                if (start < contentLength && end > contentLength)
                {
                    continue;
                }
                if (start >= current.endOffset)
                {
                    if (current.startOffset >= 0)
                    {
                        // finalize current
                        current.score *= scorer.Norm(current.startOffset);
                        // new sentence: first add 'current' to queue 
                        if (passageQueue.Count == n && current.score < passageQueue.Peek().score)
                        {
                            current.Reset(); // can't compete, just reset it
                        }
                        else
                        {
                            passageQueue.Offer(current);
                            if (passageQueue.Count > n)
                            {
                                current = passageQueue.Poll();
                                current.Reset();
                            }
                            else
                            {
                                current = new Passage();
                            }
                        }
                    }
                    // if we exceed limit, we are done
                    if (start >= contentLength)
                    {
                        Passage[] passages = passageQueue.ToArray();
                        foreach (Passage p in passages)
                        {
                            p.Sort();
                        }
                        // sort in ascending order
                        ArrayUtil.TimSort(passages, new HighlightDocComparerAnonymousHelper2());
                        return passages;
                    }
                    // advance breakiterator
                    Debug.Assert(BreakIterator.DONE < 0);
                    current.startOffset = Math.Max(bi.Preceding(start + 1), 0);
                    current.endOffset = Math.Min(bi.Next(), contentLength);
                }
                int tf = 0;
                while (true)
                {
                    tf++;
                    BytesRef term = terms[off.id];
                    if (term == null)
                    {
                        // multitermquery match, pull from payload
                        term = off.dp.Payload;
                        Debug.Assert(term != null);
                    }
                    current.AddMatch(start, end, term);
                    if (off.pos == dp.Freq())
                    {
                        break; // removed from pq
                    }
                    else
                    {
                        off.pos++;
                        dp.NextPosition();
                        start = dp.StartOffset();
                        end = dp.EndOffset();
                    }
                    if (start >= current.endOffset || end > contentLength)
                    {
                        pq.Offer(off);
                        break;
                    }
                }
                current.score += weights[off.id] * scorer.Tf(tf, current.endOffset - current.startOffset);
            }

            // Dead code but compiler disagrees:
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Called to summarize a document when no hits were
        /// found.  By default this just returns the first
        /// <paramref name="maxPassages"/> sentences; subclasses can override
        /// to customize.
        /// </summary>
        protected virtual Passage[] GetEmptyHighlight(string fieldName, BreakIterator bi, int maxPassages)
        {
            // BreakIterator should be un-next'd:
            List<Passage> passages = new List<Passage>();
            int pos = bi.Current;
            Debug.Assert(pos == 0);
            while (passages.Count < maxPassages)
            {
                int next = bi.Next();
                if (next == BreakIterator.DONE)
                {
                    break;
                }
                Passage passage = new Passage();
                passage.score = float.NaN;
                passage.startOffset = pos;
                passage.endOffset = next;
                passages.Add(passage);
                pos = next;
            }

            return passages.ToArray(/*new Passage[passages.size()]*/);
        }

        internal class OffsetsEnum : IComparable<OffsetsEnum>
        {
            internal DocsAndPositionsEnum dp;
            internal int pos;
            internal int id;

            internal OffsetsEnum(DocsAndPositionsEnum dp, int id)
            {
                this.dp = dp;
                this.id = id;
                this.pos = 1;
            }

            public virtual int CompareTo(OffsetsEnum other)
            {
                try
                {
                    int off = dp.StartOffset();
                    int otherOff = other.dp.StartOffset();
                    if (off == otherOff)
                    {
                        return id - other.id;
                    }
                    else
                    {
                        return off.CompareTo(otherOff);
                    }
                }
                catch (IOException e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }

        private static readonly DocsAndPositionsEnum EMPTY = new DocsAndPositionsEnumAnonymousHelper();

        /// <summary>
        /// we rewrite against an empty indexreader: as we don't want things like
        /// rangeQueries that don't summarize the document
        /// </summary>
        private class DocsAndPositionsEnumAnonymousHelper : DocsAndPositionsEnum
        {
            public override int NextPosition()
            {
                return 0;
            }

            public override int StartOffset()
            {
                return int.MaxValue;
            }

            public override int EndOffset()
            {
                return int.MaxValue;
            }

            public override BytesRef Payload
            {
                get { return null; }
            }

            public override int Freq()
            {
                return 0;
            }

            public override int DocID()
            {
                return NO_MORE_DOCS;
            }

            public override int NextDoc()
            {
                return NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                return NO_MORE_DOCS;
            }

            public override long Cost()
            {
                return 0;
            }
        }

        private static Query Rewrite(Query original)
        {
            Query query = original;
            for (Query rewrittenQuery = query.Rewrite(EMPTY_INDEXREADER); rewrittenQuery != query;
                rewrittenQuery = query.Rewrite(EMPTY_INDEXREADER))
            {
                query = rewrittenQuery;
            }
            return query;
        }

        private class LimitedStoredFieldVisitor : StoredFieldVisitor
        {
            private readonly string[] fields;
            private readonly char[] valueSeparators;
            private readonly int maxLength;
            private readonly StringBuilder[] builders;
            private int currentField = -1;

            public LimitedStoredFieldVisitor(string[] fields, char[] valueSeparators, int maxLength)
            {
                Debug.Assert(fields.Length == valueSeparators.Length);
                this.fields = fields;
                this.valueSeparators = valueSeparators;
                this.maxLength = maxLength;
                builders = new StringBuilder[fields.Length];
                for (int i = 0; i < builders.Length; i++)
                {
                    builders[i] = new StringBuilder();
                }
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                Debug.Assert(currentField >= 0);
                StringBuilder builder = builders[currentField];
                if (builder.Length > 0 && builder.Length < maxLength)
                {
                    builder.Append(valueSeparators[currentField]);
                }
                if (builder.Length + value.Length > maxLength)
                {
                    builder.Append(value, 0, maxLength - builder.Length);
                }
                else
                {
                    builder.Append(value);
                }
            }

            public override Status NeedsField(FieldInfo fieldInfo)
            {
                currentField = Array.BinarySearch(fields, fieldInfo.Name);
                if (currentField < 0)
                {
                    return Status.NO;
                }
                else if (builders[currentField].Length > maxLength)
                {
                    return fields.Length == 1 ? Status.STOP : Status.NO;
                }
                return Status.YES;
            }

            internal string GetValue(int i)
            {
                return builders[i].ToString();
            }

            internal void Reset()
            {
                currentField = -1;
                for (int i = 0; i < fields.Length; i++)
                {
                    builders[i].Length = 0;
                }
            }
        }
    }
}
