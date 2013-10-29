/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Search.Similarities;

namespace Lucene.Net.Index.Memory
{
    /// <summary>
    /// High-performance single-document main memory Apache Lucene fulltext search index. 
    /// 
    /// <h4>Overview</h4>
    /// 
    /// This class is a replacement/substitute for a large subset of
    /// {@link RAMDirectory} functionality. It is designed to
    /// enable maximum efficiency for on-the-fly matchmaking combining structured and 
    /// fuzzy fulltext search in realtime streaming applications such as Nux XQuery based XML 
    /// message queues, publish-subscribe systems for Blogs/newsfeeds, text chat, data acquisition and 
    /// distribution systems, application level routers, firewalls, classifiers, etc. 
    /// Rather than targeting fulltext search of infrequent queries over huge persistent 
    /// data archives (historic search), this class targets fulltext search of huge 
    /// numbers of queries over comparatively small transient realtime data (prospective 
    /// search). 
    /// For example as in 
    /// <pre>
    /// float score = search(String text, Query query)
    /// </pre>
    /// <p/>
    /// Each instance can hold at most one Lucene "document", with a document containing
    /// zero or more "fields", each field having a name and a fulltext value. The
    /// fulltext value is tokenized (split and transformed) into zero or more index terms 
    /// (aka words) on <c>addField()</c>, according to the policy implemented by an
    /// Analyzer. For example, Lucene analyzers can split on whitespace, normalize to lower case
    /// for case insensitivity, ignore common terms with little discriminatory value such as "he", "in", "and" (stop
    /// words), reduce the terms to their natural linguistic root form such as "fishing"
    /// being reduced to "fish" (stemming), resolve synonyms/inflexions/thesauri 
    /// (upon indexing and/or querying), etc. For details, see
    /// <a target="_blank" href="http://today.java.net/pub/a/today/2003/07/30/LuceneIntro.html">Lucene Analyzer Intro</a>.
    /// <p/>
    /// Arbitrary Lucene queries can be run against this class - see <a target="_blank" 
    /// href="../../../../../../../queryparsersyntax.html">Lucene Query Syntax</a>
    /// as well as <a target="_blank" 
    /// href="http://today.java.net/pub/a/today/2003/11/07/QueryParserRules.html">Query Parser Rules</a>.
    /// Note that a Lucene query selects on the field names and associated (indexed) 
    /// tokenized terms, not on the original fulltext(s) - the latter are not stored 
    /// but rather thrown away immediately after tokenization.
    /// <p/>
    /// For some interesting background information on search technology, see Bob Wyman's
    /// <a target="_blank" 
    /// href="http://bobwyman.pubsub.com/main/2005/05/mary_hodder_poi.html">Prospective Search</a>, 
    /// Jim Gray's
    /// <a target="_blank" href="http://www.acmqueue.org/modules.php?name=Content&amp;pa=showpage&amp;pid=293&amp;page=4">
    /// A Call to Arms - Custom subscriptions</a>, and Tim Bray's
    /// <a target="_blank" 
    /// href="http://www.tbray.org/ongoing/When/200x/2003/07/30/OnSearchTOC">On Search, the Series</a>.
    /// 
    /// 
    /// <h4>Example Usage</h4> 
    /// 
    /// <pre>
    /// Analyzer analyzer = PatternAnalyzer.DEFAULT_ANALYZER;
    /// //Analyzer analyzer = new SimpleAnalyzer();
    /// MemoryIndex index = new MemoryIndex();
    /// index.addField("content", "Readings about Salmons and other select Alaska fishing Manuals", analyzer);
    /// index.addField("author", "Tales of James", analyzer);
    /// QueryParser parser = new QueryParser("content", analyzer);
    /// float score = index.search(parser.parse("+author:james +salmon~ +fish/// manual~"));
    /// if (score &gt; 0.0f) {
    ///     System.out.println("it's a match");
    /// } else {
    ///     System.out.println("no match found");
    /// }
    /// System.out.println("indexData=" + index.toString());
    /// </pre>
    /// 
    /// 
    /// <h4>Example XQuery Usage</h4> 
    /// 
    /// <pre>
    /// (: An XQuery that finds all books authored by James that have something to do with "salmon fishing manuals", sorted by relevance :)
    /// declare namespace lucene = "java:nux.xom.pool.FullTextUtil";
    /// declare variable $query := "+salmon~ +fish/// manual~"; (: any arbitrary Lucene query can go here :)
    /// 
    /// for $book in /books/book[author="James" and lucene:match(abstract, $query) > 0.0]
    /// let $score := lucene:match($book/abstract, $query)
    /// order by $score descending
    /// return $book
    /// </pre>
    /// 
    /// 
    /// <h4>No thread safety guarantees</h4>
    /// 
    /// An instance can be queried multiple times with the same or different queries,
    /// but an instance is not thread-safe. If desired use idioms such as:
    /// <pre>
    /// MemoryIndex index = ...
    /// synchronized (index) {
    ///    // read and/or write index (i.e. add fields and/or query)
    /// } 
    /// </pre>
    /// 
    /// 
    /// <h4>Performance Notes</h4>
    /// 
    /// Internally there's a new data structure geared towards efficient indexing 
    /// and searching, plus the necessary support code to seamlessly plug into the Lucene 
    /// framework.
    /// <p/>
    /// This class performs very well for very small texts (e.g. 10 chars) 
    /// as well as for large texts (e.g. 10 MB) and everything in between. 
    /// Typically, it is about 10-100 times faster than <c>RAMDirectory</c>.
    /// Note that <c>RAMDirectory</c> has particularly 
    /// large efficiency overheads for small to medium sized texts, both in time and space.
    /// Indexing a field with N tokens takes O(N) in the best case, and O(N logN) in the worst 
    /// case. Memory consumption is probably larger than for <c>RAMDirectory</c>.
    /// <p/>
    /// Example throughput of many simple term queries over a single MemoryIndex: 
    /// ~500000 queries/sec on a MacBook Pro, jdk 1.5.0_06, server VM. 
    /// As always, your mileage may vary.
    /// <p/>
    /// If you're curious about
    /// the whereabouts of bottlenecks, run java 1.5 with the non-perturbing '-server
    /// -agentlib:hprof=cpu=samples,depth=10' flags, then study the trace log and
    /// correlate its hotspot trailer with its call stack headers (see <a
    /// target="_blank" href="http://java.sun.com/developer/technicalArticles/Programming/HPROF.html">
    /// hprof tracing </a>).
    ///
    ///</summary>
    [Serializable]
    public partial class MemoryIndex
    {
        /* info for each field: Map<String fieldName, Info field> */
        private readonly HashMap<String, Info> fields = new HashMap<String, Info>();

