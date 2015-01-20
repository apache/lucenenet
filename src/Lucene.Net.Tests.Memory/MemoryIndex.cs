/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Index.Memory;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Similarities;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Index.Memory
{
	/// <summary>High-performance single-document main memory Apache Lucene fulltext search index.
	/// 	</summary>
	/// <remarks>
	/// High-performance single-document main memory Apache Lucene fulltext search index.
	/// <h4>Overview</h4>
	/// This class is a replacement/substitute for a large subset of
	/// <see cref="Org.Apache.Lucene.Store.RAMDirectory">Org.Apache.Lucene.Store.RAMDirectory
	/// 	</see>
	/// functionality. It is designed to
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
	/// <p>
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
	/// <p>
	/// Arbitrary Lucene queries can be run against this class - see &lt;a target="_blank"
	/// href="
	/// <docRoot></docRoot>
	/// /../queryparser/org/apache/lucene/queryparser/classic/package-summary.html#package_description"&gt;
	/// Lucene Query Syntax</a>
	/// as well as &lt;a target="_blank"
	/// href="http://today.java.net/pub/a/today/2003/11/07/QueryParserRules.html"&gt;Query Parser Rules</a>.
	/// Note that a Lucene query selects on the field names and associated (indexed)
	/// tokenized terms, not on the original fulltext(s) - the latter are not stored
	/// but rather thrown away immediately after tokenization.
	/// <p>
	/// For some interesting background information on search technology, see Bob Wyman's
	/// &lt;a target="_blank"
	/// href="http://bobwyman.pubsub.com/main/2005/05/mary_hodder_poi.html"&gt;Prospective Search</a>,
	/// Jim Gray's
	/// <a target="_blank" href="http://www.acmqueue.org/modules.php?name=Content&pa=showpage&pid=293&page=4">
	/// A Call to Arms - Custom subscriptions</a>, and Tim Bray's
	/// &lt;a target="_blank"
	/// href="http://www.tbray.org/ongoing/When/200x/2003/07/30/OnSearchTOC"&gt;On Search, the Series</a>.
	/// <h4>Example Usage</h4>
	/// <pre class="prettyprint">
	/// Analyzer analyzer = new SimpleAnalyzer(version);
	/// MemoryIndex index = new MemoryIndex();
	/// index.addField("content", "Readings about Salmons and other select Alaska fishing Manuals", analyzer);
	/// index.addField("author", "Tales of James", analyzer);
	/// QueryParser parser = new QueryParser(version, "content", analyzer);
	/// float score = index.search(parser.parse("+author:james +salmon~ +fish* manual~"));
	/// if (score &gt; 0.0f) {
	/// System.out.println("it's a match");
	/// } else {
	/// System.out.println("no match found");
	/// }
	/// System.out.println("indexData=" + index.toString());
	/// </pre>
	/// <h4>Example XQuery Usage</h4>
	/// <pre class="prettyprint">
	/// (: An XQuery that finds all books authored by James that have something to do with "salmon fishing manuals", sorted by relevance :)
	/// declare namespace lucene = "java:nux.xom.pool.FullTextUtil";
	/// declare variable $query := "+salmon~ +fish* manual~"; (: any arbitrary Lucene query can go here :)
	/// for $book in /books/book[author="James" and lucene:match(abstract, $query) &gt; 0.0]
	/// let $score := lucene:match($book/abstract, $query)
	/// order by $score descending
	/// return $book
	/// </pre>
	/// <h4>No thread safety guarantees</h4>
	/// An instance can be queried multiple times with the same or different queries,
	/// but an instance is not thread-safe. If desired use idioms such as:
	/// <pre class="prettyprint">
	/// MemoryIndex index = ...
	/// synchronized (index) {
	/// // read and/or write index (i.e. add fields and/or query)
	/// }
	/// </pre>
	/// <h4>Performance Notes</h4>
	/// Internally there's a new data structure geared towards efficient indexing
	/// and searching, plus the necessary support code to seamlessly plug into the Lucene
	/// framework.
	/// <p>
	/// This class performs very well for very small texts (e.g. 10 chars)
	/// as well as for large texts (e.g. 10 MB) and everything in between.
	/// Typically, it is about 10-100 times faster than <code>RAMDirectory</code>.
	/// Note that <code>RAMDirectory</code> has particularly
	/// large efficiency overheads for small to medium sized texts, both in time and space.
	/// Indexing a field with N tokens takes O(N) in the best case, and O(N logN) in the worst
	/// case. Memory consumption is probably larger than for <code>RAMDirectory</code>.
	/// <p>
	/// Example throughput of many simple term queries over a single MemoryIndex:
	/// ~500000 queries/sec on a MacBook Pro, jdk 1.5.0_06, server VM.
	/// As always, your mileage may vary.
	/// <p>
	/// If you're curious about
	/// the whereabouts of bottlenecks, run java 1.5 with the non-perturbing '-server
	/// -agentlib:hprof=cpu=samples,depth=10' flags, then study the trace log and
	/// correlate its hotspot trailer with its call stack headers (see &lt;a
	/// target="_blank"
	/// href="http://java.sun.com/developer/technicalArticles/Programming/HPROF.html"&gt;
	/// hprof tracing </a>).
	/// </remarks>
	public class MemoryIndex
	{
		/// <summary>info for each field: Map<String fieldName, Info field></summary>
		private readonly Dictionary<string, MemoryIndex.Info> fields = new Dictionary<string
			, MemoryIndex.Info>();

		/// <summary>fields sorted ascending by fieldName; lazily computed on demand</summary>
		[System.NonSerialized]
		private KeyValuePair<string, MemoryIndex.Info>[] sortedFields;

		private readonly bool storeOffsets;

		private const bool DEBUG = false;

		private readonly ByteBlockPool byteBlockPool;

		private readonly IntBlockPool intBlockPool;

		private readonly IntBlockPool.SliceWriter postingsWriter;

		private Dictionary<string, FieldInfo> fieldInfos = new Dictionary<string, FieldInfo
			>();

		private Counter bytesUsed;

		private sealed class _IComparer_220 : IComparer<object>
		{
			public _IComparer_220()
			{
			}

			// for javadocs
			//  private final IntBlockPool.SliceReader postingsReader;
			public int Compare(object o1, object o2)
			{
				if (o1 is KeyValuePair<object, object>)
				{
					o1 = ((KeyValuePair<object, object>)o1).Key;
				}
				if (o2 is KeyValuePair<object, object>)
				{
					o2 = ((KeyValuePair<object, object>)o2).Key;
				}
				if (o1 == o2)
				{
					return 0;
				}
				return ((IComparable)o1).CompareTo((IComparable)o2);
			}
		}

		/// <summary>
		/// Sorts term entries into ascending order; also works for
		/// Arrays.binarySearch() and Arrays.sort()
		/// </summary>
		private static readonly IComparer<object> termComparator = new _IComparer_220();

		/// <summary>Constructs an empty instance.</summary>
		/// <remarks>Constructs an empty instance.</remarks>
		public MemoryIndex() : this(false)
		{
		}

		/// <summary>
		/// Constructs an empty instance that can optionally store the start and end
		/// character offset of each token term in the text.
		/// </summary>
		/// <remarks>
		/// Constructs an empty instance that can optionally store the start and end
		/// character offset of each token term in the text. This can be useful for
		/// highlighting of hit locations with the Lucene highlighter package.
		/// Protected until the highlighter package matures, so that this can actually
		/// be meaningfully integrated.
		/// </remarks>
		/// <param name="storeOffsets">
		/// whether or not to store the start and end character offset of
		/// each token term in the text
		/// </param>
		public MemoryIndex(bool storeOffsets) : this(storeOffsets, 0)
		{
		}

		/// <summary>
		/// Expert: This constructor accepts an upper limit for the number of bytes that should be reused if this instance is
		/// <see cref="Reset()">Reset()</see>
		/// .
		/// </summary>
		/// <param name="storeOffsets"><code>true</code> if offsets should be stored</param>
		/// <param name="maxReusedBytes">
		/// the number of bytes that should remain in the internal memory pools after
		/// <see cref="Reset()">Reset()</see>
		/// is called
		/// </param>
		internal MemoryIndex(bool storeOffsets, long maxReusedBytes)
		{
			this.storeOffsets = storeOffsets;
			this.bytesUsed = Counter.NewCounter();
			int maxBufferedByteBlocks = (int)((maxReusedBytes / 2) / ByteBlockPool.BYTE_BLOCK_SIZE
				);
			int maxBufferedIntBlocks = (int)((maxReusedBytes - (maxBufferedByteBlocks * ByteBlockPool
				.BYTE_BLOCK_SIZE)) / (IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT
				));
			(maxBufferedByteBlocks * ByteBlockPool.BYTE_BLOCK_SIZE) + (maxBufferedIntBlocks *
				 IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT) <= maxReusedBytes
				 = new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE
				, maxBufferedByteBlocks, bytesUsed));
			intBlockPool = new IntBlockPool(new RecyclingIntBlockAllocator(IntBlockPool.INT_BLOCK_SIZE
				, maxBufferedIntBlocks, bytesUsed));
			postingsWriter = new IntBlockPool.SliceWriter(intBlockPool);
		}

		/// <summary>
		/// Convenience method; Tokenizes the given field text and adds the resulting
		/// terms to the index; Equivalent to adding an indexed non-keyword Lucene
		/// <see cref="Org.Apache.Lucene.Document.Field">Org.Apache.Lucene.Document.Field</see>
		/// that is tokenized, not stored,
		/// termVectorStored with positions (or termVectorStored with positions and offsets),
		/// </summary>
		/// <param name="fieldName">a name to be associated with the text</param>
		/// <param name="text">the text to tokenize and index.</param>
		/// <param name="analyzer">the analyzer to use for tokenization</param>
		public virtual void AddField(string fieldName, string text, Analyzer analyzer)
		{
			if (fieldName == null)
			{
				throw new ArgumentException("fieldName must not be null");
			}
			if (text == null)
			{
				throw new ArgumentException("text must not be null");
			}
			if (analyzer == null)
			{
				throw new ArgumentException("analyzer must not be null");
			}
			TokenStream stream;
			try
			{
				stream = analyzer.TokenStream(fieldName, text);
			}
			catch (IOException ex)
			{
				throw new RuntimeException(ex);
			}
			AddField(fieldName, stream, 1.0f, analyzer.GetPositionIncrementGap(fieldName), analyzer
				.GetOffsetGap(fieldName));
		}

		/// <summary>
		/// Convenience method; Creates and returns a token stream that generates a
		/// token for each keyword in the given collection, "as is", without any
		/// transforming text analysis.
		/// </summary>
		/// <remarks>
		/// Convenience method; Creates and returns a token stream that generates a
		/// token for each keyword in the given collection, "as is", without any
		/// transforming text analysis. The resulting token stream can be fed into
		/// <see cref="AddField(string, Org.Apache.Lucene.Analysis.TokenStream)">AddField(string, Org.Apache.Lucene.Analysis.TokenStream)
		/// 	</see>
		/// , perhaps wrapped into another
		/// <see cref="Org.Apache.Lucene.Analysis.TokenFilter">Org.Apache.Lucene.Analysis.TokenFilter
		/// 	</see>
		/// , as desired.
		/// </remarks>
		/// <param name="keywords">the keywords to generate tokens for</param>
		/// <returns>the corresponding token stream</returns>
		public virtual TokenStream KeywordTokenStream<T>(ICollection<T> keywords)
		{
			// TODO: deprecate & move this method into AnalyzerUtil?
			if (keywords == null)
			{
				throw new ArgumentException("keywords must not be null");
			}
			return new _TokenStream_317(keywords);
		}

		private sealed class _TokenStream_317 : TokenStream
		{
			public _TokenStream_317(ICollection<T> keywords)
			{
				this.keywords = keywords;
				this.iter = keywords.Iterator();
				this.start = 0;
				this.termAtt = this.AddAttribute<CharTermAttribute>();
				this.offsetAtt = this.AddAttribute<OffsetAttribute>();
			}

			private Iterator<T> iter;

			private int start;

			private readonly CharTermAttribute termAtt;

			private readonly OffsetAttribute offsetAtt;

			public override bool IncrementToken()
			{
				if (!this.iter.HasNext())
				{
					return false;
				}
				T obj = this.iter.Next();
				if (obj == null)
				{
					throw new ArgumentException("keyword must not be null");
				}
				string term = obj.ToString();
				this.ClearAttributes();
				this.termAtt.SetEmpty().Append(term);
				this.offsetAtt.SetOffset(this.start, this.start + this.termAtt.Length);
				this.start += term.Length + 1;
				// separate words by 1 (blank) character
				return true;
			}

			private readonly ICollection<T> keywords;
		}

		/// <summary>Equivalent to <code>addField(fieldName, stream, 1.0f)</code>.</summary>
		/// <remarks>Equivalent to <code>addField(fieldName, stream, 1.0f)</code>.</remarks>
		/// <param name="fieldName">a name to be associated with the text</param>
		/// <param name="stream">the token stream to retrieve tokens from</param>
		public virtual void AddField(string fieldName, TokenStream stream)
		{
			AddField(fieldName, stream, 1.0f);
		}

		/// <summary>
		/// Iterates over the given token stream and adds the resulting terms to the index;
		/// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
		/// Lucene
		/// <see cref="Org.Apache.Lucene.Document.Field">Org.Apache.Lucene.Document.Field</see>
		/// .
		/// Finally closes the token stream. Note that untokenized keywords can be added with this method via
		/// <see cref="KeywordTokenStream{T}(System.Collections.Generic.ICollection{E})">KeywordTokenStream&lt;T&gt;(System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// , the Lucene <code>KeywordTokenizer</code> or similar utilities.
		/// </summary>
		/// <param name="fieldName">a name to be associated with the text</param>
		/// <param name="stream">the token stream to retrieve tokens from.</param>
		/// <param name="boost">the boost factor for hits for this field</param>
		/// <seealso cref="Org.Apache.Lucene.Document.Field.SetBoost(float)">Org.Apache.Lucene.Document.Field.SetBoost(float)
		/// 	</seealso>
		public virtual void AddField(string fieldName, TokenStream stream, float boost)
		{
			AddField(fieldName, stream, boost, 0);
		}

		/// <summary>
		/// Iterates over the given token stream and adds the resulting terms to the index;
		/// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
		/// Lucene
		/// <see cref="Org.Apache.Lucene.Document.Field">Org.Apache.Lucene.Document.Field</see>
		/// .
		/// Finally closes the token stream. Note that untokenized keywords can be added with this method via
		/// <see cref="KeywordTokenStream{T}(System.Collections.Generic.ICollection{E})">KeywordTokenStream&lt;T&gt;(System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// , the Lucene <code>KeywordTokenizer</code> or similar utilities.
		/// </summary>
		/// <param name="fieldName">a name to be associated with the text</param>
		/// <param name="stream">the token stream to retrieve tokens from.</param>
		/// <param name="boost">the boost factor for hits for this field</param>
		/// <param name="positionIncrementGap">the position increment gap if fields with the same name are added more than once
		/// 	</param>
		/// <seealso cref="Org.Apache.Lucene.Document.Field.SetBoost(float)">Org.Apache.Lucene.Document.Field.SetBoost(float)
		/// 	</seealso>
		public virtual void AddField(string fieldName, TokenStream stream, float boost, int
			 positionIncrementGap)
		{
			AddField(fieldName, stream, boost, positionIncrementGap, 1);
		}

		/// <summary>
		/// Iterates over the given token stream and adds the resulting terms to the index;
		/// Equivalent to adding a tokenized, indexed, termVectorStored, unstored,
		/// Lucene
		/// <see cref="Org.Apache.Lucene.Document.Field">Org.Apache.Lucene.Document.Field</see>
		/// .
		/// Finally closes the token stream. Note that untokenized keywords can be added with this method via
		/// <see cref="KeywordTokenStream{T}(System.Collections.Generic.ICollection{E})">KeywordTokenStream&lt;T&gt;(System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// , the Lucene <code>KeywordTokenizer</code> or similar utilities.
		/// </summary>
		/// <param name="fieldName">a name to be associated with the text</param>
		/// <param name="stream">the token stream to retrieve tokens from.</param>
		/// <param name="boost">the boost factor for hits for this field</param>
		/// <param name="positionIncrementGap">the position increment gap if fields with the same name are added more than once
		/// 	</param>
		/// <param name="offsetGap">the offset gap if fields with the same name are added more than once
		/// 	</param>
		/// <seealso cref="Org.Apache.Lucene.Document.Field.SetBoost(float)">Org.Apache.Lucene.Document.Field.SetBoost(float)
		/// 	</seealso>
		public virtual void AddField(string fieldName, TokenStream stream, float boost, int
			 positionIncrementGap, int offsetGap)
		{
			try
			{
				if (fieldName == null)
				{
					throw new ArgumentException("fieldName must not be null");
				}
				if (stream == null)
				{
					throw new ArgumentException("token stream must not be null");
				}
				if (boost <= 0.0f)
				{
					throw new ArgumentException("boost factor must be greater than 0.0");
				}
				int numTokens = 0;
				int numOverlapTokens = 0;
				int pos = -1;
				BytesRefHash terms;
				MemoryIndex.SliceByteStartArray sliceArray;
				MemoryIndex.Info info = null;
				long sumTotalTermFreq = 0;
				int offset = 0;
				if ((info = fields.Get(fieldName)) != null)
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
					sliceArray = new MemoryIndex.SliceByteStartArray(BytesRefHash.DEFAULT_CAPACITY);
					terms = new BytesRefHash(byteBlockPool, BytesRefHash.DEFAULT_CAPACITY, sliceArray
						);
				}
				if (!fieldInfos.ContainsKey(fieldName))
				{
					fieldInfos.Put(fieldName, new FieldInfo(fieldName, true, fieldInfos.Count, false, 
						false, false, this.storeOffsets ? FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
						 : FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, null, null, null));
				}
				TermToBytesRefAttribute termAtt = stream.GetAttribute<TermToBytesRefAttribute>();
				PositionIncrementAttribute posIncrAttribute = stream.AddAttribute<PositionIncrementAttribute
					>();
				OffsetAttribute offsetAtt = stream.AddAttribute<OffsetAttribute>();
				BytesRef @ref = termAtt.GetBytesRef();
				stream.Reset();
				while (stream.IncrementToken())
				{
					termAtt.FillBytesRef();
					//        if (DEBUG) System.err.println("token='" + term + "'");
					numTokens++;
					int posIncr = posIncrAttribute.GetPositionIncrement();
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
						postingsWriter.WriteInt(pos);
					}
					else
					{
						postingsWriter.WriteInt(pos);
						postingsWriter.WriteInt(offsetAtt.StartOffset() + offset);
						postingsWriter.WriteInt(offsetAtt.EndOffset() + offset);
					}
					sliceArray.end[ord] = postingsWriter.GetCurrentOffset();
				}
				stream.End();
				// ensure infos.numTokens > 0 invariant; needed for correct operation of terms()
				if (numTokens > 0)
				{
					fields.Put(fieldName, new MemoryIndex.Info(terms, sliceArray, numTokens, numOverlapTokens
						, boost, pos, offsetAtt.EndOffset() + offset, sumTotalTermFreq));
					sortedFields = null;
				}
			}
			catch (Exception e)
			{
				// invalidate sorted view, if any
				// can never happen
				throw new RuntimeException(e);
			}
			finally
			{
				try
				{
					if (stream != null)
					{
						stream.Close();
					}
				}
				catch (IOException e2)
				{
					throw new RuntimeException(e2);
				}
			}
		}

		/// <summary>
		/// Creates and returns a searcher that can be used to execute arbitrary
		/// Lucene queries and to collect the resulting query results as hits.
		/// </summary>
		/// <remarks>
		/// Creates and returns a searcher that can be used to execute arbitrary
		/// Lucene queries and to collect the resulting query results as hits.
		/// </remarks>
		/// <returns>a searcher</returns>
		public virtual IndexSearcher CreateSearcher()
		{
			MemoryIndex.MemoryIndexReader reader = new MemoryIndex.MemoryIndexReader(this);
			IndexSearcher searcher = new IndexSearcher(reader);
			// ensures no auto-close !!
			reader.SetSearcher(searcher);
			// to later get hold of searcher.getSimilarity()
			return searcher;
		}

		/// <summary>
		/// Convenience method that efficiently returns the relevance score by
		/// matching this index against the given Lucene query expression.
		/// </summary>
		/// <remarks>
		/// Convenience method that efficiently returns the relevance score by
		/// matching this index against the given Lucene query expression.
		/// </remarks>
		/// <param name="query">an arbitrary Lucene query to run against this index</param>
		/// <returns>
		/// the relevance score of the matchmaking; A number in the range
		/// [0.0 .. 1.0], with 0.0 indicating no match. The higher the number
		/// the better the match.
		/// </returns>
		public virtual float Search(Query query)
		{
			if (query == null)
			{
				throw new ArgumentException("query must not be null");
			}
			IndexSearcher searcher = CreateSearcher();
			try
			{
				float[] scores = new float[1];
				// inits to 0.0f (no match)
				searcher.Search(query, new _Collector_535(scores));
				float score = scores[0];
				return score;
			}
			catch (IOException e)
			{
				// can never happen (RAMDirectory)
				throw new RuntimeException(e);
			}
			finally
			{
			}
		}

		private sealed class _Collector_535 : Collector
		{
			public _Collector_535(float[] scores)
			{
				this.scores = scores;
			}

			private Scorer scorer;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				scores[0] = this.scorer.Score();
			}

			public override void SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return true;
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
			}

			private readonly float[] scores;
		}

		// searcher.close();
		/// <summary>
		/// Returns a reasonable approximation of the main memory [bytes] consumed by
		/// this instance.
		/// </summary>
		/// <remarks>
		/// Returns a reasonable approximation of the main memory [bytes] consumed by
		/// this instance. Useful for smart memory sensititive caches/pools.
		/// </remarks>
		/// <returns>the main memory consumption</returns>
		public virtual long GetMemorySize()
		{
			return RamUsageEstimator.SizeOf(this);
		}

		/// <summary>sorts into ascending order (on demand), reusing memory along the way</summary>
		private void SortFields()
		{
			if (sortedFields == null)
			{
				sortedFields = Sort(fields);
			}
		}

		/// <summary>returns a view of the given map's entries, sorted ascending by key</summary>
		private static KeyValuePair<K, V>[] Sort<K, V>(Dictionary<K, V> map)
		{
			int size = map.Count;
			KeyValuePair<K, V>[] entries = new DictionaryEntry[size];
			Iterator<KeyValuePair<K, V>> iter = map.EntrySet().Iterator();
			for (int i = 0; i < size; i++)
			{
				entries[i] = iter.Next();
			}
			if (size > 1)
			{
				ArrayUtil.IntroSort(entries, termComparator);
			}
			return entries;
		}

		/// <summary>Returns a String representation of the index data for debugging purposes.
		/// 	</summary>
		/// <remarks>Returns a String representation of the index data for debugging purposes.
		/// 	</remarks>
		/// <returns>the string representation</returns>
		public override string ToString()
		{
			StringBuilder result = new StringBuilder(256);
			SortFields();
			int sumPositions = 0;
			int sumTerms = 0;
			BytesRef spare = new BytesRef();
			for (int i = 0; i < sortedFields.Length; i++)
			{
				KeyValuePair<string, MemoryIndex.Info> entry = sortedFields[i];
				string fieldName = entry.Key;
				MemoryIndex.Info info = entry.Value;
				info.SortTerms();
				result.Append(fieldName + ":\n");
				MemoryIndex.SliceByteStartArray sliceArray = info.sliceArray;
				int numPositions = 0;
				IntBlockPool.SliceReader postingsReader = new IntBlockPool.SliceReader(intBlockPool
					);
				for (int j = 0; j < info.terms.Size(); j++)
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
				result.Append("\tterms=" + info.terms.Size());
				result.Append(", positions=" + numPositions);
				result.Append(", memory=" + RamUsageEstimator.HumanReadableUnits(RamUsageEstimator
					.SizeOf(info)));
				result.Append("\n");
				sumPositions += numPositions;
				sumTerms += info.terms.Size();
			}
			result.Append("\nfields=" + sortedFields.Length);
			result.Append(", terms=" + sumTerms);
			result.Append(", positions=" + sumPositions);
			result.Append(", memory=" + RamUsageEstimator.HumanReadableUnits(GetMemorySize())
				);
			return result.ToString();
		}

		/// <summary>
		/// Index data structure for a field; Contains the tokenized term texts and
		/// their positions.
		/// </summary>
		/// <remarks>
		/// Index data structure for a field; Contains the tokenized term texts and
		/// their positions.
		/// </remarks>
		private sealed class Info
		{
			/// <summary>
			/// Term strings and their positions for this field: Map &lt;String
			/// termText, ArrayIntList positions&gt;
			/// </summary>
			private readonly BytesRefHash terms;

			private readonly MemoryIndex.SliceByteStartArray sliceArray;

			/// <summary>Terms sorted ascending by term text; computed on demand</summary>
			[System.NonSerialized]
			private int[] sortedTerms;

			/// <summary>Number of added tokens for this field</summary>
			private readonly int numTokens;

			/// <summary>Number of overlapping tokens for this field</summary>
			private readonly int numOverlapTokens;

			/// <summary>Boost factor for hits for this field</summary>
			private readonly float boost;

			private readonly long sumTotalTermFreq;

			/// <summary>the last position encountered in this field for multi field support</summary>
			private int lastPosition;

			/// <summary>the last offset encountered in this field for multi field support</summary>
			private int lastOffset;

			public Info(BytesRefHash terms, MemoryIndex.SliceByteStartArray sliceArray, int numTokens
				, int numOverlapTokens, float boost, int lastPosition, int lastOffset, long sumTotalTermFreq
				)
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

			public long GetSumTotalTermFreq()
			{
				return sumTotalTermFreq;
			}

			/// <summary>
			/// Sorts hashed terms into ascending order, reusing memory along the
			/// way.
			/// </summary>
			/// <remarks>
			/// Sorts hashed terms into ascending order, reusing memory along the
			/// way. Note that sorting is lazily delayed until required (often it's
			/// not required at all). If a sorted view is required then hashing +
			/// sort + binary search is still faster and smaller than TreeMap usage
			/// (which would be an alternative and somewhat more elegant approach,
			/// apart from more sophisticated Tries / prefix trees).
			/// </remarks>
			public void SortTerms()
			{
				if (sortedTerms == null)
				{
					sortedTerms = terms.Sort(BytesRef.GetUTF8SortedAsUnicodeComparator());
				}
			}

			public float GetBoost()
			{
				return boost;
			}
		}

		/// <summary>
		/// Search support for Lucene framework integration; implements all methods
		/// required by the Lucene IndexReader contracts.
		/// </summary>
		/// <remarks>
		/// Search support for Lucene framework integration; implements all methods
		/// required by the Lucene IndexReader contracts.
		/// </remarks>
		private sealed class MemoryIndexReader : AtomicReader
		{
			private IndexSearcher searcher;

			public MemoryIndexReader(MemoryIndex _enclosing) : base()
			{
				this._enclosing = _enclosing;
			}

			///////////////////////////////////////////////////////////////////////////////
			// Nested classes:
			///////////////////////////////////////////////////////////////////////////////
			// needed to find searcher.getSimilarity() 
			// avoid as much superclass baggage as possible
			private MemoryIndex.Info GetInfo(string fieldName)
			{
				return this._enclosing.fields.Get(fieldName);
			}

			private MemoryIndex.Info GetInfo(int pos)
			{
				return this._enclosing.sortedFields[pos].Value;
			}

			public override Bits GetLiveDocs()
			{
				return null;
			}

			public override FieldInfos GetFieldInfos()
			{
				return new FieldInfos(Sharpen.Collections.ToArray(this._enclosing.fieldInfos.Values
					, new FieldInfo[this._enclosing.fieldInfos.Count]));
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

			/// <exception cref="System.IO.IOException"></exception>
			public override Bits GetDocsWithField(string field)
			{
				return null;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CheckIntegrity()
			{
			}

			private class MemoryFields : Fields
			{
				// no-op
				public override Sharpen.Iterator<string> Iterator()
				{
					return new _Iterator_805(this);
				}

				private sealed class _Iterator_805 : Sharpen.Iterator<string>
				{
					public _Iterator_805(MemoryFields _enclosing)
					{
						this._enclosing = _enclosing;
						this.upto = -1;
					}

					internal int upto;

					public override string Next()
					{
						this.upto++;
						if (this.upto >= this._enclosing._enclosing._enclosing.sortedFields.Length)
						{
							throw new NoSuchElementException();
						}
						return this._enclosing._enclosing._enclosing.sortedFields[this.upto].Key;
					}

					public override bool HasNext()
					{
						return this.upto + 1 < this._enclosing._enclosing._enclosing.sortedFields.Length;
					}

					public override void Remove()
					{
						throw new NotSupportedException();
					}

					private readonly MemoryFields _enclosing;
				}

				public override Org.Apache.Lucene.Index.Terms Terms(string field)
				{
					int i = System.Array.BinarySearch(this._enclosing._enclosing.sortedFields, field, 
						MemoryIndex.termComparator);
					if (i < 0)
					{
						return null;
					}
					else
					{
						MemoryIndex.Info info = this._enclosing.GetInfo(i);
						info.SortTerms();
						return new _Terms_838(this, info);
					}
				}

				private sealed class _Terms_838 : Org.Apache.Lucene.Index.Terms
				{
					public _Terms_838(MemoryFields _enclosing, MemoryIndex.Info info)
					{
						this._enclosing = _enclosing;
						this.info = info;
					}

					public override TermsEnum Iterator(TermsEnum reuse)
					{
						return new MemoryIndex.MemoryIndexReader.MemoryTermsEnum(this, info);
					}

					public override IComparer<BytesRef> GetComparator()
					{
						return BytesRef.GetUTF8SortedAsUnicodeComparator();
					}

					public override long Size()
					{
						return info.terms.Size();
					}

					public override long GetSumTotalTermFreq()
					{
						return info.GetSumTotalTermFreq();
					}

					public override long GetSumDocFreq()
					{
						// each term has df=1
						return info.terms.Size();
					}

					public override int GetDocCount()
					{
						return info.terms.Size() > 0 ? 1 : 0;
					}

					public override bool HasFreqs()
					{
						return true;
					}

					public override bool HasOffsets()
					{
						return this._enclosing._enclosing._enclosing.storeOffsets;
					}

					public override bool HasPositions()
					{
						return true;
					}

					public override bool HasPayloads()
					{
						return false;
					}

					private readonly MemoryFields _enclosing;

					private readonly MemoryIndex.Info info;
				}

				public override int Size()
				{
					return this._enclosing._enclosing.sortedFields.Length;
				}

				internal MemoryFields(MemoryIndexReader _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly MemoryIndexReader _enclosing;
			}

			public override Fields Fields()
			{
				this._enclosing.SortFields();
				return new MemoryIndex.MemoryIndexReader.MemoryFields(this);
			}

			private class MemoryTermsEnum : TermsEnum
			{
				private readonly MemoryIndex.Info info;

				private readonly BytesRef br = new BytesRef();

				internal int termUpto = -1;

				public MemoryTermsEnum(MemoryIndexReader _enclosing, MemoryIndex.Info info)
				{
					this._enclosing = _enclosing;
					this.info = info;
					info.SortTerms();
				}

				private int BinarySearch(BytesRef b, BytesRef bytesRef, int low, int high, BytesRefHash
					 hash, int[] ords, IComparer<BytesRef> comparator)
				{
					int mid = 0;
					while (low <= high)
					{
						mid = (int)(((uint)(low + high)) >> 1);
						hash.Get(ords[mid], bytesRef);
						int cmp = comparator.Compare(bytesRef, b);
						if (cmp < 0)
						{
							low = mid + 1;
						}
						else
						{
							if (cmp > 0)
							{
								high = mid - 1;
							}
							else
							{
								return mid;
							}
						}
					}
					return -(comparator.Compare(bytesRef, b) != 0 + 1);
				}

				public override bool SeekExact(BytesRef text)
				{
					this.termUpto = this.BinarySearch(text, this.br, 0, this.info.terms.Size() - 1, this
						.info.terms, this.info.sortedTerms, BytesRef.GetUTF8SortedAsUnicodeComparator());
					return this.termUpto >= 0;
				}

				public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
				{
					this.termUpto = this.BinarySearch(text, this.br, 0, this.info.terms.Size() - 1, this
						.info.terms, this.info.sortedTerms, BytesRef.GetUTF8SortedAsUnicodeComparator());
					if (this.termUpto < 0)
					{
						// not found; choose successor
						this.termUpto = -this.termUpto - 1;
						if (this.termUpto >= this.info.terms.Size())
						{
							return TermsEnum.SeekStatus.END;
						}
						else
						{
							this.info.terms.Get(this.info.sortedTerms[this.termUpto], this.br);
							return TermsEnum.SeekStatus.NOT_FOUND;
						}
					}
					else
					{
						return TermsEnum.SeekStatus.FOUND;
					}
				}

				public override void SeekExact(long ord)
				{
					//HM:revisit
					//assert ord < info.terms.size();
					this.termUpto = (int)ord;
				}

				public override BytesRef Next()
				{
					this.termUpto++;
					if (this.termUpto >= this.info.terms.Size())
					{
						return null;
					}
					else
					{
						this.info.terms.Get(this.info.sortedTerms[this.termUpto], this.br);
						return this.br;
					}
				}

				public override BytesRef Term()
				{
					return this.br;
				}

				public override long Ord()
				{
					return this.termUpto;
				}

				public override int DocFreq()
				{
					return 1;
				}

				public override long TotalTermFreq()
				{
					return this.info.sliceArray.freq[this.info.sortedTerms[this.termUpto]];
				}

				public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
				{
					if (reuse == null || !(reuse is MemoryIndex.MemoryIndexReader.MemoryDocsEnum))
					{
						reuse = new MemoryIndex.MemoryIndexReader.MemoryDocsEnum(this);
					}
					return ((MemoryIndex.MemoryIndexReader.MemoryDocsEnum)reuse).Reset(liveDocs, this
						.info.sliceArray.freq[this.info.sortedTerms[this.termUpto]]);
				}

				public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					if (reuse == null || !(reuse is MemoryIndex.MemoryIndexReader.MemoryDocsAndPositionsEnum
						))
					{
						reuse = new MemoryIndex.MemoryIndexReader.MemoryDocsAndPositionsEnum(this);
					}
					int ord = this.info.sortedTerms[this.termUpto];
					return ((MemoryIndex.MemoryIndexReader.MemoryDocsAndPositionsEnum)reuse).Reset(liveDocs
						, this.info.sliceArray.start[ord], this.info.sliceArray.end[ord], this.info.sliceArray
						.freq[ord]);
				}

				public override IComparer<BytesRef> GetComparator()
				{
					return BytesRef.GetUTF8SortedAsUnicodeComparator();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void SeekExact(BytesRef term, Org.Apache.Lucene.Index.TermState state
					)
				{
					//HM:revisit
					//assert state != null;
					this.SeekExact(((OrdTermState)state).ord);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override Org.Apache.Lucene.Index.TermState TermState()
				{
					OrdTermState ts = new OrdTermState();
					ts.ord = this.termUpto;
					return ts;
				}

				private readonly MemoryIndexReader _enclosing;
			}

			private class MemoryDocsEnum : DocsEnum
			{
				private bool hasNext;

				private Bits liveDocs;

				private int doc = -1;

				private int freq;

				public virtual DocsEnum Reset(Bits liveDocs, int freq)
				{
					this.liveDocs = liveDocs;
					this.hasNext = true;
					this.doc = -1;
					this.freq = freq;
					return this;
				}

				public override int DocID()
				{
					return this.doc;
				}

				public override int NextDoc()
				{
					if (this.hasNext && (this.liveDocs == null || this.liveDocs.Get(0)))
					{
						this.hasNext = false;
						return this.doc = 0;
					}
					else
					{
						return this.doc = DocIdSetIterator.NO_MORE_DOCS;
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int Advance(int target)
				{
					return this.SlowAdvance(target);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int Freq()
				{
					return this.freq;
				}

				public override long Cost()
				{
					return 1;
				}

				internal MemoryDocsEnum(MemoryIndexReader _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly MemoryIndexReader _enclosing;
			}

			private class MemoryDocsAndPositionsEnum : DocsAndPositionsEnum
			{
				private int posUpto;

				private bool hasNext;

				private Bits liveDocs;

				private int doc = -1;

				private IntBlockPool.SliceReader sliceReader;

				private int freq;

				private int startOffset;

				private int endOffset;

				public MemoryDocsAndPositionsEnum(MemoryIndexReader _enclosing)
				{
					this._enclosing = _enclosing;
					// for assert
					this.sliceReader = new IntBlockPool.SliceReader(this._enclosing._enclosing.intBlockPool
						);
				}

				public virtual DocsAndPositionsEnum Reset(Bits liveDocs, int start, int end, int 
					freq)
				{
					this.liveDocs = liveDocs;
					this.sliceReader.Reset(start, end);
					this.posUpto = 0;
					// for assert
					this.hasNext = true;
					this.doc = -1;
					this.freq = freq;
					return this;
				}

				public override int DocID()
				{
					return this.doc;
				}

				public override int NextDoc()
				{
					if (this.hasNext && (this.liveDocs == null || this.liveDocs.Get(0)))
					{
						this.hasNext = false;
						return this.doc = 0;
					}
					else
					{
						return this.doc = DocIdSetIterator.NO_MORE_DOCS;
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int Advance(int target)
				{
					return this.SlowAdvance(target);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int Freq()
				{
					return this.freq;
				}

				public override int NextPosition()
				{
					//HM:revisit
					//assert posUpto++ < freq;        
					//assert !sliceReader.endOfSlice() : " stores offsets : " + startOffset;
					if (this._enclosing._enclosing.storeOffsets)
					{
						int pos = this.sliceReader.ReadInt();
						this.startOffset = this.sliceReader.ReadInt();
						this.endOffset = this.sliceReader.ReadInt();
						return pos;
					}
					else
					{
						return this.sliceReader.ReadInt();
					}
				}

				public override int StartOffset()
				{
					return this.startOffset;
				}

				public override int EndOffset()
				{
					return this.endOffset;
				}

				public override BytesRef GetPayload()
				{
					return null;
				}

				public override long Cost()
				{
					return 1;
				}

				private readonly MemoryIndexReader _enclosing;
			}

			public override Fields GetTermVectors(int docID)
			{
				if (docID == 0)
				{
					return this.Fields();
				}
				else
				{
					return null;
				}
			}

			private Similarity GetSimilarity()
			{
				if (this.searcher != null)
				{
					return this.searcher.GetSimilarity();
				}
				return IndexSearcher.GetDefaultSimilarity();
			}

			private void SetSearcher(IndexSearcher searcher)
			{
				this.searcher = searcher;
			}

			public override int NumDocs()
			{
				return 1;
			}

			public override int MaxDoc()
			{
				return 1;
			}

			public override void Document(int docID, StoredFieldVisitor visitor)
			{
			}

			// no-op: there are no stored fields
			protected override void DoClose()
			{
			}

			/// <summary>performance hack: cache norms to avoid repeated expensive calculations</summary>
			private NumericDocValues cachedNormValues;

			private string cachedFieldName;

			private Similarity cachedSimilarity;

			public override NumericDocValues GetNormValues(string field)
			{
				FieldInfo fieldInfo = this._enclosing.fieldInfos.Get(field);
				if (fieldInfo == null || fieldInfo.OmitsNorms())
				{
					return null;
				}
				NumericDocValues norms = this.cachedNormValues;
				Similarity sim = this.GetSimilarity();
				if (!field.Equals(this.cachedFieldName) || sim != this.cachedSimilarity)
				{
					// not cached?
					MemoryIndex.Info info = this.GetInfo(field);
					int numTokens = info != null ? info.numTokens : 0;
					int numOverlapTokens = info != null ? info.numOverlapTokens : 0;
					float boost = info != null ? info.GetBoost() : 1.0f;
					FieldInvertState invertState = new FieldInvertState(field, 0, numTokens, numOverlapTokens
						, 0, boost);
					long value = sim.ComputeNorm(invertState);
					norms = new MemoryIndexNormDocValues(value);
					// cache it for future reuse
					this.cachedNormValues = norms;
					this.cachedFieldName = field;
					this.cachedSimilarity = sim;
				}
				return norms;
			}

			private readonly MemoryIndex _enclosing;
		}

		/// <summary>
		/// Resets the
		/// <see cref="MemoryIndex">MemoryIndex</see>
		/// to its initial state and recycles all internal buffers.
		/// </summary>
		public virtual void Reset()
		{
			this.fieldInfos.Clear();
			this.fields.Clear();
			this.sortedFields = null;
			byteBlockPool.Reset(false, false);
			// no need to 0-fill the buffers
			intBlockPool.Reset(true, false);
		}

		private sealed class SliceByteStartArray : BytesRefHash.DirectBytesStartArray
		{
			internal int[] start;

			internal int[] end;

			internal int[] freq;

			public SliceByteStartArray(int initSize) : base(initSize)
			{
			}

			// here must must 0-fill since we use slices
			// the start offset in the IntBlockPool per term
			// the end pointer in the IntBlockPool for the postings slice per term
			// the term frequency
			public override int[] Init()
			{
				int[] ord = base.Init();
				start = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
				end = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
				freq = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
				//HM:revisit
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
				//HM:revisit
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
