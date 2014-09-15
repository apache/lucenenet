using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Memory
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
	/// <seealso cref="RAMDirectory"/> functionality. It is designed to
	/// enable maximum efficiency for on-the-fly matchmaking combining structured and 
	/// fuzzy fulltext search in realtime streaming applications such as Nux XQuery based XML 
	/// message queues, publish-subscribe systems for Blogs/newsfeeds, text chat, data acquisition and 
	/// distribution systems, application level routers, firewalls, classifiers, etc. 
	/// Rather than targeting fulltext search of infrequent queries over huge persistent 
	/// data archives (historic search), this class targets fulltext search of huge 
	/// numbers of queries over comparatively small transient realtime data (prospective 
	/// search). 
	/// For example as in 
	/// <pre class="prettyprint">
	/// float score = search(String text, Query query)
	/// </pre>
	/// <para>
	/// Each instance can hold at most one Lucene "document", with a document containing
	/// zero or more "fields", each field having a name and a fulltext value. The
	/// fulltext value is tokenized (split and transformed) into zero or more index terms 
	/// (aka words) on <code>addField()</code>, according to the policy implemented by an
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
	/// <a target="_blank" href="http://www.acmqueue.org/modules.php?name=Content&pa=showpage&pid=293&page=4">
	/// A Call to Arms - Custom subscriptions</a>, and Tim Bray's
	/// <a target="_blank" 
	/// href="http://www.tbray.org/ongoing/When/200x/2003/07/30/OnSearchTOC">On Search, the Series</a>.
	/// 
	/// 
	/// <h4>Example Usage</h4> 
	/// 
	/// <pre class="prettyprint">
	/// Analyzer analyzer = new SimpleAnalyzer(version);
	/// MemoryIndex index = new MemoryIndex();
	/// index.addField("content", "Readings about Salmons and other select Alaska fishing Manuals", analyzer);
	/// index.addField("author", "Tales of James", analyzer);
	/// QueryParser parser = new QueryParser(version, "content", analyzer);
	/// float score = index.search(parser.parse("+author:james +salmon~ +fish* manual~"));
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
	/// <pre class="prettyprint">
	/// (: An XQuery that finds all books authored by James that have something to do with "salmon fishing manuals", sorted by relevance :)
	/// declare namespace lucene = "java:nux.xom.pool.FullTextUtil";
	/// declare variable $query := "+salmon~ +fish* manual~"; (: any arbitrary Lucene query can go here :)
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
	/// <pre class="prettyprint">
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
	/// </para>
	/// <para>
	/// This class performs very well for very small texts (e.g. 10 chars) 
	/// as well as for large texts (e.g. 10 MB) and everything in between. 
	/// Typically, it is about 10-100 times faster than <code>RAMDirectory</code>.
	/// Note that <code>RAMDirectory</code> has particularly 
	/// large efficiency overheads for small to medium sized texts, both in time and space.
	/// Indexing a field with N tokens takes O(N) in the best case, and O(N logN) in the worst 
	/// case. Memory consumption is probably larger than for <code>RAMDirectory</code>.
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
	public class MemoryIndex
	{

	  /// <summary>
	  /// info for each field: Map<String fieldName, Info field> </summary>
	  private readonly Dictionary<string, Info> fields = new Dictionary<string, Info>();

	  /// <summary>
	  /// fields sorted ascending by fieldName; lazily computed on demand </summary>
	  [NonSerialized]
	  private KeyValuePair<string, Info>[] sortedFields;

	  private readonly bool storeOffsets;

	  private const bool DEBUG = false;

	  private readonly ByteBlockPool byteBlockPool;
	  private readonly IntBlockPool intBlockPool;
	//  private final IntBlockPool.SliceReader postingsReader;
	  private readonly IntBlockPool.SliceWriter postingsWriter;

	  private Dictionary<string, FieldInfo> fieldInfos = new Dictionary<string, FieldInfo>();

	  private Counter bytesUsed;

	  /// <summary>
	  /// Sorts term entries into ascending order; also works for
	  /// Arrays.binarySearch() and Arrays.sort()
	  /// </summary>
	  private static readonly IComparer<object> termComparator = new ComparatorAnonymousInnerClassHelper();

	  private class ComparatorAnonymousInnerClassHelper : IComparer<object>
	  {
		  public ComparatorAnonymousInnerClassHelper()
		  {
		  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @SuppressWarnings({"unchecked","rawtypes"}) public int compare(Object o1, Object o2)
		  public virtual int Compare(object o1, object o2)
		  {
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: if (o1 instanceof java.util.Map.Entry<?,?>)
			if (o1 is KeyValuePair<?, ?>)
			{
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: o1 = ((java.util.Map.Entry<?,?>) o1).getKey();
				o1 = ((KeyValuePair<?, ?>) o1).Key;
			}
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: if (o2 instanceof java.util.Map.Entry<?,?>)
			if (o2 is KeyValuePair<?, ?>)
			{
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: o2 = ((java.util.Map.Entry<?,?>) o2).getKey();
				o2 = ((KeyValuePair<?, ?>) o2).Key;
			}
			if (o1 == o2)
			{
				return 0;
			}
			return ((IComparable) o1).CompareTo((IComparable) o2);
		  }
	  }

	  /// <summary>
	  /// Constructs an empty instance.
	  /// </summary>
	  public MemoryIndex() : this(false)
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
	  public MemoryIndex(bool storeOffsets) : this(storeOffsets, 0)
	  {

	  }

	  /// <summary>
	  /// Expert: This constructor accepts an upper limit for the number of bytes that should be reused if this instance is <seealso cref="#reset()"/>. </summary>
	  /// <param name="storeOffsets"> <code>true</code> if offsets should be stored </param>
	  /// <param name="maxReusedBytes"> the number of bytes that should remain in the internal memory pools after <seealso cref="#reset()"/> is called </param>
	  internal MemoryIndex(bool storeOffsets, long maxReusedBytes)
	  {
		this.storeOffsets = storeOffsets;
		this.bytesUsed = Counter.newCounter();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxBufferedByteBlocks = (int)((maxReusedBytes/2) / org.apache.lucene.util.ByteBlockPool.BYTE_BLOCK_SIZE);
		int maxBufferedByteBlocks = (int)((maxReusedBytes / 2) / ByteBlockPool.BYTE_BLOCK_SIZE);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxBufferedIntBlocks = (int)((maxReusedBytes - (maxBufferedByteBlocks*org.apache.lucene.util.ByteBlockPool.BYTE_BLOCK_SIZE))/(org.apache.lucene.util.IntBlockPool.INT_BLOCK_SIZE * org.apache.lucene.util.RamUsageEstimator.NUM_BYTES_INT));
		int maxBufferedIntBlocks = (int)((maxReusedBytes - (maxBufferedByteBlocks * ByteBlockPool.BYTE_BLOCK_SIZE)) / (IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT));
		assert(maxBufferedByteBlocks * ByteBlockPool.BYTE_BLOCK_SIZE) + (maxBufferedIntBlocks * IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT) <= maxReusedBytes;
		byteBlockPool = new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, maxBufferedByteBlocks, bytesUsed));
		intBlockPool = new IntBlockPool(new RecyclingIntBlockAllocator(IntBlockPool.INT_BLOCK_SIZE, maxBufferedIntBlocks, bytesUsed));
		postingsWriter = new SliceWriter(intBlockPool);
	  }

	  /// <summary>
	  /// Convenience method; Tokenizes the given field text and adds the resulting
	  /// terms to the index; Equivalent to adding an indexed non-keyword Lucene
	  /// <seealso cref="org.apache.lucene.document.Field"/> that is tokenized, not stored,
	  /// termVectorStored with positions (or termVectorStored with positions and offsets),
	  /// </summary>
	  /// <param name="fieldName">
	  ///            a name to be associated with the text </param>
	  /// <param name="text">
	  ///            the text to tokenize and index. </param>
	  /// <param name="analyzer">
	  ///            the analyzer to use for tokenization </param>
	  public virtual void addField(string fieldName, string text, Analyzer analyzer)
	  {
		if (fieldName == null)
		{
		  throw new System.ArgumentException("fieldName must not be null");
		}
		if (text == null)
		{
		  throw new System.ArgumentException("text must not be null");
		}
		if (analyzer == null)
		{
		  throw new System.ArgumentException("analyzer must not be null");
		}

		TokenStream stream;
		try
		{
		  stream = analyzer.TokenStream(fieldName, text);
		}
		catch (IOException ex)
		{
		  throw new Exception(ex);
		}

		addField(fieldName, stream, 1.0f, analyzer.GetPositionIncrementGap(fieldName), analyzer.GetOffsetGap(fieldName));
	  }

	  /// <summary>
	  /// Convenience method; Creates and returns a token stream that generates a
	  /// token for each keyword in the given collection, "as is", without any
	  /// transforming text analysis. The resulting token stream can be fed into
	  /// <seealso cref="#addField(String, TokenStream)"/>, perhaps wrapped into another
	  /// <seealso cref="org.apache.lucene.analysis.TokenFilter"/>, as desired.
	  /// </summary>
	  /// <param name="keywords">
	  ///            the keywords to generate tokens for </param>
	  /// <returns> the corresponding token stream </returns>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public <T> org.apache.lucene.analysis.TokenStream keywordTokenStream(final java.util.Collection<T> keywords)
	  public virtual TokenStream keywordTokenStream<T>(ICollection<T> keywords)
	  {
		// TODO: deprecate & move this method into AnalyzerUtil?
		if (keywords == null)
		{
		  throw new System.ArgumentException("keywords must not be null");
		}

		return new TokenStreamAnonymousInnerClassHelper(this, keywords);
	  }

	  private class TokenStreamAnonymousInnerClassHelper : TokenStream
	  {
		  private readonly MemoryIndex outerInstance;

		  private ICollection<T> keywords;

		  public TokenStreamAnonymousInnerClassHelper(MemoryIndex outerInstance, ICollection<T> keywords)
		  {
			  this.outerInstance = outerInstance;
			  this.keywords = keywords;
			  iter = keywords.GetEnumerator();
			  start = 0;
			  termAtt = addAttribute(typeof(CharTermAttribute));
			  offsetAtt = addAttribute(typeof(OffsetAttribute));
		  }

		  private IEnumerator<T> iter;
		  private int start;
		  private readonly CharTermAttribute termAtt;
		  private readonly OffsetAttribute offsetAtt;

		  public override bool incrementToken()
		  {
			if (!iter.hasNext())
			{
				return false;
			}

			T obj = iter.next();
			if (obj == null)
			{
			  throw new System.ArgumentException("keyword must not be null");
			}

			string term = obj.ToString();
			clearAttributes();
			termAtt.setEmpty().append(term);
			offsetAtt.setOffset(start, start + termAtt.length());
			start += term.Length + 1; // separate words by 1 (blank) character
			return true;
		  }
	  }

	  /// <summary>
	  /// Equivalent to <code>addField(fieldName, stream, 1.0f)</code>.
	  /// </summary>
	  /// <param name="fieldName">
	  ///            a name to be associated with the text </param>
	  /// <param name="stream">
	  ///            the token stream to retrieve tokens from </param>
	  public virtual void addField(string fieldName, TokenStream stream)
	  {
		addField(fieldName, stream, 1.0f);
	  }

	  /// <summary>
	  /// Iterates over the given token stream and adds the resulting terms to the index;
	  /// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
	  /// Lucene <seealso cref="org.apache.lucene.document.Field"/>.
	  /// Finally closes the token stream. Note that untokenized keywords can be added with this method via 
	  /// <seealso cref="#keywordTokenStream(Collection)"/>, the Lucene <code>KeywordTokenizer</code> or similar utilities.
	  /// </summary>
	  /// <param name="fieldName">
	  ///            a name to be associated with the text </param>
	  /// <param name="stream">
	  ///            the token stream to retrieve tokens from. </param>
	  /// <param name="boost">
	  ///            the boost factor for hits for this field
	  /// </param>
	  /// <seealso cref= org.apache.lucene.document.Field#setBoost(float) </seealso>
	  public virtual void addField(string fieldName, TokenStream stream, float boost)
	  {
		addField(fieldName, stream, boost, 0);
	  }


	  /// <summary>
	  /// Iterates over the given token stream and adds the resulting terms to the index;
	  /// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
	  /// Lucene <seealso cref="org.apache.lucene.document.Field"/>.
	  /// Finally closes the token stream. Note that untokenized keywords can be added with this method via
	  /// <seealso cref="#keywordTokenStream(Collection)"/>, the Lucene <code>KeywordTokenizer</code> or similar utilities.
	  /// </summary>
	  /// <param name="fieldName">
	  ///            a name to be associated with the text </param>
	  /// <param name="stream">
	  ///            the token stream to retrieve tokens from. </param>
	  /// <param name="boost">
	  ///            the boost factor for hits for this field
	  /// </param>
	  /// <param name="positionIncrementGap">
	  ///            the position increment gap if fields with the same name are added more than once
	  /// 
	  /// </param>
	  /// <seealso cref= org.apache.lucene.document.Field#setBoost(float) </seealso>
	  public virtual void addField(string fieldName, TokenStream stream, float boost, int positionIncrementGap)
	  {
		addField(fieldName, stream, boost, positionIncrementGap, 1);
	  }

	  /// <summary>
	  /// Iterates over the given token stream and adds the resulting terms to the index;
	  /// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
	  /// Lucene <seealso cref="org.apache.lucene.document.Field"/>.
	  /// Finally closes the token stream. Note that untokenized keywords can be added with this method via 
	  /// <seealso cref="#keywordTokenStream(Collection)"/>, the Lucene <code>KeywordTokenizer</code> or similar utilities.
	  /// 
	  /// </summary>
	  /// <param name="fieldName">
	  ///            a name to be associated with the text </param>
	  /// <param name="stream">
	  ///            the token stream to retrieve tokens from. </param>
	  /// <param name="boost">
	  ///            the boost factor for hits for this field </param>
	  /// <param name="positionIncrementGap">
	  ///            the position increment gap if fields with the same name are added more than once </param>
	  /// <param name="offsetGap">
	  ///            the offset gap if fields with the same name are added more than once </param>
	  /// <seealso cref= org.apache.lucene.document.Field#setBoost(float) </seealso>
	  public virtual void addField(string fieldName, TokenStream stream, float boost, int positionIncrementGap, int offsetGap)
	  {
		try
		{
		  if (fieldName == null)
		  {
			throw new System.ArgumentException("fieldName must not be null");
		  }
		  if (stream == null)
		  {
			  throw new System.ArgumentException("token stream must not be null");
		  }
		  if (boost <= 0.0f)
		  {
			  throw new System.ArgumentException("boost factor must be greater than 0.0");
		  }
		  int numTokens = 0;
		  int numOverlapTokens = 0;
		  int pos = -1;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.BytesRefHash terms;
		  BytesRefHash terms;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SliceByteStartArray sliceArray;
		  SliceByteStartArray sliceArray;
		  Info info = null;
		  long sumTotalTermFreq = 0;
		  int offset = 0;
		  if ((info = fields[fieldName]) != null)
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
			fieldInfos[fieldName] = new FieldInfo(fieldName, true, fieldInfos.Count, false, false, false, this.storeOffsets ? FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS : FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, null, null, null);
		  }
		  TermToBytesRefAttribute termAtt = stream.getAttribute(typeof(TermToBytesRefAttribute));
		  PositionIncrementAttribute posIncrAttribute = stream.addAttribute(typeof(PositionIncrementAttribute));
		  OffsetAttribute offsetAtt = stream.addAttribute(typeof(OffsetAttribute));
		  BytesRef @ref = termAtt.BytesRef;
		  stream.reset();

		  while (stream.incrementToken())
		  {
			termAtt.fillBytesRef();
	//        if (DEBUG) System.err.println("token='" + term + "'");
			numTokens++;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int posIncr = posIncrAttribute.getPositionIncrement();
			int posIncr = posIncrAttribute.PositionIncrement;
			if (posIncr == 0)
			{
			  numOverlapTokens++;
			}
			pos += posIncr;
			int ord = terms.add(@ref);
			if (ord < 0)
			{
			  ord = (-ord) - 1;
			  postingsWriter.reset(sliceArray.end[ord]);
			}
			else
			{
			  sliceArray.start[ord] = postingsWriter.startNewSlice();
			}
			sliceArray.freq[ord]++;
			sumTotalTermFreq++;
			if (!storeOffsets)
			{
			  postingsWriter.writeInt(pos);
			}
			else
			{
			  postingsWriter.writeInt(pos);
			  postingsWriter.writeInt(offsetAtt.startOffset() + offset);
			  postingsWriter.writeInt(offsetAtt.endOffset() + offset);
			}
			sliceArray.end[ord] = postingsWriter.CurrentOffset;
		  }
		  stream.end();

		  // ensure infos.numTokens > 0 invariant; needed for correct operation of terms()
		  if (numTokens > 0)
		  {
			fields[fieldName] = new Info(terms, sliceArray, numTokens, numOverlapTokens, boost, pos, offsetAtt.endOffset() + offset, sumTotalTermFreq);
			sortedFields = null; // invalidate sorted view, if any
		  }
		} // can never happen
		catch (Exception e)
		{
		  throw new Exception(e);
		}
		finally
		{
		  try
		  {
			if (stream != null)
			{
			  stream.close();
			}
		  }
		  catch (IOException e2)
		  {
			throw new Exception(e2);
		  }
		}
	  }

	  /// <summary>
	  /// Creates and returns a searcher that can be used to execute arbitrary
	  /// Lucene queries and to collect the resulting query results as hits.
	  /// </summary>
	  /// <returns> a searcher </returns>
	  public virtual IndexSearcher createSearcher()
	  {
		MemoryIndexReader reader = new MemoryIndexReader(this);
		IndexSearcher searcher = new IndexSearcher(reader); // ensures no auto-close !!
		reader.Searcher = searcher; // to later get hold of searcher.getSimilarity()
		return searcher;
	  }

	  /// <summary>
	  /// Convenience method that efficiently returns the relevance score by
	  /// matching this index against the given Lucene query expression.
	  /// </summary>
	  /// <param name="query">
	  ///            an arbitrary Lucene query to run against this index </param>
	  /// <returns> the relevance score of the matchmaking; A number in the range
	  ///         [0.0 .. 1.0], with 0.0 indicating no match. The higher the number
	  ///         the better the match.
	  ///  </returns>
	  public virtual float Search(Query query)
	  {
		if (query == null)
		{
		  throw new System.ArgumentException("query must not be null");
		}

		IndexSearcher searcher = createSearcher();
		try
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float[] scores = new float[1];
		  float[] scores = new float[1]; // inits to 0.0f (no match)
		  searcher.search(query, new CollectorAnonymousInnerClassHelper(this, scores));
		  float score = scores[0];
		  return score;
		} // can never happen (RAMDirectory)
		catch (IOException e)
		{
		  throw new Exception(e);
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

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly MemoryIndex outerInstance;

		  private float[] scores;

		  public CollectorAnonymousInnerClassHelper(MemoryIndex outerInstance, float[] scores)
		  {
			  this.outerInstance = outerInstance;
			  this.scores = scores;
		  }

		  private Scorer scorer;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void collect(int doc) throws java.io.IOException
		  public override void collect(int doc)
		  {
			scores[0] = scorer.score();
		  }

		  public override Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }

		  public override bool acceptsDocsOutOfOrder()
		  {
			return true;
		  }

		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
			  }
		  }
	  }

	  /// <summary>
	  /// Returns a reasonable approximation of the main memory [bytes] consumed by
	  /// this instance. Useful for smart memory sensititive caches/pools. </summary>
	  /// <returns> the main memory consumption </returns>
	  public virtual long MemorySize
	  {
		  get
		  {
			return RamUsageEstimator.sizeOf(this);
		  }
	  }

	  /// <summary>
	  /// sorts into ascending order (on demand), reusing memory along the way </summary>
	  private void sortFields()
	  {
		if (sortedFields == null)
		{
			sortedFields = sort(fields);
		}
	  }

	  /// <summary>
	  /// returns a view of the given map's entries, sorted ascending by key </summary>
	  private static KeyValuePair<K, V>[] sort<K, V>(Dictionary<K, V> map)
	  {
		int size = map.Count;
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") java.util.Map.Entry<K,V>[] entries = new java.util.Map.Entry[size];
		KeyValuePair<K, V>[] entries = new DictionaryEntry[size];

		IEnumerator<KeyValuePair<K, V>> iter = map.SetOfKeyValuePairs().GetEnumerator();
		for (int i = 0; i < size; i++)
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  entries[i] = iter.next();
		}

		if (size > 1)
		{
			ArrayUtil.introSort(entries, termComparator);
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
		sortFields();
		int sumPositions = 0;
		int sumTerms = 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.BytesRef spare = new org.apache.lucene.util.BytesRef();
		BytesRef spare = new BytesRef();
		for (int i = 0; i < sortedFields.Length; i++)
		{
		  KeyValuePair<string, Info> entry = sortedFields[i];
		  string fieldName = entry.Key;
		  Info info = entry.Value;
		  info.sortTerms();
		  result.Append(fieldName + ":\n");
		  SliceByteStartArray sliceArray = info.sliceArray;
		  int numPositions = 0;
		  SliceReader postingsReader = new SliceReader(intBlockPool);
		  for (int j = 0; j < info.terms.size(); j++)
		  {
			int ord = info.sortedTerms[j];
			info.terms.get(ord, spare);
			int freq = sliceArray.freq[ord];
			result.Append("\t'" + spare + "':" + freq + ":");
			postingsReader.reset(sliceArray.start[ord], sliceArray.end[ord]);
			result.Append(" [");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int iters = storeOffsets ? 3 : 1;
			int iters = storeOffsets ? 3 : 1;
			while (!postingsReader.endOfSlice())
			{
			  result.Append("(");

			  for (int k = 0; k < iters; k++)
			  {
				result.Append(postingsReader.readInt());
				if (k < iters - 1)
				{
				  result.Append(", ");
				}
			  }
			  result.Append(")");
			  if (!postingsReader.endOfSlice())
			  {
				result.Append(",");
			  }

			}
			result.Append("]");
			result.Append("\n");
			numPositions += freq;
		  }

		  result.Append("\tterms=" + info.terms.size());
		  result.Append(", positions=" + numPositions);
		  result.Append(", memory=" + RamUsageEstimator.humanReadableUnits(RamUsageEstimator.sizeOf(info)));
		  result.Append("\n");
		  sumPositions += numPositions;
		  sumTerms += info.terms.size();
		}

		result.Append("\nfields=" + sortedFields.Length);
		result.Append(", terms=" + sumTerms);
		result.Append(", positions=" + sumPositions);
		result.Append(", memory=" + RamUsageEstimator.humanReadableUnits(MemorySize));
		return result.ToString();
	  }

	  /// <summary>
	  /// Index data structure for a field; Contains the tokenized term texts and
	  /// their positions.
	  /// </summary>
	  private sealed class Info
	  {

		/// <summary>
		/// Term strings and their positions for this field: Map <String
		/// termText, ArrayIntList positions>
		/// </summary>
		internal readonly BytesRefHash terms;

		internal readonly SliceByteStartArray sliceArray;

		/// <summary>
		/// Terms sorted ascending by term text; computed on demand </summary>
		[NonSerialized]
		internal int[] sortedTerms;

		/// <summary>
		/// Number of added tokens for this field </summary>
		internal readonly int numTokens;

		/// <summary>
		/// Number of overlapping tokens for this field </summary>
		internal readonly int numOverlapTokens;

		/// <summary>
		/// Boost factor for hits for this field </summary>
		internal readonly float boost;

		internal readonly long sumTotalTermFreq;

		/// <summary>
		/// the last position encountered in this field for multi field support </summary>
		internal int lastPosition;

		/// <summary>
		/// the last offset encountered in this field for multi field support </summary>
		internal int lastOffset;

		public Info(BytesRefHash terms, SliceByteStartArray sliceArray, int numTokens, int numOverlapTokens, float boost, int lastPosition, int lastOffset, long sumTotalTermFreq)
		{
		  this.terms = terms;
		  this.sliceArray = sliceArray;
		  this.numTokens = numTokens;
		  this.numOverlapTokens = numOverlapTokens;
		  this.boost = boost;
		  this.sumTotalTermFreq = sumTotalTermFreq;
		  this.lastPosition = lastPosition;
		  this.lastOffset = lastOffset;
		}

		public long SumTotalTermFreq
		{
			get
			{
			  return sumTotalTermFreq;
			}
		}

		/// <summary>
		/// Sorts hashed terms into ascending order, reusing memory along the
		/// way. Note that sorting is lazily delayed until required (often it's
		/// not required at all). If a sorted view is required then hashing +
		/// sort + binary search is still faster and smaller than TreeMap usage
		/// (which would be an alternative and somewhat more elegant approach,
		/// apart from more sophisticated Tries / prefix trees).
		/// </summary>
		public void sortTerms()
		{
		  if (sortedTerms == null)
		  {
			sortedTerms = terms.sort(BytesRef.UTF8SortedAsUnicodeComparator);
		  }
		}

		public float Boost
		{
			get
			{
			  return boost;
			}
		}
	  }

	  ///////////////////////////////////////////////////////////////////////////////
	  // Nested classes:
	  ///////////////////////////////////////////////////////////////////////////////

	  /// <summary>
	  /// Search support for Lucene framework integration; implements all methods
	  /// required by the Lucene IndexReader contracts.
	  /// </summary>
	  private sealed class MemoryIndexReader : AtomicReader
	  {
		  private readonly MemoryIndex outerInstance;


		internal IndexSearcher searcher; // needed to find searcher.getSimilarity()

		internal MemoryIndexReader(MemoryIndex outerInstance) : base(); // avoid as much superclass baggage as possible
		{
			this.outerInstance = outerInstance;
		}

		internal Info getInfo(string fieldName)
		{
		  return outerInstance.fields[fieldName];
		}

		internal Info getInfo(int pos)
		{
		  return outerInstance.sortedFields[pos].Value;
		}

		public override Bits LiveDocs
		{
			get
			{
			  return null;
			}
		}

		public override FieldInfos FieldInfos
		{
			get
			{
			  return new FieldInfos(outerInstance.fieldInfos.Values.toArray(new FieldInfo[outerInstance.fieldInfos.Count]));
			}
		}

		public override NumericDocValues getNumericDocValues(string field)
		{
		  return null;
		}

		public override BinaryDocValues getBinaryDocValues(string field)
		{
		  return null;
		}

		public override SortedDocValues getSortedDocValues(string field)
		{
		  return null;
		}

		public override SortedSetDocValues getSortedSetDocValues(string field)
		{
		  return null;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.util.Bits getDocsWithField(String field) throws java.io.IOException
		public override Bits getDocsWithField(string field)
		{
		  return null;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void checkIntegrity() throws java.io.IOException
		public override void checkIntegrity()
		{
		  // no-op
		}

		private class MemoryFields : Fields
		{
			private readonly MemoryIndex.MemoryIndexReader outerInstance;

			public MemoryFields(MemoryIndex.MemoryIndexReader outerInstance)
			{
				this.outerInstance = outerInstance;
			}

		  public override IEnumerator<string> iterator()
		  {
			return new IteratorAnonymousInnerClassHelper(this);
		  }

		  private class IteratorAnonymousInnerClassHelper : IEnumerator<string>
		  {
			  private readonly MemoryFields outerInstance;

			  public IteratorAnonymousInnerClassHelper(MemoryFields outerInstance)
			  {
				  this.outerInstance = outerInstance;
				  upto = -1;
			  }

			  internal int upto;

			  public virtual string next()
			  {
				upto++;
				if (upto >= outerInstance.outerInstance.outerInstance.sortedFields.Length)
				{
				  throw new NoSuchElementException();
				}
				return outerInstance.outerInstance.outerInstance.sortedFields[upto].Key;
			  }

			  public virtual bool hasNext()
			  {
				return upto + 1 < outerInstance.outerInstance.outerInstance.sortedFields.Length;
			  }

			  public virtual void remove()
			  {
				throw new System.NotSupportedException();
			  }
		  }

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.Terms terms(final String field)
		  public override Terms terms(string field)
		  {
			int i = Arrays.binarySearch(outerInstance.outerInstance.sortedFields, field, termComparator);
			if (i < 0)
			{
			  return null;
			}
			else
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Info info = getInfo(i);
			  Info info = outerInstance.getInfo(i);
			  info.sortTerms();

			  return new TermsAnonymousInnerClassHelper(this, info);
			}
		  }

		  private class TermsAnonymousInnerClassHelper : Terms
		  {
			  private readonly MemoryFields outerInstance;

			  private MemoryIndex.Info info;

			  public TermsAnonymousInnerClassHelper(MemoryFields outerInstance, MemoryIndex.Info info)
			  {
				  this.outerInstance = outerInstance;
				  this.info = info;
			  }

			  public override TermsEnum iterator(TermsEnum reuse)
			  {
				return new MemoryTermsEnum(outerInstance.outerInstance, info);
			  }

			  public override IComparer<BytesRef> Comparator
			  {
				  get
				  {
					return BytesRef.UTF8SortedAsUnicodeComparator;
				  }
			  }

			  public override long size()
			  {
				return info.terms.size();
			  }

			  public override long SumTotalTermFreq
			  {
				  get
				  {
					return info.SumTotalTermFreq;
				  }
			  }

			  public override long SumDocFreq
			  {
				  get
				  {
					// each term has df=1
					return info.terms.size();
				  }
			  }

			  public override int DocCount
			  {
				  get
				  {
					return info.terms.size() > 0 ? 1 : 0;
				  }
			  }

			  public override bool hasFreqs()
			  {
				return true;
			  }

			  public override bool hasOffsets()
			  {
				return outerInstance.outerInstance.outerInstance.storeOffsets;
			  }

			  public override bool hasPositions()
			  {
				return true;
			  }

			  public override bool hasPayloads()
			  {
				return false;
			  }
		  }

		  public override int size()
		  {
			return outerInstance.outerInstance.sortedFields.Length;
		  }
		}

		public override Fields fields()
		{
		  outerInstance.sortFields();
		  return new MemoryFields(this);
		}

		private class MemoryTermsEnum : TermsEnum
		{
			private readonly MemoryIndex.MemoryIndexReader outerInstance;

		  internal readonly Info info;
		  internal readonly BytesRef br = new BytesRef();
		  internal int termUpto = -1;

		  public MemoryTermsEnum(MemoryIndex.MemoryIndexReader outerInstance, Info info)
		  {
			  this.outerInstance = outerInstance;
			this.info = info;
			info.sortTerms();
		  }

		  internal int binarySearch(BytesRef b, BytesRef bytesRef, int low, int high, BytesRefHash hash, int[] ords, IComparer<BytesRef> comparator)
		  {
			int mid = 0;
			while (low <= high)
			{
			  mid = (int)((uint)(low + high) >> 1);
			  hash.get(ords[mid], bytesRef);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int cmp = comparator.compare(bytesRef, b);
			  int cmp = comparator.Compare(bytesRef, b);
			  if (cmp < 0)
			  {
				low = mid + 1;
			  }
			  else if (cmp > 0)
			  {
				high = mid - 1;
			  }
			  else
			  {
				return mid;
			  }
			}
			Debug.Assert(comparator.Compare(bytesRef, b) != 0);
			return -(low + 1);
		  }


		  public override bool seekExact(BytesRef text)
		  {
			termUpto = binarySearch(text, br, 0, info.terms.size() - 1, info.terms, info.sortedTerms, BytesRef.UTF8SortedAsUnicodeComparator);
			return termUpto >= 0;
		  }

		  public override SeekStatus seekCeil(BytesRef text)
		  {
			termUpto = binarySearch(text, br, 0, info.terms.size() - 1, info.terms, info.sortedTerms, BytesRef.UTF8SortedAsUnicodeComparator);
			if (termUpto < 0) // not found; choose successor
			{
			  termUpto = -termUpto - 1;
			  if (termUpto >= info.terms.size())
			  {
				return SeekStatus.END;
			  }
			  else
			  {
				info.terms.get(info.sortedTerms[termUpto], br);
				return SeekStatus.NOT_FOUND;
			  }
			}
			else
			{
			  return SeekStatus.FOUND;
			}
		  }

		  public override void SeekExact(long ord)
		  {
			Debug.Assert(ord < info.terms.size());
			termUpto = (int) ord;
		  }

		  public override BytesRef Next()
		  {
			termUpto++;
			if (termUpto >= info.terms.size())
			{
			  return null;
			}
			else
			{
			  info.terms.get(info.sortedTerms[termUpto], br);
			  return br;
			}
		  }

		  public override BytesRef term()
		  {
			return br;
		  }

		  public override long ord()
		  {
			return termUpto;
		  }

		  public override int docFreq()
		  {
			return 1;
		  }

		  public override long totalTermFreq()
		  {
			return info.sliceArray.freq[info.sortedTerms[termUpto]];
		  }

		  public override DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)
		  {
			if (reuse == null || !(reuse is MemoryDocsEnum))
			{
			  reuse = new MemoryDocsEnum(outerInstance);
			}
			return ((MemoryDocsEnum) reuse).reset(liveDocs, info.sliceArray.freq[info.sortedTerms[termUpto]]);
		  }

		  public override DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		  {
			if (reuse == null || !(reuse is MemoryDocsAndPositionsEnum))
			{
			  reuse = new MemoryDocsAndPositionsEnum(outerInstance);
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ord = info.sortedTerms[termUpto];
			int ord = info.sortedTerms[termUpto];
			return ((MemoryDocsAndPositionsEnum) reuse).reset(liveDocs, info.sliceArray.start[ord], info.sliceArray.end[ord], info.sliceArray.freq[ord]);
		  }

		  public override IComparer<BytesRef> Comparator
		  {
			  get
			  {
				return BytesRef.UTF8SortedAsUnicodeComparator;
			  }
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void seekExact(org.apache.lucene.util.BytesRef term, org.apache.lucene.index.TermState state) throws java.io.IOException
		  public override void seekExact(BytesRef term, TermState state)
		  {
			Debug.Assert(state != null);
			this.seekExact(((OrdTermState)state).ord);
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.TermState termState() throws java.io.IOException
		  public override TermState termState()
		  {
			OrdTermState ts = new OrdTermState();
			ts.ord = termUpto;
			return ts;
		  }
		}

		private class MemoryDocsEnum : DocsEnum
		{
			private readonly MemoryIndex.MemoryIndexReader outerInstance;

			public MemoryDocsEnum(MemoryIndex.MemoryIndexReader outerInstance)
			{
				this.outerInstance = outerInstance;
			}

		  internal bool hasNext;
		  internal Bits liveDocs;
		  internal int doc = -1;
		  internal int freq_Renamed;

		  public virtual DocsEnum reset(Bits liveDocs, int freq)
		  {
			this.liveDocs = liveDocs;
			hasNext = true;
			doc = -1;
			this.freq_Renamed = freq;
			return this;
		  }

		  public override int docID()
		  {
			return doc;
		  }

		  public override int nextDoc()
		  {
			if (hasNext && (liveDocs == null || liveDocs.get(0)))
			{
			  hasNext = false;
			  return doc = 0;
			}
			else
			{
			  return doc = NO_MORE_DOCS;
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
		  public override int advance(int target)
		  {
			return slowAdvance(target);
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int freq() throws java.io.IOException
		  public override int freq()
		  {
			return freq_Renamed;
		  }

		  public override long cost()
		  {
			return 1;
		  }
		}

		private class MemoryDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			private readonly MemoryIndex.MemoryIndexReader outerInstance;

		  internal int posUpto; // for assert
		  internal bool hasNext;
		  internal Bits liveDocs;
		  internal int doc = -1;
		  internal SliceReader sliceReader;
		  internal int freq_Renamed;
		  internal int startOffset_Renamed;
		  internal int endOffset_Renamed;

		  public MemoryDocsAndPositionsEnum(MemoryIndex.MemoryIndexReader outerInstance)
		  {
			  this.outerInstance = outerInstance;
			this.sliceReader = new SliceReader(outerInstance.outerInstance.intBlockPool);
		  }

		  public virtual DocsAndPositionsEnum reset(Bits liveDocs, int start, int end, int freq)
		  {
			this.liveDocs = liveDocs;
			this.sliceReader.reset(start, end);
			posUpto = 0; // for assert
			hasNext = true;
			doc = -1;
			this.freq_Renamed = freq;
			return this;
		  }


		  public override int docID()
		  {
			return doc;
		  }

		  public override int nextDoc()
		  {
			if (hasNext && (liveDocs == null || liveDocs.get(0)))
			{
			  hasNext = false;
			  return doc = 0;
			}
			else
			{
			  return doc = NO_MORE_DOCS;
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
		  public override int advance(int target)
		  {
			return slowAdvance(target);
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int freq() throws java.io.IOException
		  public override int freq()
		  {
			return freq_Renamed;
		  }

		  public override int nextPosition()
		  {
			Debug.Assert(posUpto++ < freq_Renamed);
			Debug.Assert(!sliceReader.endOfSlice(), " stores offsets : " + startOffset_Renamed);
			if (outerInstance.outerInstance.storeOffsets)
			{
			  int pos = sliceReader.readInt();
			  startOffset_Renamed = sliceReader.readInt();
			  endOffset_Renamed = sliceReader.readInt();
			  return pos;
			}
			else
			{
			  return sliceReader.readInt();
			}
		  }

		  public override int startOffset()
		  {
			return startOffset_Renamed;
		  }

		  public override int endOffset()
		  {
			return endOffset_Renamed;
		  }

		  public override BytesRef Payload
		  {
			  get
			  {
				return null;
			  }
		  }

		  public override long cost()
		  {
			return 1;
		  }
		}

		public override Fields getTermVectors(int docID)
		{
		  if (docID == 0)
		  {
			return fields();
		  }
		  else
		  {
			return null;
		  }
		}

		internal Similarity Similarity
		{
			get
			{
			  if (searcher != null)
			  {
				  return searcher.Similarity;
			  }
			  return IndexSearcher.DefaultSimilarity;
			}
		}

		internal IndexSearcher Searcher
		{
			set
			{
			  this.searcher = value;
			}
		}

		public override int numDocs()
		{
		  if (DEBUG)
		  {
			  Console.Error.WriteLine("MemoryIndexReader.numDocs");
		  }
		  return 1;
		}

		public override int maxDoc()
		{
		  if (DEBUG)
		  {
			  Console.Error.WriteLine("MemoryIndexReader.maxDoc");
		  }
		  return 1;
		}

		public override void document(int docID, StoredFieldVisitor visitor)
		{
		  if (DEBUG)
		  {
			  Console.Error.WriteLine("MemoryIndexReader.document");
		  }
		  // no-op: there are no stored fields
		}

		protected internal override void doClose()
		{
		  if (DEBUG)
		  {
			  Console.Error.WriteLine("MemoryIndexReader.doClose");
		  }
		}

		/// <summary>
		/// performance hack: cache norms to avoid repeated expensive calculations </summary>
		internal NumericDocValues cachedNormValues;
		internal string cachedFieldName;
		internal Similarity cachedSimilarity;

		public override NumericDocValues getNormValues(string field)
		{
		  FieldInfo fieldInfo = outerInstance.fieldInfos[field];
		  if (fieldInfo == null || fieldInfo.omitsNorms())
		  {
			return null;
		  }
		  NumericDocValues norms = cachedNormValues;
		  Similarity sim = Similarity;
		  if (!field.Equals(cachedFieldName) || sim != cachedSimilarity) // not cached?
		  {
			Info info = getInfo(field);
			int numTokens = info != null ? info.numTokens : 0;
			int numOverlapTokens = info != null ? info.numOverlapTokens : 0;
			float boost = info != null ? info.Boost : 1.0f;
			FieldInvertState invertState = new FieldInvertState(field, 0, numTokens, numOverlapTokens, 0, boost);
			long value = sim.computeNorm(invertState);
			norms = new MemoryIndexNormDocValues(value);
			// cache it for future reuse
			cachedNormValues = norms;
			cachedFieldName = field;
			cachedSimilarity = sim;
			if (DEBUG)
			{
				Console.Error.WriteLine("MemoryIndexReader.norms: " + field + ":" + value + ":" + numTokens);
			}
		  }
		  return norms;
		}
	  }

	  /// <summary>
	  /// Resets the <seealso cref="MemoryIndex"/> to its initial state and recycles all internal buffers.
	  /// </summary>
	  public virtual void reset()
	  {
		this.fieldInfos.Clear();
		this.fields.Clear();
		this.sortedFields = null;
		byteBlockPool.reset(false, false); // no need to 0-fill the buffers
		intBlockPool.reset(true, false); // here must must 0-fill since we use slices
	  }

	  private sealed class SliceByteStartArray : BytesRefHash.DirectBytesStartArray
	  {
		internal int[] start; // the start offset in the IntBlockPool per term
		internal int[] end; // the end pointer in the IntBlockPool for the postings slice per term
		internal int[] freq; // the term frequency

		public SliceByteStartArray(int initSize) : base(initSize)
		{
		}

		public override int[] init()
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] ord = base.init();
		  int[] ord = base.init();
		  start = new int[ArrayUtil.oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
		  end = new int[ArrayUtil.oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
		  freq = new int[ArrayUtil.oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
		  Debug.Assert(start.Length >= ord.Length);
		  Debug.Assert(end.Length >= ord.Length);
		  Debug.Assert(freq.Length >= ord.Length);
		  return ord;
		}

		public override int[] grow()
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] ord = base.grow();
		  int[] ord = base.grow();
		  if (start.Length < ord.Length)
		  {
			start = ArrayUtil.grow(start, ord.Length);
			end = ArrayUtil.grow(end, ord.Length);
			freq = ArrayUtil.grow(freq, ord.Length);
		  }
		  Debug.Assert(start.Length >= ord.Length);
		  Debug.Assert(end.Length >= ord.Length);
		  Debug.Assert(freq.Length >= ord.Length);
		  return ord;
		}

		public override int[] clear()
		{
		 start = end = null;
		 return base.clear();
		}

	  }
	}

}