        /* fields sorted ascending by fieldName; lazily computed on demand */
        [NonSerialized]
        private KeyValuePair<String, Info>[] sortedFields;

        private readonly bool storeOffsets;

        private const bool DEBUG = false;

        private readonly ByteBlockPool byteBlockPool;
        private readonly IntBlockPool intBlockPool;
        //  private final IntBlockPool.SliceReader postingsReader;
        private readonly IntBlockPool.SliceWriter postingsWriter;

        private HashMap<String, FieldInfo> fieldInfos = new HashMap<String, FieldInfo>();

        private Counter bytesUsed;

        // .NET: we're using the stuff in TermComparer.cs instead
        //private static final Comparator<Object> termComparator ...

        /*
         * Constructs an empty instance.
         */
        public MemoryIndex()
            : this(false)
        {
        }

        /*
         * Constructs an empty instance that can optionally store the start and end
         * character offset of each token term in the text. This can be useful for
         * highlighting of hit locations with the Lucene highlighter package.
         * Private until the highlighter package matures, so that this can actually
         * be meaningfully integrated.
         * 
         * @param storeOffsets
         *            whether or not to store the start and end character offset of
         *            each token term in the text
         */

        public MemoryIndex(bool storeOffsets)
            : this(storeOffsets, 0)
        {
        }

