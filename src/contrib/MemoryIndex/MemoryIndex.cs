/**
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Search;
using Lucene.Net.Support;

namespace Lucene.Net.Index.Memory {
    
    /**
 * High-performance single-document main memory Apache Lucene fulltext search index. 
 * 
 * <h4>Overview</h4>
 * 
 * This class is a replacement/substitute for a large subset of
 * {@link RAMDirectory} functionality. It is designed to
 * enable maximum efficiency for on-the-fly matchmaking combining structured and 
 * fuzzy fulltext search in realtime streaming applications such as Nux XQuery based XML 
 * message queues, publish-subscribe systems for Blogs/newsfeeds, text chat, data acquisition and 
 * distribution systems, application level routers, firewalls, classifiers, etc. 
 * Rather than targeting fulltext search of infrequent queries over huge persistent 
 * data archives (historic search), this class targets fulltext search of huge 
 * numbers of queries over comparatively small transient realtime data (prospective 
 * search). 
 * For example as in 
 * <pre>
 * float score = search(String text, Query query)
 * </pre>
 * <p>
 * Each instance can hold at most one Lucene "document", with a document containing
 * zero or more "fields", each field having a name and a fulltext value. The
 * fulltext value is tokenized (split and transformed) into zero or more index terms 
 * (aka words) on <code>addField()</code>, according to the policy implemented by an
 * Analyzer. For example, Lucene analyzers can split on whitespace, normalize to lower case
 * for case insensitivity, ignore common terms with little discriminatory value such as "he", "in", "and" (stop
 * words), reduce the terms to their natural linguistic root form such as "fishing"
 * being reduced to "fish" (stemming), resolve synonyms/inflexions/thesauri 
 * (upon indexing and/or querying), etc. For details, see
 * <a target="_blank" href="http://today.java.net/pub/a/today/2003/07/30/LuceneIntro.html">Lucene Analyzer Intro</a>.
 * <p>
 * Arbitrary Lucene queries can be run against this class - see <a target="_blank" 
 * href="../../../../../../../queryparsersyntax.html">Lucene Query Syntax</a>
 * as well as <a target="_blank" 
 * href="http://today.java.net/pub/a/today/2003/11/07/QueryParserRules.html">Query Parser Rules</a>.
 * Note that a Lucene query selects on the field names and associated (indexed) 
 * tokenized terms, not on the original fulltext(s) - the latter are not stored 
 * but rather thrown away immediately after tokenization.
 * <p>
 * For some interesting background information on search technology, see Bob Wyman's
 * <a target="_blank" 
 * href="http://bobwyman.pubsub.com/main/2005/05/mary_hodder_poi.html">Prospective Search</a>, 
 * Jim Gray's
 * <a target="_blank" href="http://www.acmqueue.org/modules.php?name=Content&pa=showpage&pid=293&page=4">
 * A Call to Arms - Custom subscriptions</a>, and Tim Bray's
 * <a target="_blank" 
 * href="http://www.tbray.org/ongoing/When/200x/2003/07/30/OnSearchTOC">On Search, the Series</a>.
 * 
 * 
 * <h4>Example Usage</h4> 
 * 
 * <pre>
 * Analyzer analyzer = PatternAnalyzer.DEFAULT_ANALYZER;
 * //Analyzer analyzer = new SimpleAnalyzer();
 * MemoryIndex index = new MemoryIndex();
 * index.addField("content", "Readings about Salmons and other select Alaska fishing Manuals", analyzer);
 * index.addField("author", "Tales of James", analyzer);
 * QueryParser parser = new QueryParser("content", analyzer);
 * float score = index.search(parser.parse("+author:james +salmon~ +fish* manual~"));
 * if (score &gt; 0.0f) {
 *     System.out.println("it's a match");
 * } else {
 *     System.out.println("no match found");
 * }
 * System.out.println("indexData=" + index.toString());
 * </pre>
 * 
 * 
 * <h4>Example XQuery Usage</h4> 
 * 
 * <pre>
 * (: An XQuery that finds all books authored by James that have something to do with "salmon fishing manuals", sorted by relevance :)
 * declare namespace lucene = "java:nux.xom.pool.FullTextUtil";
 * declare variable $query := "+salmon~ +fish* manual~"; (: any arbitrary Lucene query can go here :)
 * 
 * for $book in /books/book[author="James" and lucene:match(abstract, $query) > 0.0]
 * let $score := lucene:match($book/abstract, $query)
 * order by $score descending
 * return $book
 * </pre>
 * 
 * 
 * <h4>No thread safety guarantees</h4>
 * 
 * An instance can be queried multiple times with the same or different queries,
 * but an instance is not thread-safe. If desired use idioms such as:
 * <pre>
 * MemoryIndex index = ...
 * synchronized (index) {
 *    // read and/or write index (i.e. add fields and/or query)
 * } 
 * </pre>
 * 
 * 
 * <h4>Performance Notes</h4>
 * 
 * Internally there's a new data structure geared towards efficient indexing 
 * and searching, plus the necessary support code to seamlessly plug into the Lucene 
 * framework.
 * <p>
 * This class performs very well for very small texts (e.g. 10 chars) 
 * as well as for large texts (e.g. 10 MB) and everything in between. 
 * Typically, it is about 10-100 times faster than <code>RAMDirectory</code>.
 * Note that <code>RAMDirectory</code> has particularly 
 * large efficiency overheads for small to medium sized texts, both in time and space.
 * Indexing a field with N tokens takes O(N) in the best case, and O(N logN) in the worst 
 * case. Memory consumption is probably larger than for <code>RAMDirectory</code>.
 * <p>
 * Example throughput of many simple term queries over a single MemoryIndex: 
 * ~500000 queries/sec on a MacBook Pro, jdk 1.5.0_06, server VM. 
 * As always, your mileage may vary.
 * <p>
 * If you're curious about
 * the whereabouts of bottlenecks, run java 1.5 with the non-perturbing '-server
 * -agentlib:hprof=cpu=samples,depth=10' flags, then study the trace log and
 * correlate its hotspot trailer with its call stack headers (see <a
 * target="_blank"
 * href="http://java.sun.com/developer/technicalArticles/Programming/HPROF.html">
 * hprof tracing </a>).
 *
 */
    public class MemoryIndex : ISerializable 
    {

        private static readonly SortedDictionary<String,Info> sortedFields = new SortedDictionary<String,Info>();
  
        /** pos: positions[3*i], startOffset: positions[3*i +1], endOffset: positions[3*i +2] */
        private readonly int stride;
  
        /** Could be made configurable; See {@link Document#setBoost(float)} */
        private const float docBoost = 1.0f;
  
        private static long serialVersionUID = 2782195016849084649L;

        private static bool DEBUG = false;
  
        #region Constructors
    
        /// <summary>
        /// Constructs and empty instance
        /// </summary>
        public MemoryIndex() : this(false) 
        {}

        /// <summary>
        /// Constructs an empty instance that can optionally store the start and end
        /// character offset of each token term in the text. This can be useful for
        /// highlighting of hit locations with the Lucene highlighter package.
        /// Private until the highlighter package matures, so that this can actually
        /// be meaningfully integrated.
        /// </summary>
        /// <param>
        /// storeOffsets: weather or not to store thes start and end character offest of each 
        /// token temr in the text
        /// </param>
        private MemoryIndex(bool storeOffsets) {
            this.stride = storeOffsets ? 3 : 1;
        }
  
        #endregion
    
        /// <summary>
        /// Convenience method; Creates and returns a token stream that generates a
        /// token for each keyword in the given collection, "as is", without any
        /// transforming text analysis. The resulting token stream can be fed into
        /// {@link #addField(String, TokenStream)}, perhaps wrapped into another
        /// {@link org.apache.lucene.analysis.TokenFilter}, as desired.
        /// 
        /// @param keywords
        ///            the keywords to generate tokens for
        /// @return the corresponding token stream
        /// </summary>
        public TokenStream KeywordTokenStream<T>(IList<T> keywords) 
        {
            if (keywords == null)
                throw new ArgumentException("keywords must not be null");

            return new MemoryIndexTokenStream(keywords);
        }
  
        /// <summary>
        /// Convenience method; Tokenizes the given field text and adds the resulting
        /// terms to the index; Equivalent to adding an indexed non-keyword Lucene
        /// {@link org.apache.lucene.document.Field} that is
        /// {@link org.apache.lucene.document.Field.Index#ANALYZED tokenized},
        /// {@link org.apache.lucene.document.Field.Store#NO not stored},
        /// {@link org.apache.lucene.document.Field.TermVector#WITH_POSITIONS termVectorStored with positions} (or
        /// {@link org.apache.lucene.document.Field.TermVector#WITH_POSITIONS termVectorStored with positions and offsets}),
        /// 
        /// @param fieldName
        ///            a name to be associated with the text
        /// @param text
        ///            the text to tokenize and index.
        /// @param analyzer
        ///            the analyzer to use for tokenization
        /// </summary>
        public void AddField(String fieldName, String text, Analyzer analyzer) 
        {
            if (fieldName == null)
                throw new ArgumentException("fieldName must not be null");
            if (text == null)
                throw new ArgumentException("text must not be null");
            if (analyzer == null)
                throw new ArgumentException("analyzer must not be null");
    
            TokenStream stream = analyzer.TokenStream(fieldName, new StringReader(text));

            AddField(fieldName, stream);
        }
    
        /// <summary>
        /// Equivalent to <code>AddField(fieldName, stream 1.0f)</code>
        /// </summary>
        /// <param name="fieldName">A name to be associated with the text</param>
        /// <param name="stream">The token stream to retrieve tokens from</param>
        public void AddField(String fieldName, TokenStream stream) {
            AddField(fieldName, stream, 1.0f);
        }

        /// <summary>
        /// Iterate over the given token stream and adds the resulting terms to the index;
        /// Equivalent to adding a tokenized, indexed, termVectorStored, unstored, Lucene 
        /// <see>Lucene.Net.Documents.Field</see>. Finally closes the token stream.
        /// 
        /// Note: Untokenized keywords can be added with this method via {@link #keywordTokenStream(Collection)}, 
        /// the Lucene contrib <code>KeywordTokenizer</code> or similar utilities.
        /// 
        /// </summary>
        /// <param name="fieldName">A name to be associated with the text</param>
        /// <param name="boost">The token stream to retrieve tokens from</param>
        /// <param name="stream">The boost factor for hits for this field 
        ///     <seealso>Lucene.Net.Documents.Field#SetBoost</seealso>
        /// </param>
        public void AddField(String fieldName, TokenStream stream, float boost) 
        {
            try {
                if (fieldName == null)
                    throw new ArgumentException("fieldName must not be null");
        
                if (stream == null)
                    throw new ArgumentException("token stream must not be null");
        
                if (boost <= 0.0f)
                    throw new ArgumentException("boost factor must be greater than 0.0");
        
                if (sortedFields[fieldName] != null)
                    throw new ArgumentException("field must not be added more than once");
      
                var terms = new Dictionary<String,List<Int32>>();
                int numTokens = 0;
                int numOverlapTokens = 0;
                int pos = -1;
      
                var termAtt = stream.AddAttribute<TermAttribute>();
                var posIncrAttribute = stream.AddAttribute<PositionIncrementAttribute>();
                var offsetAtt = stream.AddAttribute<OffsetAttribute>();

                stream.Reset();
                
                while (stream.IncrementToken())
                {
                    var term = termAtt.Term();
                    if (term.Length == 0) continue; // nothing to do

                    numTokens++;
                    int posIncr = posIncrAttribute.PositionIncrement;

                    if (posIncr == 0)
                        numOverlapTokens++;

                    pos += posIncr;

                    List<Int32> positions = terms[term];
                    if (positions == null) 
                    { 
                        positions = new List<Int32>();
                        terms.Add(term, positions);
                    }

                    if (stride == 1) 
                    {
                        positions.Add(pos);
                    } 
                    else 
                    {
                        positions.Insert(offsetAtt.StartOffset, pos);
                    }
                }
            
                stream.End();

                // ensure infos.numTokens > 0 invariant; needed for correct operation of terms()
                if (numTokens > 0) 
                {
                    boost = boost * docBoost; // see DocumentWriter.addDocument(...)
                    sortedFields.Add(fieldName, new Info(terms, numTokens, numOverlapTokens, boost));
                    sortedFields = null;    // invalidate sorted view, if any
                }
            } finally {
                try {
                    if (stream != null) stream.Dispose();
                } catch (IOException e2) {
                    throw new SystemException("Error Disposing of Stream", e2);
                }
            }
        }
  
        /// <summary>
        /// Creates and returns a searcher that can be used to execute arbitrary
        /// Lucene queries and to collect the resulting query results as hits.
        /// </summary>
        /// <returns>A searcher</returns>
        public IndexSearcher CreateSearcher() {
            MemoryIndexReader reader = new MemoryIndexReader();
            var searcher = new IndexSearcher(reader); // ensures no auto-close !!
            reader.SetSearcher(searcher); // to later get hold of searcher.getSimilarity()
            return searcher;
        }
  
        /**
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
    
            Searcher searcher = createSearcher();
            
            List<float> scores = new List<float>(); // inits to 0.0f (no match)
            searcher.Search(query, new AnonCollector());
            float score = scores[0];
            return score;
        }
  
        /// <summary>
        /// Returns a reasonable approximation of the main memory [bytes] consumed by
        /// this instance. Useful for smart memory sensititive caches/pools. Assumes
        /// fieldNames are interned, whereas tokenized terms are memory-overlaid.
        /// </summary>
        public int GetMemorySize()
        {
            return Marshal.SizeOf(this);
        } 

        private int NumPositions(ICollection<int> positions) {
            return positions.Count / stride;
        }
  
        /**
        * Returns a String representation of the index data for debugging purposes.
        * 
        * @return the string representation
        */
        public override String ToString() {
            var result = new StringBuilder(256);    
            int sumChars = 0;
            int sumPositions = 0;
            int sumTerms = 0;

            foreach (var sortedField in sortedFields)
            {
                result.Append(sortedField.Key + ":\n");

                int numChars = 0;
                int numPositions = 0;

                foreach (var field in sortedField.Value.SortedTerms)
                {
                    result.Append("\t'" + field.Key + "':" + NumPositions(field.Value) + ":");
                    result.Append(field.Value.ToString()); 
                    result.Append("\n");
                    numPositions += NumPositions(field.Value);
                    numChars += field.Key.Length;
                }

                result.Append("\tterms=" + sortedField.Value.SortedTerms.Count);
                result.Append(", positions=" + numPositions);
                result.Append(", Kchars=" + (numChars/1000.0f));
                result.Append("\n");
                sumPositions += numPositions;
                sumChars += numChars;
                sumTerms += sortedField.Value.SortedTerms.Count;
            
            }
    
            result.Append("\nfields=" + sortedFields.Count);
            result.Append(", terms=" + sumTerms);
            result.Append(", positions=" + sumPositions);
            result.Append(", Kchars=" + (sumChars/1000.0f));

            return result.ToString();
        }
   

        #region Implementation of ISerializable

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
   
    internal class AnonCollector : Collector
    {
        private Scorer _scorer;

        #region Overrides of Collector

        public override void SetScorer(Scorer scorer)
        {
            _scorer = scorer;
        }

        public override void Collect(int doc)
        {
            scorer[0] = _scorer.Score();
        }

        public override void SetNextReader(IndexReader reader, int docBase) {}

        public override bool AcceptsDocsOutOfOrder
        {
            get { return true; }
        }

        #endregion
    }
 
}
