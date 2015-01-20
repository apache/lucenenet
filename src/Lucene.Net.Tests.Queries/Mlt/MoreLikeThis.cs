/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Mlt;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Similarities;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Mlt
{
	/// <summary>Generate "more like this" similarity queries.</summary>
	/// <remarks>
	/// Generate "more like this" similarity queries.
	/// Based on this mail:
	/// <code><pre>
	/// Lucene does let you access the document frequency of terms, with IndexReader.docFreq().
	/// Term frequencies can be computed by re-tokenizing the text, which, for a single document,
	/// is usually fast enough.  But looking up the docFreq() of every term in the document is
	/// probably too slow.
	/// <p/>
	/// You can use some heuristics to prune the set of terms, to avoid calling docFreq() too much,
	/// or at all.  Since you're trying to maximize a tf*idf score, you're probably most interested
	/// in terms with a high tf. Choosing a tf threshold even as low as two or three will radically
	/// reduce the number of terms under consideration.  Another heuristic is that terms with a
	/// high idf (i.e., a low df) tend to be longer.  So you could threshold the terms by the
	/// number of characters, not selecting anything less than, e.g., six or seven characters.
	/// With these sorts of heuristics you can usually find small set of, e.g., ten or fewer terms
	/// that do a pretty good job of characterizing a document.
	/// <p/>
	/// It all depends on what you're trying to do.  If you're trying to eek out that last percent
	/// of precision and recall regardless of computational difficulty so that you can win a TREC
	/// competition, then the techniques I mention above are useless.  But if you're trying to
	/// provide a "more like this" button on a search results page that does a decent job and has
	/// good performance, such techniques might be useful.
	/// <p/>
	/// An efficient, effective "more-like-this" query generator would be a great contribution, if
	/// anyone's interested.  I'd imagine that it would take a Reader or a String (the document's
	/// text), analyzer Analyzer, and return a set of representative terms using heuristics like those
	/// above.  The frequency and length thresholds could be parameters, etc.
	/// <p/>
	/// Doug
	/// </pre></code>
	/// <p/>
	/// <p/>
	/// <p/>
	/// <h3>Initial Usage</h3>
	/// <p/>
	/// This class has lots of options to try to make it efficient and flexible.
	/// The simplest possible usage is as follows. The bold
	/// fragment is specific to this class.
	/// <p/>
	/// <pre class="prettyprint">
	/// <p/>
	/// IndexReader ir = ...
	/// IndexSearcher is = ...
	/// <p/>
	/// MoreLikeThis mlt = new MoreLikeThis(ir);
	/// Reader target = ... // orig source of doc you want to find similarities to
	/// Query query = mlt.like( target);
	/// <p/>
	/// Hits hits = is.search(query);
	/// // now the usual iteration thru 'hits' - the only thing to watch for is to make sure
	/// //you ignore the doc if it matches your 'target' document, as it should be similar to itself
	/// <p/>
	/// </pre>
	/// <p/>
	/// Thus you:
	/// <ol>
	/// <li> do your normal, Lucene setup for searching,
	/// <li> create a MoreLikeThis,
	/// <li> get the text of the doc you want to find similarities to
	/// <li> then call one of the like() calls to generate a similarity query
	/// <li> call the searcher to find the similar docs
	/// </ol>
	/// <p/>
	/// <h3>More Advanced Usage</h3>
	/// <p/>
	/// You may want to use
	/// <see cref="SetFieldNames(string[])">setFieldNames(...)</see>
	/// so you can examine
	/// multiple fields (e.g. body and title) for similarity.
	/// <p/>
	/// <p/>
	/// Depending on the size of your index and the size and makeup of your documents you
	/// may want to call the other set methods to control how the similarity queries are
	/// generated:
	/// <ul>
	/// <li>
	/// <see cref="SetMinTermFreq(int)">setMinTermFreq(...)</see>
	/// <li>
	/// <see cref="SetMinDocFreq(int)">setMinDocFreq(...)</see>
	/// <li>
	/// <see cref="SetMaxDocFreq(int)">setMaxDocFreq(...)</see>
	/// <li>
	/// <see cref="SetMaxDocFreqPct(int)">setMaxDocFreqPct(...)</see>
	/// <li>
	/// <see cref="SetMinWordLen(int)">setMinWordLen(...)</see>
	/// <li>
	/// <see cref="SetMaxWordLen(int)">setMaxWordLen(...)</see>
	/// <li>
	/// <see cref="SetMaxQueryTerms(int)">setMaxQueryTerms(...)</see>
	/// <li>
	/// <see cref="SetMaxNumTokensParsed(int)">setMaxNumTokensParsed(...)</see>
	/// <li>
	/// <see cref="SetStopWords(System.Collections.Generic.ICollection{E})">setStopWord(...)
	/// 	</see>
	/// </ul>
	/// <p/>
	/// <hr>
	/// <pre>
	/// Changes: Mark Harwood 29/02/04
	/// Some bugfixing, some refactoring, some optimisation.
	/// - bugfix: retrieveTerms(int docNum) was not working for indexes without a termvector -added missing code
	/// - bugfix: No significant terms being created for fields with a termvector - because
	/// was only counting one occurrence per term/field pair in calculations(ie not including frequency info from TermVector)
	/// - refactor: moved common code into isNoiseWord()
	/// - optimise: when no termvector support available - used maxNumTermsParsed to limit amount of tokenization
	/// </pre>
	/// </remarks>
	public sealed class MoreLikeThis
	{
		/// <summary>Default maximum number of tokens to parse in each example doc field that is not stored with TermVector support.
		/// 	</summary>
		/// <remarks>Default maximum number of tokens to parse in each example doc field that is not stored with TermVector support.
		/// 	</remarks>
		/// <seealso cref="GetMaxNumTokensParsed()">GetMaxNumTokensParsed()</seealso>
		public const int DEFAULT_MAX_NUM_TOKENS_PARSED = 5000;

		/// <summary>Ignore terms with less than this frequency in the source doc.</summary>
		/// <remarks>Ignore terms with less than this frequency in the source doc.</remarks>
		/// <seealso cref="GetMinTermFreq()">GetMinTermFreq()</seealso>
		/// <seealso cref="SetMinTermFreq(int)">SetMinTermFreq(int)</seealso>
		public const int DEFAULT_MIN_TERM_FREQ = 2;

		/// <summary>Ignore words which do not occur in at least this many docs.</summary>
		/// <remarks>Ignore words which do not occur in at least this many docs.</remarks>
		/// <seealso cref="GetMinDocFreq()">GetMinDocFreq()</seealso>
		/// <seealso cref="SetMinDocFreq(int)">SetMinDocFreq(int)</seealso>
		public const int DEFAULT_MIN_DOC_FREQ = 5;

		/// <summary>Ignore words which occur in more than this many docs.</summary>
		/// <remarks>Ignore words which occur in more than this many docs.</remarks>
		/// <seealso cref="GetMaxDocFreq()">GetMaxDocFreq()</seealso>
		/// <seealso cref="SetMaxDocFreq(int)">SetMaxDocFreq(int)</seealso>
		/// <seealso cref="SetMaxDocFreqPct(int)">SetMaxDocFreqPct(int)</seealso>
		public const int DEFAULT_MAX_DOC_FREQ = int.MaxValue;

		/// <summary>Boost terms in query based on score.</summary>
		/// <remarks>Boost terms in query based on score.</remarks>
		/// <seealso cref="IsBoost()">IsBoost()</seealso>
		/// <seealso cref="SetBoost(bool)">SetBoost(bool)</seealso>
		public const bool DEFAULT_BOOST = false;

		/// <summary>Default field names.</summary>
		/// <remarks>
		/// Default field names. Null is used to specify that the field names should be looked
		/// up at runtime from the provided reader.
		/// </remarks>
		public static readonly string[] DEFAULT_FIELD_NAMES = new string[] { "contents" };

		/// <summary>Ignore words less than this length or if 0 then this has no effect.</summary>
		/// <remarks>Ignore words less than this length or if 0 then this has no effect.</remarks>
		/// <seealso cref="GetMinWordLen()">GetMinWordLen()</seealso>
		/// <seealso cref="SetMinWordLen(int)">SetMinWordLen(int)</seealso>
		public const int DEFAULT_MIN_WORD_LENGTH = 0;

		/// <summary>Ignore words greater than this length or if 0 then this has no effect.</summary>
		/// <remarks>Ignore words greater than this length or if 0 then this has no effect.</remarks>
		/// <seealso cref="GetMaxWordLen()">GetMaxWordLen()</seealso>
		/// <seealso cref="SetMaxWordLen(int)">SetMaxWordLen(int)</seealso>
		public const int DEFAULT_MAX_WORD_LENGTH = 0;

		/// <summary>Default set of stopwords.</summary>
		/// <remarks>
		/// Default set of stopwords.
		/// If null means to allow stop words.
		/// </remarks>
		/// <seealso cref="SetStopWords(System.Collections.Generic.ICollection{E})">SetStopWords(System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</seealso>
		/// <seealso cref="GetStopWords()">GetStopWords()</seealso>
		public static readonly ICollection<object> DEFAULT_STOP_WORDS = null;

		/// <summary>Current set of stop words.</summary>
		/// <remarks>Current set of stop words.</remarks>
		private ICollection<object> stopWords = DEFAULT_STOP_WORDS;

		/// <summary>Return a Query with no more than this many terms.</summary>
		/// <remarks>Return a Query with no more than this many terms.</remarks>
		/// <seealso cref="Org.Apache.Lucene.Search.BooleanQuery.GetMaxClauseCount()">Org.Apache.Lucene.Search.BooleanQuery.GetMaxClauseCount()
		/// 	</seealso>
		/// <seealso cref="GetMaxQueryTerms()">GetMaxQueryTerms()</seealso>
		/// <seealso cref="SetMaxQueryTerms(int)">SetMaxQueryTerms(int)</seealso>
		public const int DEFAULT_MAX_QUERY_TERMS = 25;

		/// <summary>Analyzer that will be used to parse the doc.</summary>
		/// <remarks>Analyzer that will be used to parse the doc.</remarks>
		private Analyzer analyzer = null;

		/// <summary>Ignore words less frequent that this.</summary>
		/// <remarks>Ignore words less frequent that this.</remarks>
		private int minTermFreq = DEFAULT_MIN_TERM_FREQ;

		/// <summary>Ignore words which do not occur in at least this many docs.</summary>
		/// <remarks>Ignore words which do not occur in at least this many docs.</remarks>
		private int minDocFreq = DEFAULT_MIN_DOC_FREQ;

		/// <summary>Ignore words which occur in more than this many docs.</summary>
		/// <remarks>Ignore words which occur in more than this many docs.</remarks>
		private int maxDocFreq = DEFAULT_MAX_DOC_FREQ;

		/// <summary>Should we apply a boost to the Query based on the scores?</summary>
		private bool boost = DEFAULT_BOOST;

		/// <summary>Field name we'll analyze.</summary>
		/// <remarks>Field name we'll analyze.</remarks>
		private string[] fieldNames = DEFAULT_FIELD_NAMES;

		/// <summary>The maximum number of tokens to parse in each example doc field that is not stored with TermVector support
		/// 	</summary>
		private int maxNumTokensParsed = DEFAULT_MAX_NUM_TOKENS_PARSED;

		/// <summary>Ignore words if less than this len.</summary>
		/// <remarks>Ignore words if less than this len.</remarks>
		private int minWordLen = DEFAULT_MIN_WORD_LENGTH;

		/// <summary>Ignore words if greater than this len.</summary>
		/// <remarks>Ignore words if greater than this len.</remarks>
		private int maxWordLen = DEFAULT_MAX_WORD_LENGTH;

		/// <summary>Don't return a query longer than this.</summary>
		/// <remarks>Don't return a query longer than this.</remarks>
		private int maxQueryTerms = DEFAULT_MAX_QUERY_TERMS;

		/// <summary>For idf() calculations.</summary>
		/// <remarks>For idf() calculations.</remarks>
		private TFIDFSimilarity similarity;

		/// <summary>IndexReader to use</summary>
		private readonly IndexReader ir;

		/// <summary>Boost factor to use when boosting the terms</summary>
		private float boostFactor = 1;

		// = new DefaultSimilarity();
		/// <summary>Returns the boost factor used when boosting terms</summary>
		/// <returns>the boost factor used when boosting terms</returns>
		/// <seealso cref="SetBoostFactor(float)">SetBoostFactor(float)</seealso>
		public float GetBoostFactor()
		{
			return boostFactor;
		}

		/// <summary>Sets the boost factor to use when boosting terms</summary>
		/// <seealso cref="GetBoostFactor()">GetBoostFactor()</seealso>
		public void SetBoostFactor(float boostFactor)
		{
			this.boostFactor = boostFactor;
		}

		/// <summary>Constructor requiring an IndexReader.</summary>
		/// <remarks>Constructor requiring an IndexReader.</remarks>
		public MoreLikeThis(IndexReader ir) : this(ir, new DefaultSimilarity())
		{
		}

		public MoreLikeThis(IndexReader ir, TFIDFSimilarity sim)
		{
			this.ir = ir;
			this.similarity = sim;
		}

		public TFIDFSimilarity GetSimilarity()
		{
			return similarity;
		}

		public void SetSimilarity(TFIDFSimilarity similarity)
		{
			this.similarity = similarity;
		}

		/// <summary>Returns an analyzer that will be used to parse source doc with.</summary>
		/// <remarks>
		/// Returns an analyzer that will be used to parse source doc with. The default analyzer
		/// is not set.
		/// </remarks>
		/// <returns>the analyzer that will be used to parse source doc with.</returns>
		public Analyzer GetAnalyzer()
		{
			return analyzer;
		}

		/// <summary>Sets the analyzer to use.</summary>
		/// <remarks>
		/// Sets the analyzer to use. An analyzer is not required for generating a query with the
		/// <see cref="Like(int)">Like(int)</see>
		/// method, all other 'like' methods require an analyzer.
		/// </remarks>
		/// <param name="analyzer">the analyzer to use to tokenize text.</param>
		public void SetAnalyzer(Analyzer analyzer)
		{
			this.analyzer = analyzer;
		}

		/// <summary>Returns the frequency below which terms will be ignored in the source doc.
		/// 	</summary>
		/// <remarks>
		/// Returns the frequency below which terms will be ignored in the source doc. The default
		/// frequency is the
		/// <see cref="DEFAULT_MIN_TERM_FREQ">DEFAULT_MIN_TERM_FREQ</see>
		/// .
		/// </remarks>
		/// <returns>the frequency below which terms will be ignored in the source doc.</returns>
		public int GetMinTermFreq()
		{
			return minTermFreq;
		}

		/// <summary>Sets the frequency below which terms will be ignored in the source doc.</summary>
		/// <remarks>Sets the frequency below which terms will be ignored in the source doc.</remarks>
		/// <param name="minTermFreq">the frequency below which terms will be ignored in the source doc.
		/// 	</param>
		public void SetMinTermFreq(int minTermFreq)
		{
			this.minTermFreq = minTermFreq;
		}

		/// <summary>
		/// Returns the frequency at which words will be ignored which do not occur in at least this
		/// many docs.
		/// </summary>
		/// <remarks>
		/// Returns the frequency at which words will be ignored which do not occur in at least this
		/// many docs. The default frequency is
		/// <see cref="DEFAULT_MIN_DOC_FREQ">DEFAULT_MIN_DOC_FREQ</see>
		/// .
		/// </remarks>
		/// <returns>
		/// the frequency at which words will be ignored which do not occur in at least this
		/// many docs.
		/// </returns>
		public int GetMinDocFreq()
		{
			return minDocFreq;
		}

		/// <summary>
		/// Sets the frequency at which words will be ignored which do not occur in at least this
		/// many docs.
		/// </summary>
		/// <remarks>
		/// Sets the frequency at which words will be ignored which do not occur in at least this
		/// many docs.
		/// </remarks>
		/// <param name="minDocFreq">
		/// the frequency at which words will be ignored which do not occur in at
		/// least this many docs.
		/// </param>
		public void SetMinDocFreq(int minDocFreq)
		{
			this.minDocFreq = minDocFreq;
		}

		/// <summary>Returns the maximum frequency in which words may still appear.</summary>
		/// <remarks>
		/// Returns the maximum frequency in which words may still appear.
		/// Words that appear in more than this many docs will be ignored. The default frequency is
		/// <see cref="DEFAULT_MAX_DOC_FREQ">DEFAULT_MAX_DOC_FREQ</see>
		/// .
		/// </remarks>
		/// <returns>
		/// get the maximum frequency at which words are still allowed,
		/// words which occur in more docs than this are ignored.
		/// </returns>
		public int GetMaxDocFreq()
		{
			return maxDocFreq;
		}

		/// <summary>Set the maximum frequency in which words may still appear.</summary>
		/// <remarks>
		/// Set the maximum frequency in which words may still appear. Words that appear
		/// in more than this many docs will be ignored.
		/// </remarks>
		/// <param name="maxFreq">
		/// the maximum count of documents that a term may appear
		/// in to be still considered relevant
		/// </param>
		public void SetMaxDocFreq(int maxFreq)
		{
			this.maxDocFreq = maxFreq;
		}

		/// <summary>Set the maximum percentage in which words may still appear.</summary>
		/// <remarks>
		/// Set the maximum percentage in which words may still appear. Words that appear
		/// in more than this many percent of all docs will be ignored.
		/// </remarks>
		/// <param name="maxPercentage">
		/// the maximum percentage of documents (0-100) that a term may appear
		/// in to be still considered relevant
		/// </param>
		public void SetMaxDocFreqPct(int maxPercentage)
		{
			this.maxDocFreq = maxPercentage * ir.NumDocs() / 100;
		}

		/// <summary>Returns whether to boost terms in query based on "score" or not.</summary>
		/// <remarks>
		/// Returns whether to boost terms in query based on "score" or not. The default is
		/// <see cref="DEFAULT_BOOST">DEFAULT_BOOST</see>
		/// .
		/// </remarks>
		/// <returns>whether to boost terms in query based on "score" or not.</returns>
		/// <seealso cref="SetBoost(bool)">SetBoost(bool)</seealso>
		public bool IsBoost()
		{
			return boost;
		}

		/// <summary>Sets whether to boost terms in query based on "score" or not.</summary>
		/// <remarks>Sets whether to boost terms in query based on "score" or not.</remarks>
		/// <param name="boost">true to boost terms in query based on "score", false otherwise.
		/// 	</param>
		/// <seealso cref="IsBoost()">IsBoost()</seealso>
		public void SetBoost(bool boost)
		{
			this.boost = boost;
		}

		/// <summary>Returns the field names that will be used when generating the 'More Like This' query.
		/// 	</summary>
		/// <remarks>
		/// Returns the field names that will be used when generating the 'More Like This' query.
		/// The default field names that will be used is
		/// <see cref="DEFAULT_FIELD_NAMES">DEFAULT_FIELD_NAMES</see>
		/// .
		/// </remarks>
		/// <returns>the field names that will be used when generating the 'More Like This' query.
		/// 	</returns>
		public string[] GetFieldNames()
		{
			return fieldNames;
		}

		/// <summary>Sets the field names that will be used when generating the 'More Like This' query.
		/// 	</summary>
		/// <remarks>
		/// Sets the field names that will be used when generating the 'More Like This' query.
		/// Set this to null for the field names to be determined at runtime from the IndexReader
		/// provided in the constructor.
		/// </remarks>
		/// <param name="fieldNames">
		/// the field names that will be used when generating the 'More Like This'
		/// query.
		/// </param>
		public void SetFieldNames(string[] fieldNames)
		{
			this.fieldNames = fieldNames;
		}

		/// <summary>Returns the minimum word length below which words will be ignored.</summary>
		/// <remarks>
		/// Returns the minimum word length below which words will be ignored. Set this to 0 for no
		/// minimum word length. The default is
		/// <see cref="DEFAULT_MIN_WORD_LENGTH">DEFAULT_MIN_WORD_LENGTH</see>
		/// .
		/// </remarks>
		/// <returns>the minimum word length below which words will be ignored.</returns>
		public int GetMinWordLen()
		{
			return minWordLen;
		}

		/// <summary>Sets the minimum word length below which words will be ignored.</summary>
		/// <remarks>Sets the minimum word length below which words will be ignored.</remarks>
		/// <param name="minWordLen">the minimum word length below which words will be ignored.
		/// 	</param>
		public void SetMinWordLen(int minWordLen)
		{
			this.minWordLen = minWordLen;
		}

		/// <summary>Returns the maximum word length above which words will be ignored.</summary>
		/// <remarks>
		/// Returns the maximum word length above which words will be ignored. Set this to 0 for no
		/// maximum word length. The default is
		/// <see cref="DEFAULT_MAX_WORD_LENGTH">DEFAULT_MAX_WORD_LENGTH</see>
		/// .
		/// </remarks>
		/// <returns>the maximum word length above which words will be ignored.</returns>
		public int GetMaxWordLen()
		{
			return maxWordLen;
		}

		/// <summary>Sets the maximum word length above which words will be ignored.</summary>
		/// <remarks>Sets the maximum word length above which words will be ignored.</remarks>
		/// <param name="maxWordLen">the maximum word length above which words will be ignored.
		/// 	</param>
		public void SetMaxWordLen(int maxWordLen)
		{
			this.maxWordLen = maxWordLen;
		}

		/// <summary>Set the set of stopwords.</summary>
		/// <remarks>
		/// Set the set of stopwords.
		/// Any word in this set is considered "uninteresting" and ignored.
		/// Even if your Analyzer allows stopwords, you might want to tell the MoreLikeThis code to ignore them, as
		/// for the purposes of document similarity it seems reasonable to assume that "a stop word is never interesting".
		/// </remarks>
		/// <param name="stopWords">set of stopwords, if null it means to allow stop words</param>
		/// <seealso cref="GetStopWords()">GetStopWords()</seealso>
		public void SetStopWords<_T0>(ICollection<_T0> stopWords)
		{
			this.stopWords = stopWords;
		}

		/// <summary>Get the current stop words being used.</summary>
		/// <remarks>Get the current stop words being used.</remarks>
		/// <seealso cref="SetStopWords(System.Collections.Generic.ICollection{E})">SetStopWords(System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</seealso>
		public ICollection<object> GetStopWords()
		{
			return stopWords;
		}

		/// <summary>Returns the maximum number of query terms that will be included in any generated query.
		/// 	</summary>
		/// <remarks>
		/// Returns the maximum number of query terms that will be included in any generated query.
		/// The default is
		/// <see cref="DEFAULT_MAX_QUERY_TERMS">DEFAULT_MAX_QUERY_TERMS</see>
		/// .
		/// </remarks>
		/// <returns>the maximum number of query terms that will be included in any generated query.
		/// 	</returns>
		public int GetMaxQueryTerms()
		{
			return maxQueryTerms;
		}

		/// <summary>Sets the maximum number of query terms that will be included in any generated query.
		/// 	</summary>
		/// <remarks>Sets the maximum number of query terms that will be included in any generated query.
		/// 	</remarks>
		/// <param name="maxQueryTerms">
		/// the maximum number of query terms that will be included in any
		/// generated query.
		/// </param>
		public void SetMaxQueryTerms(int maxQueryTerms)
		{
			this.maxQueryTerms = maxQueryTerms;
		}

		/// <returns>The maximum number of tokens to parse in each example doc field that is not stored with TermVector support
		/// 	</returns>
		/// <seealso cref="DEFAULT_MAX_NUM_TOKENS_PARSED">DEFAULT_MAX_NUM_TOKENS_PARSED</seealso>
		public int GetMaxNumTokensParsed()
		{
			return maxNumTokensParsed;
		}

		/// <param name="i">The maximum number of tokens to parse in each example doc field that is not stored with TermVector support
		/// 	</param>
		public void SetMaxNumTokensParsed(int i)
		{
			maxNumTokensParsed = i;
		}

		/// <summary>Return a query that will return docs like the passed lucene document ID.
		/// 	</summary>
		/// <remarks>Return a query that will return docs like the passed lucene document ID.
		/// 	</remarks>
		/// <param name="docNum">the documentID of the lucene doc to generate the 'More Like This" query for.
		/// 	</param>
		/// <returns>a query that will return docs like the passed lucene document ID.</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public Query Like(int docNum)
		{
			if (fieldNames == null)
			{
				// gather list of valid fields from lucene
				ICollection<string> fields = MultiFields.GetIndexedFields(ir);
				fieldNames = Sharpen.Collections.ToArray(fields, new string[fields.Count]);
			}
			return CreateQuery(RetrieveTerms(docNum));
		}

		/// <summary>Return a query that will return docs like the passed Reader.</summary>
		/// <remarks>Return a query that will return docs like the passed Reader.</remarks>
		/// <returns>a query that will return docs like the passed Reader.</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public Query Like(StreamReader r, string fieldName)
		{
			return CreateQuery(RetrieveTerms(r, fieldName));
		}

		/// <summary>Create the More like query from a PriorityQueue</summary>
		private Query CreateQuery(PriorityQueue<object[]> q)
		{
			BooleanQuery query = new BooleanQuery();
			object cur;
			int qterms = 0;
			float bestScore = 0;
			while ((cur = q.Pop()) != null)
			{
				object[] ar = (object[])cur;
				TermQuery tq = new TermQuery(new Term((string)ar[1], (string)ar[0]));
				if (boost)
				{
					if (qterms == 0)
					{
						bestScore = ((float)ar[2]);
					}
					float myScore = ((float)ar[2]);
					tq.SetBoost(boostFactor * myScore / bestScore);
				}
				try
				{
					query.Add(tq, BooleanClause.Occur.SHOULD);
				}
				catch (BooleanQuery.TooManyClauses)
				{
					break;
				}
				qterms++;
				if (maxQueryTerms > 0 && qterms >= maxQueryTerms)
				{
					break;
				}
			}
			return query;
		}

		/// <summary>Create a PriorityQueue from a word-&gt;tf map.</summary>
		/// <remarks>Create a PriorityQueue from a word-&gt;tf map.</remarks>
		/// <param name="words">a map of words keyed on the word(String) with Int objects as the values.
		/// 	</param>
		/// <exception cref="System.IO.IOException"></exception>
		private PriorityQueue<object[]> CreateQueue(IDictionary<string, MoreLikeThis.Int>
			 words)
		{
			// have collected all words in doc and their freqs
			int numDocs = ir.NumDocs();
			MoreLikeThis.FreqQ res = new MoreLikeThis.FreqQ(words.Count);
			// will order words by score
			foreach (string word in words.Keys)
			{
				// for every word
				int tf = words.Get(word).x;
				// term freq in the source doc
				if (minTermFreq > 0 && tf < minTermFreq)
				{
					continue;
				}
				// filter out words that don't occur enough times in the source
				// go through all the fields and find the largest document frequency
				string topField = fieldNames[0];
				int docFreq = 0;
				foreach (string fieldName in fieldNames)
				{
					int freq = ir.DocFreq(new Term(fieldName, word));
					topField = (freq > docFreq) ? fieldName : topField;
					docFreq = (freq > docFreq) ? freq : docFreq;
				}
				if (minDocFreq > 0 && docFreq < minDocFreq)
				{
					continue;
				}
				// filter out words that don't occur in enough docs
				if (docFreq > maxDocFreq)
				{
					continue;
				}
				// filter out words that occur in too many docs
				if (docFreq == 0)
				{
					continue;
				}
				// index update problem?
				float idf = similarity.Idf(docFreq, numDocs);
				float score = tf * idf;
				// only really need 1st 3 entries, other ones are for troubleshooting
				res.InsertWithOverflow(new object[] { word, topField, score, idf, docFreq, tf });
			}
			// the word
			// the top field
			// overall score
			// idf
			// freq in all docs
			return res;
		}

		/// <summary>Describe the parameters that control how the "more like this" query is formed.
		/// 	</summary>
		/// <remarks>Describe the parameters that control how the "more like this" query is formed.
		/// 	</remarks>
		public string DescribeParams()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\t").Append("maxQueryTerms  : ").Append(maxQueryTerms).Append("\n");
			sb.Append("\t").Append("minWordLen     : ").Append(minWordLen).Append("\n");
			sb.Append("\t").Append("maxWordLen     : ").Append(maxWordLen).Append("\n");
			sb.Append("\t").Append("fieldNames     : ");
			string delim = string.Empty;
			foreach (string fieldName in fieldNames)
			{
				sb.Append(delim).Append(fieldName);
				delim = ", ";
			}
			sb.Append("\n");
			sb.Append("\t").Append("boost          : ").Append(boost).Append("\n");
			sb.Append("\t").Append("minTermFreq    : ").Append(minTermFreq).Append("\n");
			sb.Append("\t").Append("minDocFreq     : ").Append(minDocFreq).Append("\n");
			return sb.ToString();
		}

		/// <summary>Find words for a more-like-this query former.</summary>
		/// <remarks>Find words for a more-like-this query former.</remarks>
		/// <param name="docNum">the id of the lucene document from which to find terms</param>
		/// <exception cref="System.IO.IOException"></exception>
		public PriorityQueue<object[]> RetrieveTerms(int docNum)
		{
			IDictionary<string, MoreLikeThis.Int> termFreqMap = new Dictionary<string, MoreLikeThis.Int
				>();
			foreach (string fieldName in fieldNames)
			{
				Fields vectors = ir.GetTermVectors(docNum);
				Terms vector;
				if (vectors != null)
				{
					vector = vectors.Terms(fieldName);
				}
				else
				{
					vector = null;
				}
				// field does not store term vector info
				if (vector == null)
				{
					Org.Apache.Lucene.Document.Document d = ir.Document(docNum);
					IndexableField[] fields = d.GetFields(fieldName);
					foreach (IndexableField field in fields)
					{
						string stringValue = field.StringValue();
						if (stringValue != null)
						{
							AddTermFrequencies(new StringReader(stringValue), termFreqMap, fieldName);
						}
					}
				}
				else
				{
					AddTermFrequencies(termFreqMap, vector);
				}
			}
			return CreateQueue(termFreqMap);
		}

		/// <summary>Adds terms and frequencies found in vector into the Map termFreqMap</summary>
		/// <param name="termFreqMap">a Map of terms and their frequencies</param>
		/// <param name="vector">List of terms and their frequencies for a doc/field</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void AddTermFrequencies(IDictionary<string, MoreLikeThis.Int> termFreqMap
			, Terms vector)
		{
			TermsEnum termsEnum = vector.Iterator(null);
			CharsRef spare = new CharsRef();
			BytesRef text;
			while ((text = termsEnum.Next()) != null)
			{
				UnicodeUtil.UTF8toUTF16(text, spare);
				string term = spare.ToString();
				if (IsNoiseWord(term))
				{
					continue;
				}
				int freq = (int)termsEnum.TotalTermFreq();
				// increment frequency
				MoreLikeThis.Int cnt = termFreqMap.Get(term);
				if (cnt == null)
				{
					cnt = new MoreLikeThis.Int();
					termFreqMap.Put(term, cnt);
					cnt.x = freq;
				}
				else
				{
					cnt.x += freq;
				}
			}
		}

		/// <summary>Adds term frequencies found by tokenizing text from reader into the Map words
		/// 	</summary>
		/// <param name="r">a source of text to be tokenized</param>
		/// <param name="termFreqMap">a Map of terms and their frequencies</param>
		/// <param name="fieldName">Used by analyzer for any special per-field analysis</param>
		/// <exception cref="System.IO.IOException"></exception>
		private void AddTermFrequencies(StreamReader r, IDictionary<string, MoreLikeThis.Int
			> termFreqMap, string fieldName)
		{
			if (analyzer == null)
			{
				throw new NotSupportedException("To use MoreLikeThis without " + "term vectors, you must provide an Analyzer"
					);
			}
			TokenStream ts = analyzer.TokenStream(fieldName, r);
			try
			{
				int tokenCount = 0;
				// for every token
				CharTermAttribute termAtt = ts.AddAttribute<CharTermAttribute>();
				ts.Reset();
				while (ts.IncrementToken())
				{
					string word = termAtt.ToString();
					tokenCount++;
					if (tokenCount > maxNumTokensParsed)
					{
						break;
					}
					if (IsNoiseWord(word))
					{
						continue;
					}
					// increment frequency
					MoreLikeThis.Int cnt = termFreqMap.Get(word);
					if (cnt == null)
					{
						termFreqMap.Put(word, new MoreLikeThis.Int());
					}
					else
					{
						cnt.x++;
					}
				}
				ts.End();
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(ts);
			}
		}

		/// <summary>determines if the passed term is likely to be of interest in "more like" comparisons
		/// 	</summary>
		/// <param name="term">The word being considered</param>
		/// <returns>true if should be ignored, false if should be used in further analysis</returns>
		private bool IsNoiseWord(string term)
		{
			int len = term.Length;
			if (minWordLen > 0 && len < minWordLen)
			{
				return true;
			}
			if (maxWordLen > 0 && len > maxWordLen)
			{
				return true;
			}
			return stopWords != null && stopWords.Contains(term);
		}

		/// <summary>Find words for a more-like-this query former.</summary>
		/// <remarks>
		/// Find words for a more-like-this query former.
		/// The result is a priority queue of arrays with one entry for <b>every word</b> in the document.
		/// Each array has 6 elements.
		/// The elements are:
		/// <ol>
		/// <li> The word (String)
		/// <li> The top field that this word comes from (String)
		/// <li> The score for this word (Float)
		/// <li> The IDF value (Float)
		/// <li> The frequency of this word in the index (Integer)
		/// <li> The frequency of this word in the source document (Integer)
		/// </ol>
		/// This is a somewhat "advanced" routine, and in general only the 1st entry in the array is of interest.
		/// This method is exposed so that you can identify the "interesting words" in a document.
		/// For an easier method to call see
		/// <see cref="RetrieveInterestingTerms(int)">retrieveInterestingTerms()</see>
		/// .
		/// </remarks>
		/// <param name="r">the reader that has the content of the document</param>
		/// <param name="fieldName">field passed to the analyzer to use when analyzing the content
		/// 	</param>
		/// <returns>the most interesting words in the document ordered by score, with the highest scoring, or best entry, first
		/// 	</returns>
		/// <seealso cref="RetrieveInterestingTerms(int)">RetrieveInterestingTerms(int)</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public PriorityQueue<object[]> RetrieveTerms(StreamReader r, string fieldName)
		{
			IDictionary<string, MoreLikeThis.Int> words = new Dictionary<string, MoreLikeThis.Int
				>();
			AddTermFrequencies(r, words, fieldName);
			return CreateQueue(words);
		}

		/// <seealso cref="RetrieveInterestingTerms(System.IO.StreamReader, string)">RetrieveInterestingTerms(System.IO.StreamReader, string)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public string[] RetrieveInterestingTerms(int docNum)
		{
			AList<object> al = new AList<object>(maxQueryTerms);
			PriorityQueue<object[]> pq = RetrieveTerms(docNum);
			object cur;
			int lim = maxQueryTerms;
			// have to be careful, retrieveTerms returns all words but that's probably not useful to our caller...
			// we just want to return the top words
			while (((cur = pq.Pop()) != null) && lim-- > 0)
			{
				object[] ar = (object[])cur;
				al.AddItem(ar[0]);
			}
			// the 1st entry is the interesting word
			string[] res = new string[al.Count];
			return Sharpen.Collections.ToArray(al, res);
		}

		/// <summary>Convenience routine to make it easy to return the most interesting words in a document.
		/// 	</summary>
		/// <remarks>
		/// Convenience routine to make it easy to return the most interesting words in a document.
		/// More advanced users will call
		/// <see cref="RetrieveTerms(System.IO.StreamReader, string)">retrieveTerms()</see>
		/// directly.
		/// </remarks>
		/// <param name="r">the source document</param>
		/// <param name="fieldName">field passed to analyzer to use when analyzing the content
		/// 	</param>
		/// <returns>the most interesting words in the document</returns>
		/// <seealso cref="RetrieveTerms(System.IO.StreamReader, string)">RetrieveTerms(System.IO.StreamReader, string)
		/// 	</seealso>
		/// <seealso cref="SetMaxQueryTerms(int)">SetMaxQueryTerms(int)</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public string[] RetrieveInterestingTerms(StreamReader r, string fieldName)
		{
			AList<object> al = new AList<object>(maxQueryTerms);
			PriorityQueue<object[]> pq = RetrieveTerms(r, fieldName);
			object cur;
			int lim = maxQueryTerms;
			// have to be careful, retrieveTerms returns all words but that's probably not useful to our caller...
			// we just want to return the top words
			while (((cur = pq.Pop()) != null) && lim-- > 0)
			{
				object[] ar = (object[])cur;
				al.AddItem(ar[0]);
			}
			// the 1st entry is the interesting word
			string[] res = new string[al.Count];
			return Sharpen.Collections.ToArray(al, res);
		}

		/// <summary>PriorityQueue that orders words by score.</summary>
		/// <remarks>PriorityQueue that orders words by score.</remarks>
		private class FreqQ : PriorityQueue<object[]>
		{
			public FreqQ(int s) : base(s)
			{
			}

			protected override bool LessThan(object[] aa, object[] bb)
			{
				float fa = (float)aa[2];
				float fb = (float)bb[2];
				return fa > fb;
			}
		}

		/// <summary>Use for frequencies and to avoid renewing Integers.</summary>
		/// <remarks>Use for frequencies and to avoid renewing Integers.</remarks>
		private class Int
		{
			internal int x;

			public Int()
			{
				x = 1;
			}
		}
	}
}