        internal MemoryIndex(bool storeOffsets, long maxReusedBytes)
        {
            this.storeOffsets = storeOffsets;
            this.bytesUsed = Counter.NewCounter();
            int maxBufferedByteBlocks = (int)((maxReusedBytes / 2) / ByteBlockPool.BYTE_BLOCK_SIZE);
            int maxBufferedIntBlocks = (int)((maxReusedBytes - (maxBufferedByteBlocks * ByteBlockPool.BYTE_BLOCK_SIZE)) / (IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT));
            //assert (maxBufferedByteBlocks * ByteBlockPool.BYTE_BLOCK_SIZE) + (maxBufferedIntBlocks * IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT) <= maxReusedBytes;
            byteBlockPool = new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, maxBufferedByteBlocks, bytesUsed));
            intBlockPool = new IntBlockPool(new RecyclingIntBlockAllocator(IntBlockPool.INT_BLOCK_SIZE, maxBufferedIntBlocks, bytesUsed));
            postingsWriter = new IntBlockPool.SliceWriter(intBlockPool);
        }

        /*
         * Convenience method; Tokenizes the given field text and adds the resulting
         * terms to the index; Equivalent to adding an indexed non-keyword Lucene
         * {@link org.apache.lucene.document.Field} that is
         * {@link org.apache.lucene.document.Field.Index#ANALYZED tokenized},
         * {@link org.apache.lucene.document.Field.Store#NO not stored},
         * {@link org.apache.lucene.document.Field.TermVector#WITH_POSITIONS termVectorStored with positions} (or
         * {@link org.apache.lucene.document.Field.TermVector#WITH_POSITIONS termVectorStored with positions and offsets}),
         * 
         * @param fieldName
         *            a name to be associated with the text
         * @param text
         *            the text to tokenize and index.
         * @param analyzer
         *            the analyzer to use for tokenization
         */

        public void AddField(String fieldName, String text, Analyzer analyzer)
        {
            if (fieldName == null)
                throw new ArgumentException("fieldName must not be null");
            if (text == null)
                throw new ArgumentException("text must not be null");
            if (analyzer == null)
                throw new ArgumentException("analyzer must not be null");

            TokenStream stream = analyzer.TokenStream(fieldName, new StringReader(text));

            AddField(fieldName, stream, 1.0f, analyzer.GetPositionIncrementGap(fieldName));
        }

        /*
         * Convenience method; Creates and returns a token stream that generates a
         * token for each keyword in the given collection, "as is", without any
         * transforming text analysis. The resulting token stream can be fed into
         * {@link #addField(String, TokenStream)}, perhaps wrapped into another
         * {@link org.apache.lucene.analysis.TokenFilter}, as desired.
         * 
         * @param keywords
         *            the keywords to generate tokens for
         * @return the corresponding token stream
         */

        public TokenStream CreateKeywordTokenStream<T>(ICollection<T> keywords)
        {
            // TODO: deprecate & move this method into AnalyzerUtil?
            if (keywords == null)
                throw new ArgumentException("keywords must not be null");

            return new KeywordTokenStream<T>(keywords);
        }

        /*
         * Equivalent to <c>addField(fieldName, stream, 1.0f)</c>.
         * 
         * @param fieldName
         *            a name to be associated with the text
         * @param stream
         *            the token stream to retrieve tokens from
         */
        public void AddField(String fieldName, TokenStream stream)
        {
            AddField(fieldName, stream, 1.0f);
        }

        /**
        * Iterates over the given token stream and adds the resulting terms to the index;
        * Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
        * Lucene {@link org.apache.lucene.document.Field}.
        * Finally closes the token stream. Note that untokenized keywords can be added with this method via 
        * {@link #keywordTokenStream(Collection)}, the Lucene <code>KeywordTokenizer</code> or similar utilities.
        * 
        * @param fieldName
        *            a name to be associated with the text
        * @param stream
        *            the token stream to retrieve tokens from.
        * @param boost
        *            the boost factor for hits for this field
        *  
        * @see org.apache.lucene.document.Field#setBoost(float)
        */
        public void AddField(String fieldName, TokenStream stream, float boost)
        {
            AddField(fieldName, stream, boost, 0);
        }

        /*
         * Iterates over the given token stream and adds the resulting terms to the index;
         * Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
         * Lucene {@link org.apache.lucene.document.Field}.
         * Finally closes the token stream. Note that untokenized keywords can be added with this method via 
         * {@link #CreateKeywordTokenStream(Collection)}, the Lucene contrib <c>KeywordTokenizer</c> or similar utilities.
         * 
         * @param fieldName
         *            a name to be associated with the text
         * @param stream
         *            the token stream to retrieve tokens from.
         * @param boost
         *            the boost factor for hits for this field
         * @see org.apache.lucene.document.Field#setBoost(float)
         */
        public void AddField(String fieldName, TokenStream stream, float boost, int positionIncrementGap)
        {
            try
            {
                if (fieldName == null)
                    throw new ArgumentException("fieldName must not be null");
                if (stream == null)
                    throw new ArgumentException("token stream must not be null");
                if (boost <= 0.0f)
                    throw new ArgumentException("boost factor must be greater than 0.0");
                int numTokens = 0;
                int numOverlapTokens = 0;
                int pos = -1;
                BytesRefHash terms;
                SliceByteStartArray sliceArray;
                Info info = null;
                long sumTotalTermFreq = 0;
                if ((info = fields[fieldName]) != null)
                {
                    numTokens = info.numTokens;
                    numOverlapTokens = info.numOverlapTokens;
                    pos = info.lastPosition + positionIncrementGap;
                    terms = info.terms;
                    boost *= info.boost;
                    sliceArray = info.sliceArray;
                    sumTotalTermFreq = info.sumTotalTermFreq;
                }
                else
                {
                    sliceArray = new SliceByteStartArray(BytesRefHash.DEFAULT_CAPACITY);
                    terms = new BytesRefHash(byteBlockPool, BytesRefHash.DEFAULT_CAPACITY, sliceArray);
                }

                if (!fieldInfos.ContainsKey(fieldName))
                {
                    fieldInfos[fieldName] =
                        new FieldInfo(fieldName, true, fieldInfos.Count, false, false, false, this.storeOffsets ? FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS : FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, null, null, null);
                }
                ITermToBytesRefAttribute termAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
                IPositionIncrementAttribute posIncrAttribute = stream.AddAttribute<IPositionIncrementAttribute>();
                IOffsetAttribute offsetAtt = stream.AddAttribute<IOffsetAttribute>();
                BytesRef ref_renamed = termAtt.BytesRef;
                stream.Reset();

                while (stream.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    //if (DEBUG) System.err.println("token='" + term + "'");
                    numTokens++;
                    int posIncr = posIncrAttribute.PositionIncrement;
                    if (posIncr == 0)
                        numOverlapTokens++;
                    pos += posIncr;
                    int ord = terms.Add(ref_renamed);
                    if (ord < 0)
                    {
                        ord = (-ord) - 1;
                        postingsWriter.Reset(sliceArray.end[ord]);
                    }
                    else
                    {
                        sliceArray.start[ord] = postingsWriter.StartNewSlice();
                    }
                    sliceArray.freq[ord]++;
                    sumTotalTermFreq++;
                    if (!storeOffsets)
                    {
                        postingsWriter.WriteInt(pos);
                    }
                    else
                    {
                        postingsWriter.WriteInt(pos);
                        postingsWriter.WriteInt(offsetAtt.StartOffset);
                        postingsWriter.WriteInt(offsetAtt.EndOffset);
                    }
                    sliceArray.end[ord] = postingsWriter.CurrentOffset;
                }
                stream.End();

                // ensure infos.numTokens > 0 invariant; needed for correct operation of terms()
                if (numTokens > 0)
                {
                    fields[fieldName] = new Info(terms, sliceArray, numTokens, numOverlapTokens, boost, pos, sumTotalTermFreq);
                    sortedFields = null;    // invalidate sorted view, if any
                }
            }
            catch (IOException e)
            {
                // can never happen
                throw new SystemException(string.Empty, e);
            }
            finally
            {
                try
                {
                    if (stream != null) stream.Dispose();
                }
                catch (IOException e2)
                {
                    throw new SystemException(string.Empty, e2);
                }
            }
        }

        /*
         * Creates and returns a searcher that can be used to execute arbitrary
         * Lucene queries and to collect the resulting query results as hits.
         * 
         * @return a searcher
         */

        public IndexSearcher CreateSearcher()
        {
            MemoryIndexReader reader = new MemoryIndexReader(this);
            IndexSearcher searcher = new IndexSearcher(reader); // ensures no auto-close !!
            reader.SetSearcher(searcher); // to later get hold of searcher.getSimilarity()
            return searcher;
        }

        /*
         * Convenience method that efficiently returns the relevance score by
         * matching this index against the given Lucene query expression.
         * 
         * @param query
         *            an arbitrary Lucene query to run against this index
         * @return the relevance score of the matchmaking; A number in the range
         *         [0.0 .. 1.0], with 0.0 indicating no match. The higher the number
         *         the better the match.
         *
         */

        public float Search(Query query)
        {
            if (query == null)
                throw new ArgumentException("query must not be null");

            IndexSearcher searcher = CreateSearcher();
            try
            {
                float[] scores = new float[1]; // inits to 0.0f (no match)
                searcher.Search(query, new FillingCollector(scores));
                float score = scores[0];
                return score;
            }
            catch (IOException e)
            {
                // can never happen (RAMDirectory)
                throw new SystemException(string.Empty, e);
            }
            finally
            {
                // searcher.close();
                /*
                 * Note that it is harmless and important for good performance to
                 * NOT close the index reader!!! This avoids all sorts of
                 * unnecessary baggage and locking in the Lucene IndexReader
                 * superclass, all of which is completely unnecessary for this main
                 * memory index data structure without thread-safety claims.
                 * 
                 * Wishing IndexReader would be an interface...
                 * 
                 * Actually with the new tight createSearcher() API auto-closing is now
                 * made impossible, hence searcher.close() would be harmless and also 
                 * would not degrade performance...
                 */
            }
        }

        /*
         * Returns a reasonable approximation of the main memory [bytes] consumed by
         * this instance. Useful for smart memory sensititive caches/pools. Assumes
         * fieldNames are interned, whereas tokenized terms are memory-overlaid.
         * 
         * @return the main memory consumption
         */
        public long GetMemorySize()
        {
            return RamUsageEstimator.SizeOf(this);
        }

        /* sorts into ascending order (on demand), reusing memory along the way */

        private void SortFields()
        {
            if (sortedFields == null) sortedFields = Sort(fields);
        }

        /* returns a view of the given map's entries, sorted ascending by key */

        private static KeyValuePair<TKey, TValue>[] Sort<TKey, TValue>(HashMap<TKey, TValue> map)
            where TKey : class, IComparable<TKey>
        {
            int size = map.Count;

            var entries = map.ToArray();

            if (size > 1) Array.Sort(entries, TermComparer.KeyComparer);
            return entries;
        }

        /*
         * Returns a String representation of the index data for debugging purposes.
         * 
         * @return the string representation
         */

        public override String ToString()
        {
            StringBuilder result = new StringBuilder(256);
            SortFields();
            int sumPositions = 0;
            int sumTerms = 0;
            BytesRef spare = new BytesRef();

            for (int i = 0; i < sortedFields.Length; i++)
            {
                KeyValuePair<String, Info> entry = sortedFields[i];
                String fieldName = entry.Key;
                Info info = entry.Value;
                info.SortTerms();
                result.Append(fieldName + ":\n");

                SliceByteStartArray sliceArray = info.sliceArray;
                int numPositions = 0;
                IntBlockPool.SliceReader postingsReader = new IntBlockPool.SliceReader(intBlockPool);
                for (int j = 0; j < info.terms.Size; j++)
                {
                    int ord = info.sortedTerms[j];
                    info.terms.Get(ord, spare);
                    int freq = sliceArray.freq[ord];
                    result.Append("\t'" + spare + "':" + freq + ":");
                    postingsReader.Reset(sliceArray.start[ord], sliceArray.end[ord]);
                    result.Append(" [");
                    int iters = storeOffsets ? 3 : 1;
                    while (!postingsReader.EndOfSlice())
                    {
                        result.Append("(");

                        for (int k = 0; k < iters; k++)
                        {
                            result.Append(postingsReader.ReadInt());
                            if (k < iters - 1)
                            {
                                result.Append(", ");
                            }
                        }
                        result.Append(")");
                        if (!postingsReader.EndOfSlice())
                        {
                            result.Append(",");
                        }

                    }
                    result.Append("]");
                    result.Append("\n");
                    numPositions += freq;
                }

                result.Append("\tterms=" + info.sortedTerms.Length);
                result.Append(", positions=" + numPositions);
                result.Append(", memory=" + RamUsageEstimator.HumanReadableUnits(RamUsageEstimator.SizeOf(info)));
                result.Append("\n");
                sumPositions += numPositions;
                sumTerms += info.terms.Size;
            }

            result.Append("\nfields=" + sortedFields.Length);
            result.Append(", terms=" + sumTerms);
            result.Append(", positions=" + sumPositions);
            result.Append(", memory=" + RamUsageEstimator.HumanReadableUnits(GetMemorySize()));
            return result.ToString();
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Nested classes:
        ///////////////////////////////////////////////////////////////////////////////
        /*
         * Index data structure for a field; Contains the tokenized term texts and
         * their positions.
         */

        [Serializable]
        private sealed class Info
        {
            /**
            * Term strings and their positions for this field: Map String
            * termText, ArrayIntList positions
            */
            internal readonly BytesRefHash terms;

            internal readonly SliceByteStartArray sliceArray;

            /* Terms sorted ascending by term text; computed on demand */
            [NonSerialized]
            internal int[] sortedTerms;

            /* Number of added tokens for this field */
            internal readonly int numTokens;

            /* Number of overlapping tokens for this field */
            internal readonly int numOverlapTokens;

            /* Boost factor for hits for this field */
            internal readonly float boost;

            internal readonly long sumTotalTermFreq;

            /** the last position encountered in this field for multi field support*/
            internal int lastPosition;

            public Info(BytesRefHash terms, SliceByteStartArray sliceArray, int numTokens, int numOverlapTokens, float boost, int lastPosition, long sumTotalTermFreq)
            {
                this.terms = terms;
                this.sliceArray = sliceArray;
                this.numTokens = numTokens;
                this.numOverlapTokens = numOverlapTokens;
                this.boost = boost;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.lastPosition = lastPosition;
            }

            public long SumTotalTermFreq
            {
                get { return sumTotalTermFreq; }
            }

            /*
            * Sorts hashed terms into ascending order, reusing memory along the
            * way. Note that sorting is lazily delayed until required (often it's
            * not required at all). If a sorted view is required then hashing +
            * sort + binary search is still faster and smaller than TreeMap usage
            * (which would be an alternative and somewhat more elegant approach,
            * apart from more sophisticated Tries / prefix trees).
            */

            public void SortTerms()
            {
                if (sortedTerms == null)
                    sortedTerms = terms.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            }

            public float Boost
            {
                get { return boost; }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Nested classes:
        ///////////////////////////////////////////////////////////////////////////////

        /*
         * Search support for Lucene framework integration; implements all methods
         * required by the Lucene IndexReader contracts.
         */

        private sealed partial class MemoryIndexReader : AtomicReader
        {
            private readonly MemoryIndex _index;

            private IndexSearcher searcher; // needed to find searcher.getSimilarity() 

            internal MemoryIndexReader(MemoryIndex index)
            {
                _index = index;
            }

            private Info GetInfo(String fieldName)
            {
                return _index.fields[fieldName];
            }

            private Info GetInfo(int pos)
            {
                return _index.sortedFields[pos].Value;
            }

            public override IBits LiveDocs
            {
                get { return null; }
            }

            public override FieldInfos FieldInfos
            {
                get { return new FieldInfos(_index.fieldInfos.Values.ToArray()); }
            }

            public override NumericDocValues GetNumericDocValues(string field)
            {
                return null;
            }

            public override BinaryDocValues GetBinaryDocValues(string field)
            {
                return null;
            }

            public override SortedDocValues GetSortedDocValues(string field)
            {
                return null;
            }

            public override SortedSetDocValues GetSortedSetDocValues(string field)
            {
                return null;
            }

            private sealed class MemoryFields : Fields
            {
                private readonly MemoryIndexReader parent;

                public MemoryFields(MemoryIndexReader parent)
                {
                    this.parent = parent;
                }

                public override IEnumerator<string> GetEnumerator()
                {
                    return parent._index.sortedFields.Select(i => i.Key).GetEnumerator();
                }

                public override Terms Terms(string field)
                {
                    int i = Array.BinarySearch(parent._index.sortedFields, field, new TermComparer<Info>());
                    if (i < 0)
                    {
                        return null;
                    }
                    else
                    {
                        Info info = parent.GetInfo(i);
                        info.SortTerms();

                        return new AnonymousTerms(this, info);
                    }
                }

                private sealed class AnonymousTerms : Terms
                {
                    private readonly MemoryFields parent;
                    private readonly Info info;

                    public AnonymousTerms(MemoryFields parent, Info info)
                    {
                        this.parent = parent;
                        this.info = info;
                    }

                    public override TermsEnum Iterator(TermsEnum reuse)
                    {
                        return new MemoryTermsEnum(parent.parent._index, info);
                    }

                    public override IComparer<BytesRef> Comparator
                    {
                        get { return BytesRef.UTF8SortedAsUnicodeComparer; }
                    }

                    public override long Size
                    {
                        get { return info.terms.Size; }
                    }

                    public override long SumTotalTermFreq
                    {
                        get { return info.SumTotalTermFreq; }
                    }

                    public override long SumDocFreq
                    {
                        get { return info.terms.Size; }
                    }

                    public override int DocCount
                    {
                        get { return info.terms.Size > 0 ? 1 : 0; }
                    }

                    public override bool HasOffsets
                    {
                        get { return parent.parent._index.storeOffsets; }
                    }

                    public override bool HasPositions
                    {
                        get { return true; }
                    }

                    public override bool HasPayloads
                    {
                        get { return false; }
                    }
                }

                public override int Size
                {
                    get { return parent._index.sortedFields.Length; }
                }
            }

            public override Fields Fields
            {
                get
                {
                    _index.SortFields();
                    return new MemoryFields(this);
                }
            }

            public override Fields GetTermVectors(int docID)
            {
                if (docID == 0)
                {
                    return Fields;
                }
                else
                {
                    return null;
                }
            }

            private Similarity GetSimilarity()
            {
                if (searcher != null) return searcher.Similarity;
                return IndexSearcher.DefaultSimilarity;
            }

            internal void SetSearcher(IndexSearcher searcher)
            {
                this.searcher = searcher;
            }

            public override int NumDocs
            {
                get
                {
                    if (DEBUG) System.Diagnostics.Debug.WriteLine("MemoryIndexReader.numDocs");

                    return 1;
                }
            }

            public override int MaxDoc
            {
                get
                {
                    if (DEBUG) System.Diagnostics.Debug.WriteLine("MemoryIndexReader.maxDoc");
                    return 1;
                }
            }

            public override void Document(int docID, StoredFieldVisitor visitor)
            {
                if (DEBUG) System.Diagnostics.Debug.WriteLine("MemoryIndexReader.document");
                // no-op: there are no stored fields
            }
            
            protected override void DoClose()
            {
                if (DEBUG) System.Diagnostics.Debug.WriteLine("MemoryIndexReader.doClose");
            }

            /* performance hack: cache norms to avoid repeated expensive calculations */
            private NumericDocValues cachedNormValues;
            private String cachedFieldName;
            private Similarity cachedSimilarity;

            public override NumericDocValues GetNormValues(string field)
            {
                FieldInfo fieldInfo = _index.fieldInfos[field];
                if (fieldInfo == null || fieldInfo.OmitsNorms)
                    return null;
                NumericDocValues norms = cachedNormValues;
                Similarity sim = GetSimilarity();
                if (!field.Equals(cachedFieldName) || sim != cachedSimilarity)
                { // not cached?
                    Info info = GetInfo(field);
                    int numTokens = info != null ? info.numTokens : 0;
                    int numOverlapTokens = info != null ? info.numOverlapTokens : 0;
                    float boost = info != null ? info.Boost : 1.0f;
                    FieldInvertState invertState = new FieldInvertState(field, 0, numTokens, numOverlapTokens, 0, boost);
                    long value = sim.ComputeNorm(invertState);
                    norms = new MemoryIndexNormDocValues(value);
                    // cache it for future reuse
                    cachedNormValues = norms;
                    cachedFieldName = field;
                    cachedSimilarity = sim;
                    if (DEBUG) System.Diagnostics.Debug.WriteLine("MemoryIndexReader.norms: " + field + ":" + value + ":" + numTokens);
                }
                return norms;
            }
        }
        
        /**
        * Resets the {@link MemoryIndex} to its initial state and recycles all internal buffers.
        */
        public void Reset()
        {
            this.fieldInfos.Clear();
            this.fields.Clear();
            this.sortedFields = null;
            byteBlockPool.Reset(false, false); // no need to 0-fill the buffers
            intBlockPool.Reset(true, false); // here must must 0-fill since we use slices
        }
    }
}
