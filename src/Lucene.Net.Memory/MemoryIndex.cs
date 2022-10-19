using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lucene.Net.Index.Memory
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
    /// High-performance single-document main memory Apache Lucene fulltext search index. 
    /// 
    /// <h4>Overview</h4>
    /// 
    /// This class is a replacement/substitute for a large subset of
    /// <see cref="Store.RAMDirectory"/> functionality. It is designed to
    /// enable maximum efficiency for on-the-fly matchmaking combining structured and 
    /// fuzzy fulltext search in realtime streaming applications such as Nux XQuery based XML 
    /// message queues, publish-subscribe systems for Blogs/newsfeeds, text chat, data acquisition and 
    /// distribution systems, application level routers, firewalls, classifiers, etc. 
    /// Rather than targeting fulltext search of infrequent queries over huge persistent 
    /// data archives (historic search), this class targets fulltext search of huge 
    /// numbers of queries over comparatively small transient realtime data (prospective 
    /// search). 
    /// For example as in 
    /// <code>
    /// float score = Search(string text, Query query)
    /// </code>
    /// <para>
    /// Each instance can hold at most one Lucene "document", with a document containing
    /// zero or more "fields", each field having a name and a fulltext value. The
    /// fulltext value is tokenized (split and transformed) into zero or more index terms 
    /// (aka words) on <code>AddField()</code>, according to the policy implemented by an
    /// Analyzer. For example, Lucene analyzers can split on whitespace, normalize to lower case
    /// for case insensitivity, ignore common terms with little discriminatory value such as "he", "in", "and" (stop
    /// words), reduce the terms to their natural linguistic root form such as "fishing"
    /// being reduced to "fish" (stemming), resolve synonyms/inflexions/thesauri 
    /// (upon indexing and/or querying), etc. For details, see
    /// <a target="_blank" href="http://today.java.net/pub/a/today/2003/07/30/LuceneIntro.html">Lucene Analyzer Intro</a>.
    /// </para>
    /// <para>
    /// Arbitrary Lucene queries can be run against this class - see <a target="_blank" 
    /// href="{@docRoot}/../queryparser/org/apache/lucene/queryparser/classic/package-summary.html#package_description">
    /// Lucene Query Syntax</a>
    /// as well as <a target="_blank" 
    /// href="http://today.java.net/pub/a/today/2003/11/07/QueryParserRules.html">Query Parser Rules</a>.
    /// Note that a Lucene query selects on the field names and associated (indexed) 
    /// tokenized terms, not on the original fulltext(s) - the latter are not stored 
    /// but rather thrown away immediately after tokenization.
    /// </para>
    /// <para>
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
    /// <code>
    /// Analyzer analyzer = new SimpleAnalyzer(version);
    /// MemoryIndex index = new MemoryIndex();
    /// index.AddField("content", "Readings about Salmons and other select Alaska fishing Manuals", analyzer);
    /// index.AddField("author", "Tales of James", analyzer);
    /// QueryParser parser = new QueryParser(version, "content", analyzer);
    /// float score = index.Search(parser.Parse("+author:james +salmon~ +fish* manual~"));
    /// if (score &gt; 0.0f) {
    ///     Console.WriteLine("it's a match");
    /// } else {
    ///     Console.WriteLine("no match found");
    /// }
    /// Console.WriteLine("indexData=" + index.toString());
    /// </code>
    /// 
    /// 
    /// <h4>Example XQuery Usage</h4> 
    /// 
    /// <code>
    /// (: An XQuery that finds all books authored by James that have something to do with "salmon fishing manuals", sorted by relevance :)
    /// declare namespace lucene = "java:nux.xom.pool.FullTextUtil";
    /// declare variable $query := "+salmon~ +fish* manual~"; (: any arbitrary Lucene query can go here :)
    /// 
    /// for $book in /books/book[author="James" and lucene:match(abstract, $query) > 0.0]
    /// let $score := lucene:match($book/abstract, $query)
    /// order by $score descending
    /// return $book
    /// </code>
    /// 
    /// 
    /// <h4>No thread safety guarantees</h4>
    /// 
    /// An instance can be queried multiple times with the same or different queries,
    /// but an instance is not thread-safe. If desired use idioms such as:
    /// <code>
    /// MemoryIndex index = ...
    /// lock (index) {
    ///    // read and/or write index (i.e. add fields and/or query)
    /// } 
    /// </code>
    /// 
    /// 
    /// <h4>Performance Notes</h4>
    /// 
    /// Internally there's a new data structure geared towards efficient indexing 
    /// and searching, plus the necessary support code to seamlessly plug into the Lucene 
    /// framework.
    /// </para>
    /// <para>
    /// This class performs very well for very small texts (e.g. 10 chars) 
    /// as well as for large texts (e.g. 10 MB) and everything in between. 
    /// Typically, it is about 10-100 times faster than <see cref="Store.RAMDirectory"/>.
    /// Note that <see cref="Store.RAMDirectory"/> has particularly 
    /// large efficiency overheads for small to medium sized texts, both in time and space.
    /// Indexing a field with N tokens takes O(N) in the best case, and O(N logN) in the worst 
    /// case. Memory consumption is probably larger than for <see cref="Store.RAMDirectory"/>.
    /// </para>
    /// <para>
    /// Example throughput of many simple term queries over a single MemoryIndex: 
    /// ~500000 queries/sec on a MacBook Pro, jdk 1.5.0_06, server VM. 
    /// As always, your mileage may vary.
    /// </para>
    /// <para>
    /// If you're curious about
    /// the whereabouts of bottlenecks, run java 1.5 with the non-perturbing '-server
    /// -agentlib:hprof=cpu=samples,depth=10' flags, then study the trace log and
    /// correlate its hotspot trailer with its call stack headers (see <a
    /// target="_blank"
    /// href="http://java.sun.com/developer/technicalArticles/Programming/HPROF.html">
    /// hprof tracing </a>).
    /// 
    /// </para>
    /// </summary>
    public partial class MemoryIndex
    {
        /// <summary>
        /// info for each field: <see cref="IDictionary{String, Info}"/>
        /// </summary>
        private readonly IDictionary<string, Info> fields = new Dictionary<string, Info>();

        /// <summary>
        /// fields sorted ascending by fieldName; lazily computed on demand </summary>
        private KeyValuePair<string, Info>[] sortedFields;

        private readonly bool storeOffsets;

        private readonly ByteBlockPool byteBlockPool;
        private readonly Int32BlockPool intBlockPool;
        //  private final IntBlockPool.SliceReader postingsReader;
        private readonly Int32BlockPool.SliceWriter postingsWriter;

        private readonly Dictionary<string, FieldInfo> fieldInfos = new Dictionary<string, FieldInfo>(); // LUCENENET: marked readonly

        private readonly Counter bytesUsed; // LUCENENET: marked readonly

        /// <summary>
        /// Constructs an empty instance.
        /// </summary>
        public MemoryIndex()
            : this(false)
        {
        }

        /// <summary>
        /// Constructs an empty instance that can optionally store the start and end
        /// character offset of each token term in the text. This can be useful for
        /// highlighting of hit locations with the Lucene highlighter package.
        /// Protected until the highlighter package matures, so that this can actually
        /// be meaningfully integrated.
        /// </summary>
        /// <param name="storeOffsets">
        ///            whether or not to store the start and end character offset of
        ///            each token term in the text </param>
        public MemoryIndex(bool storeOffsets)
              : this(storeOffsets, 0)
        {
        }

        /// <summary>
        /// Expert: This constructor accepts an upper limit for the number of bytes that should be reused if this instance is <see cref="Reset()"/>.
        /// </summary>
        /// <param name="storeOffsets"> <c>true</c> if offsets should be stored </param>
        /// <param name="maxReusedBytes"> the number of bytes that should remain in the internal memory pools after <see cref="Reset()"/> is called </param>
        internal MemoryIndex(bool storeOffsets, long maxReusedBytes)
        {
            this.storeOffsets = storeOffsets;
            this.bytesUsed = Counter.NewCounter();
            int maxBufferedByteBlocks = (int)((maxReusedBytes / 2) / ByteBlockPool.BYTE_BLOCK_SIZE);
            int maxBufferedIntBlocks = (int)((maxReusedBytes - (maxBufferedByteBlocks * ByteBlockPool.BYTE_BLOCK_SIZE)) / (Int32BlockPool.INT32_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT32));
            if (Debugging.AssertsEnabled) Debugging.Assert((maxBufferedByteBlocks * ByteBlockPool.BYTE_BLOCK_SIZE) + (maxBufferedIntBlocks * Int32BlockPool.INT32_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT32) <= maxReusedBytes);
            byteBlockPool = new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, maxBufferedByteBlocks, bytesUsed));
            intBlockPool = new Int32BlockPool(new RecyclingInt32BlockAllocator(Int32BlockPool.INT32_BLOCK_SIZE, maxBufferedIntBlocks, bytesUsed));
            postingsWriter = new Int32BlockPool.SliceWriter(intBlockPool);
        }

        /// <summary>
        /// Convenience method; Tokenizes the given field text and adds the resulting
        /// terms to the index; Equivalent to adding an indexed non-keyword Lucene
        /// <see cref="Documents.Field"/> that is tokenized, not stored,
        /// termVectorStored with positions (or termVectorStored with positions and offsets),
        /// </summary>
        /// <param name="fieldName"> a name to be associated with the text </param>
        /// <param name="text"> the text to tokenize and index. </param>
        /// <param name="analyzer"> the analyzer to use for tokenization </param>
        public virtual void AddField(string fieldName, string text, Analyzer analyzer)
        {
            if (fieldName is null)
            {
                throw new ArgumentNullException(nameof(fieldName), "fieldName must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text), "text must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (analyzer is null)
            {
                throw new ArgumentNullException(nameof(analyzer), "analyzer must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            TokenStream stream;
            try
            {
                stream = analyzer.GetTokenStream(fieldName, text);
            }
            catch (Exception ex) when (ex.IsIOException())
            {
                throw RuntimeException.Create(ex);
            }

            AddField(fieldName, stream, 1.0f, analyzer.GetPositionIncrementGap(fieldName), analyzer.GetOffsetGap(fieldName));
        }

        /// <summary>
        /// Convenience method; Creates and returns a token stream that generates a
        /// token for each keyword in the given collection, "as is", without any
        /// transforming text analysis. The resulting token stream can be fed into
        /// <see cref="AddField(string, TokenStream)"/>, perhaps wrapped into another
        /// <see cref="TokenFilter"/>, as desired.
        /// </summary>
        /// <param name="keywords"> the keywords to generate tokens for </param>
        /// <returns> the corresponding token stream </returns>
        public virtual TokenStream KeywordTokenStream<T>(ICollection<T> keywords)
        {
            // TODO: deprecate & move this method into AnalyzerUtil?
            if (keywords is null)
            {
                throw new ArgumentNullException(nameof(keywords), "keywords must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            return new TokenStreamAnonymousClass<T>(keywords);
        }

        private sealed class TokenStreamAnonymousClass<T> : TokenStream
        {
            public TokenStreamAnonymousClass(ICollection<T> keywords)
            {
                iter = keywords.GetEnumerator();
                start = 0;
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            private IEnumerator<T> iter;
            private int start;
            private readonly ICharTermAttribute termAtt;
            private readonly IOffsetAttribute offsetAtt;

            public override bool IncrementToken()
            {
                if (!iter.MoveNext())
                {
                    return false;
                }

                T obj = iter.Current;
                if (obj is null)
                {
                    throw new ArgumentException("keyword must not be null");
                }

                string term = obj.ToString();
                ClearAttributes();
                termAtt.SetEmpty().Append(term);
                offsetAtt.SetOffset(start, start + termAtt.Length);
                start += term.Length + 1; // separate words by 1 (blank) character
                return true;
            }

            /// <summary>
            /// Releases resources used by the <see cref="TokenStreamAnonymousClass{T}"/> and
            /// if overridden in a derived class, optionally releases unmanaged resources.
            /// </summary>
            /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
            /// <c>false</c> to release only unmanaged resources.</param>

            // LUCENENET specific
            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        iter?.Dispose(); // LUCENENET specific - dispose iter and set to null
                        iter = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        /// <summary>
        /// Equivalent to <c>AddField(fieldName, stream, 1.0f)</c>.
        /// </summary>
        /// <param name="fieldName"> a name to be associated with the text </param>
        /// <param name="stream"> the token stream to retrieve tokens from </param>
        public virtual void AddField(string fieldName, TokenStream stream)
        {
            AddField(fieldName, stream, 1.0f);
        }

        /// <summary>
        /// Iterates over the given token stream and adds the resulting terms to the index;
        /// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
        /// Lucene <see cref="Documents.Field"/>.
        /// Finally closes the token stream. Note that untokenized keywords can be added with this method via 
        /// <see cref="T:KeywordTokenStream{T}(ICollection{T}"/>)"/>, the Lucene <c>KeywordTokenizer</c> or similar utilities.
        /// </summary>
        /// <param name="fieldName"> a name to be associated with the text </param>
        /// <param name="stream"> the token stream to retrieve tokens from. </param>
        /// <param name="boost"> the boost factor for hits for this field </param>
        /// <seealso cref="Documents.Field.Boost"/>
        public virtual void AddField(string fieldName, TokenStream stream, float boost)
        {
            AddField(fieldName, stream, boost, 0);
        }


        /// <summary>
        /// Iterates over the given token stream and adds the resulting terms to the index;
        /// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
        /// Lucene <see cref="Documents.Field"/>.
        /// Finally closes the token stream. Note that untokenized keywords can be added with this method via
        /// <see cref="T:KeywordTokenStream{T}(ICollection{T}"/>)"/>, the Lucene <c>KeywordTokenizer</c> or similar utilities.
        /// </summary>
        /// <param name="fieldName"> a name to be associated with the text </param>
        /// <param name="stream"> the token stream to retrieve tokens from. </param>
        /// <param name="boost"> the boost factor for hits for this field </param>
        /// <param name="positionIncrementGap"> 
        /// the position increment gap if fields with the same name are added more than once
        /// </param>
        /// <seealso cref="Documents.Field.Boost"/>
        public virtual void AddField(string fieldName, TokenStream stream, float boost, int positionIncrementGap)
        {
            AddField(fieldName, stream, boost, positionIncrementGap, 1);
        }

        /// <summary>
        /// Iterates over the given token stream and adds the resulting terms to the index;
        /// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
        /// Lucene <see cref="Documents.Field"/>.
        /// Finally closes the token stream. Note that untokenized keywords can be added with this method via 
        /// <see cref="T:KeywordTokenStream{T}(ICollection{T}"/>)"/>, the Lucene <c>KeywordTokenizer</c> or similar utilities.
        /// 
        /// </summary>
        /// <param name="fieldName"> a name to be associated with the text </param>
        /// <param name="stream"> the token stream to retrieve tokens from. </param>
        /// <param name="boost"> the boost factor for hits for this field </param>
        /// <param name="positionIncrementGap"> the position increment gap if fields with the same name are added more than once </param>
        /// <param name="offsetGap"> the offset gap if fields with the same name are added more than once </param>
        /// <seealso cref="Documents.Field.Boost"/>
        public virtual void AddField(string fieldName, TokenStream stream, float boost, int positionIncrementGap, int offsetGap)
        {
            try
            {
                if (fieldName is null)
                {
                    throw new ArgumentNullException(nameof(fieldName), "fieldName must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
                }
                if (stream is null)
                {
                    throw new ArgumentNullException(nameof(stream), "token stream must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
                }
                if (boost <= 0.0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(boost), "boost factor must be greater than 0.0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                int numTokens = 0;
                int numOverlapTokens = 0;
                int pos = -1;
                BytesRefHash terms;
                SliceByteStartArray sliceArray;
                long sumTotalTermFreq = 0;
                int offset = 0;
                if (fields.TryGetValue(fieldName, out Info info))
                {
                    numTokens = info.numTokens;
                    numOverlapTokens = info.numOverlapTokens;
                    pos = info.lastPosition + positionIncrementGap;
                    offset = info.lastOffset + offsetGap;
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
                    fieldInfos[fieldName] = new FieldInfo(fieldName, 
                                                        true, 
                                                        fieldInfos.Count, 
                                                        false, 
                                                        false, 
                                                        false, 
                                                        this.storeOffsets ? IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS : IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, 
                                                        DocValuesType.NONE, 
                                                        DocValuesType.NONE, 
                                                        null);
                }
                ITermToBytesRefAttribute termAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
                IPositionIncrementAttribute posIncrAttribute = stream.AddAttribute<IPositionIncrementAttribute>();
                IOffsetAttribute offsetAtt = stream.AddAttribute<IOffsetAttribute>();
                BytesRef @ref = termAtt.BytesRef;
                stream.Reset();

                while (stream.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    //        if (DEBUG) System.err.println("token='" + term + "'");
                    numTokens++;
                    int posIncr = posIncrAttribute.PositionIncrement;
                    if (posIncr == 0)
                    {
                        numOverlapTokens++;
                    }
                    pos += posIncr;
                    int ord = terms.Add(@ref);
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
                        postingsWriter.WriteInt32(pos);
                    }
                    else
                    {
                        postingsWriter.WriteInt32(pos);
                        postingsWriter.WriteInt32(offsetAtt.StartOffset + offset);
                        postingsWriter.WriteInt32(offsetAtt.EndOffset + offset);
                    }
                    sliceArray.end[ord] = postingsWriter.CurrentOffset;
                }
                stream.End();

                // ensure infos.numTokens > 0 invariant; needed for correct operation of terms()
                if (numTokens > 0)
                {
                    fields[fieldName] = new Info(terms, sliceArray, numTokens, numOverlapTokens, boost, pos, offsetAtt.EndOffset + offset, sumTotalTermFreq);
                    sortedFields = null; // invalidate sorted view, if any
                }
            } // can never happen
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create(e);
            }
            finally
            {
                try
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
                catch (Exception e2) when (e2.IsIOException())
                {
                    throw RuntimeException.Create(e2);
                }
            }
        }

        /// <summary>
        /// Creates and returns a searcher that can be used to execute arbitrary
        /// Lucene queries and to collect the resulting query results as hits.
        /// </summary>
        /// <returns> a searcher </returns>
        public virtual IndexSearcher CreateSearcher()
        {
            MemoryIndexReader reader = new MemoryIndexReader(this);
            IndexSearcher searcher = new IndexSearcher(reader); // ensures no auto-close !!
            reader.searcher = searcher; // to later get hold of searcher.getSimilarity()
            return searcher;
        }

        /// <summary>
        /// Convenience method that efficiently returns the relevance score by
        /// matching this index against the given Lucene query expression.
        /// </summary>
        /// <param name="query"> an arbitrary Lucene query to run against this index </param>
        /// <returns> the relevance score of the matchmaking; A number in the range
        ///         [0.0 .. 1.0], with 0.0 indicating no match. The higher the number
        ///         the better the match.
        ///  </returns>
        public virtual float Search(Query query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query), "query must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            IndexSearcher searcher = CreateSearcher();
            try
            {
                float[] scores = new float[1]; // inits to 0.0f (no match)
                searcher.Search(query, new CollectorAnonymousClass(scores));
                float score = scores[0];
                return score;
            } // can never happen (RAMDirectory)
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
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

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly float[] scores;

            public CollectorAnonymousClass(float[] scores)
            {
                this.scores = scores;
            }

            private Scorer scorer;

            public void Collect(int doc)
            {
                scores[0] = scorer.GetScore();
            }

            public void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public bool AcceptsDocsOutOfOrder => true;

            public void SetNextReader(AtomicReaderContext context)
            {
            }
        }

        /// <summary>
        /// Returns a reasonable approximation of the main memory [bytes] consumed by
        /// this instance. Useful for smart memory sensititive caches/pools. </summary>
        /// <returns> the main memory consumption </returns>
        public virtual long GetMemorySize()
        {
            return RamUsageEstimator.SizeOf(this);
        }

        /// <summary>
        /// sorts into ascending order (on demand), reusing memory along the way
        /// </summary>
        private void SortFields()
        {
            if (sortedFields is null)
            {
                sortedFields = Sort(fields);
            }
        }

        /// <summary>
        /// returns a view of the given map's entries, sorted ascending by key
        /// </summary>
        private static KeyValuePair<K, V>[] Sort<K, V>(IDictionary<K, V> map)
              where K : class, IComparable<K>
        {
            int size = map.Count;
            KeyValuePair<K, V>[] entries = new KeyValuePair<K, V>[size];

            using (IEnumerator<KeyValuePair<K, V>> iter = map.GetEnumerator())
            {
                for (int i = 0; i < size && iter.MoveNext(); i++)
                {
                    entries[i] = iter.Current;
                }
            }

            if (size > 1)
            {
                ArrayUtil.IntroSort(entries, new TermComparer<K, V>());
            }
            return entries;
        }

        /// <summary>
        /// Returns a String representation of the index data for debugging purposes.
        /// </summary>
        /// <returns> the string representation </returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder(256);
            SortFields();
            int sumPositions = 0;
            int sumTerms = 0;
            BytesRef spare = new BytesRef();
            for (int i = 0; i < sortedFields.Length; i++)
            {
                KeyValuePair<string, Info> entry = sortedFields[i];
                string fieldName = entry.Key;
                Info info = entry.Value;
                info.SortTerms();
                result.Append(fieldName + ":\n");
                SliceByteStartArray sliceArray = info.sliceArray;
                int numPositions = 0;
                Int32BlockPool.SliceReader postingsReader = new Int32BlockPool.SliceReader(intBlockPool);
                for (int j = 0; j < info.terms.Count; j++)
                {
                    int ord = info.sortedTerms[j];
                    info.terms.Get(ord, spare);
                    int freq = sliceArray.freq[ord];
                    result.Append("\t'" + spare + "':" + freq + ":");
                    postingsReader.Reset(sliceArray.start[ord], sliceArray.end[ord]);
                    result.Append(" [");
                    int iters = storeOffsets ? 3 : 1;
                    while (!postingsReader.IsEndOfSlice)
                    {
                        result.Append('(');

                        for (int k = 0; k < iters; k++)
                        {
                            result.Append(postingsReader.ReadInt32());
                            if (k < iters - 1)
                            {
                                result.Append(", ");
                            }
                        }
                        result.Append(')');
                        if (!postingsReader.IsEndOfSlice)
                        {
                            result.Append(',');
                        }

                    }
                    result.Append(']');
                    result.Append('\n');
                    numPositions += freq;
                }

                result.Append("\tterms=" + info.terms.Count);
                result.Append(", positions=" + numPositions);
                result.Append(", memory=" + RamUsageEstimator.HumanReadableUnits(RamUsageEstimator.SizeOf(info)));
                result.Append('\n');
                sumPositions += numPositions;
                sumTerms += info.terms.Count;
            }

            result.Append("\nfields=" + sortedFields.Length);
            result.Append(", terms=" + sumTerms);
            result.Append(", positions=" + sumPositions);
            result.Append(", memory=" + RamUsageEstimator.HumanReadableUnits(GetMemorySize()));
            return result.ToString();
        }

        /// <summary>
        /// Resets the <seealso cref="MemoryIndex"/> to its initial state and recycles all internal buffers.
        /// </summary>
        public virtual void Reset()
        {
            this.fieldInfos.Clear();
            this.fields.Clear();
            this.sortedFields = null;
            byteBlockPool.Reset(false, false); // no need to 0-fill the buffers
            intBlockPool.Reset(true, false); // here must must 0-fill since we use slices
        }

        internal sealed class SliceByteStartArray : BytesRefHash.DirectBytesStartArray
        {
            internal int[] start; // the start offset in the IntBlockPool per term
            internal int[] end; // the end pointer in the IntBlockPool for the postings slice per term
            internal int[] freq; // the term frequency

            public SliceByteStartArray(int initSize) : base(initSize)
            {
            }

            public override int[] Init()
            {
                int[] ord = base.Init();
                start = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT32)];
                end = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT32)];
                freq = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT32)];
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(start.Length >= ord.Length);
                    Debugging.Assert(end.Length >= ord.Length);
                    Debugging.Assert(freq.Length >= ord.Length);
                }
                return ord;
            }

            public override int[] Grow()
            {
                int[] ord = base.Grow();
                if (start.Length < ord.Length)
                {
                    start = ArrayUtil.Grow(start, ord.Length);
                    end = ArrayUtil.Grow(end, ord.Length);
                    freq = ArrayUtil.Grow(freq, ord.Length);
                }
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(start.Length >= ord.Length);
                    Debugging.Assert(end.Length >= ord.Length);
                    Debugging.Assert(freq.Length >= ord.Length);
                }
                return ord;
            }

            public override int[] Clear()
            {
                start = end = null;
                return base.Clear();
            }
        }
    }
}