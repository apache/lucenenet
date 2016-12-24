// <summary>
// Copyright 2004-2005 The Apache Software Foundation./// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </summary>

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Queries.Mlt
{
    /// <summary>
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
    /// You may want to use <seealso cref="#setFieldNames setFieldNames(...)"/> so you can examine
    /// multiple fields (e.g. body and title) for similarity.
    /// <p/>
    /// <p/>
    /// Depending on the size of your index and the size and makeup of your documents you
    /// may want to call the other set methods to control how the similarity queries are
    /// generated:
    /// <ul>
    /// <li> <seealso cref="#setMinTermFreq setMinTermFreq(...)"/>
    /// <li> <seealso cref="#setMinDocFreq setMinDocFreq(...)"/>
    /// <li> <seealso cref="#setMaxDocFreq setMaxDocFreq(...)"/>
    /// <li> <seealso cref="#setMaxDocFreqPct setMaxDocFreqPct(...)"/>
    /// <li> <seealso cref="#setMinWordLen setMinWordLen(...)"/>
    /// <li> <seealso cref="#setMaxWordLen setMaxWordLen(...)"/>
    /// <li> <seealso cref="#setMaxQueryTerms setMaxQueryTerms(...)"/>
    /// <li> <seealso cref="#setMaxNumTokensParsed setMaxNumTokensParsed(...)"/>
    /// <li> <seealso cref="#setStopWords setStopWord(...)"/>
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
    /// </summary>
    public sealed class MoreLikeThis
    {

        /// <summary>
        /// Default maximum number of tokens to parse in each example doc field that is not stored with TermVector support.
        /// </summary>
        /// <seealso cref= #getMaxNumTokensParsed </seealso>
        public const int DEFAULT_MAX_NUM_TOKENS_PARSED = 5000;

        /// <summary>
        /// Ignore terms with less than this frequency in the source doc.
        /// </summary>
        /// <seealso cref= #getMinTermFreq </seealso>
        /// <seealso cref= #setMinTermFreq </seealso>
        public const int DEFAULT_MIN_TERM_FREQ = 2;

        /// <summary>
        /// Ignore words which do not occur in at least this many docs.
        /// </summary>
        /// <seealso cref= #getMinDocFreq </seealso>
        /// <seealso cref= #setMinDocFreq </seealso>
        public const int DEFAULT_MIN_DOC_FREQ = 5;

        /// <summary>
        /// Ignore words which occur in more than this many docs.
        /// </summary>
        /// <seealso cref= #getMaxDocFreq </seealso>
        /// <seealso cref= #setMaxDocFreq </seealso>
        /// <seealso cref= #setMaxDocFreqPct </seealso>
        public static readonly int DEFAULT_MAX_DOC_FREQ = int.MaxValue;

        /// <summary>
        /// Boost terms in query based on score.
        /// </summary>
        /// <seealso cref= #isBoost </seealso>
        /// <seealso cref= #setBoost </seealso>
        public const bool DEFAULT_BOOST = false;

        /// <summary>
        /// Default field names. Null is used to specify that the field names should be looked
        /// up at runtime from the provided reader.
        /// </summary>
        public static readonly string[] DEFAULT_FIELD_NAMES = new string[] { "contents" };

        /// <summary>
        /// Ignore words less than this length or if 0 then this has no effect.
        /// </summary>
        /// <seealso cref= #getMinWordLen </seealso>
        /// <seealso cref= #setMinWordLen </seealso>
        public const int DEFAULT_MIN_WORD_LENGTH = 0;

        /// <summary>
        /// Ignore words greater than this length or if 0 then this has no effect.
        /// </summary>
        /// <seealso cref= #getMaxWordLen </seealso>
        /// <seealso cref= #setMaxWordLen </seealso>
        public const int DEFAULT_MAX_WORD_LENGTH = 0;

        /// <summary>
        /// Default set of stopwords.
        /// If null means to allow stop words.
        /// </summary>
        /// <seealso cref= #setStopWords </seealso>
        /// <seealso cref= #getStopWords </seealso>
        public const ISet<string> DEFAULT_STOP_WORDS = null;

        /// <summary>
        /// Return a Query with no more than this many terms.
        /// </summary>
        /// <seealso cref= BooleanQuery#getMaxClauseCount </seealso>
        /// <seealso cref= #getMaxQueryTerms </seealso>
        /// <seealso cref= #setMaxQueryTerms </seealso>
        public const int DEFAULT_MAX_QUERY_TERMS = 25;

        /// <summary>
        /// IndexReader to use
        /// </summary>
        private readonly IndexReader ir;

        /// <summary>
        /// Boost factor to use when boosting the terms
        /// </summary>
        private float boostFactor = 1;

        /// <summary>
        /// Returns the boost factor used when boosting terms
        /// </summary>
        /// <returns> the boost factor used when boosting terms </returns>
        /// <seealso cref= #setBoostFactor(float) </seealso>
        public float BoostFactor
        {
            get
            {
                return boostFactor;
            }
            set
            {
                this.boostFactor = value;
            }
        }


        /// <summary>
        /// Constructor requiring an IndexReader.
        /// </summary>
        public MoreLikeThis(IndexReader ir)
            : this(ir, new DefaultSimilarity())
        {
        }

        public MoreLikeThis(IndexReader ir, TFIDFSimilarity sim)
        {
            this.ir = ir;
            this.Similarity = sim;
            StopWords = DEFAULT_STOP_WORDS;

            MinTermFreq = DEFAULT_MIN_TERM_FREQ;
            MinDocFreq = DEFAULT_MIN_DOC_FREQ;
            MaxDocFreq = DEFAULT_MAX_DOC_FREQ;
            Boost = DEFAULT_BOOST;
            FieldNames = DEFAULT_FIELD_NAMES;
            MaxNumTokensParsed = DEFAULT_MAX_NUM_TOKENS_PARSED;
            MinWordLen = DEFAULT_MIN_WORD_LENGTH;
            MaxWordLen = DEFAULT_MAX_WORD_LENGTH;
            MaxQueryTerms = DEFAULT_MAX_QUERY_TERMS;
        }


        public TFIDFSimilarity Similarity { get; set; }


        /// <summary>
        /// Returns an analyzer that will be used to parse source doc with. The default analyzer
        /// is not set.
        /// </summary>
        /// <returns> the analyzer that will be used to parse source doc with. </returns>
        public Analyzer Analyzer { get; set; }


        /// <summary>
        /// Returns the frequency below which terms will be ignored in the source doc. The default
        /// frequency is the <seealso cref="#DEFAULT_MIN_TERM_FREQ"/>.
        /// </summary>
        /// <returns> the frequency below which terms will be ignored in the source doc. </returns>
        public int MinTermFreq { get; set; }


        /// <summary>
        /// Returns the frequency at which words will be ignored which do not occur in at least this
        /// many docs. The default frequency is <seealso cref="#DEFAULT_MIN_DOC_FREQ"/>.
        /// </summary>
        /// <returns> the frequency at which words will be ignored which do not occur in at least this
        ///         many docs. </returns>
        public int MinDocFreq { get; set; }


        /// <summary>
        /// Returns the maximum frequency in which words may still appear.
        /// Words that appear in more than this many docs will be ignored. The default frequency is
        /// <seealso cref="#DEFAULT_MAX_DOC_FREQ"/>.
        /// </summary>
        /// <returns> get the maximum frequency at which words are still allowed,
        ///         words which occur in more docs than this are ignored. </returns>
        public int MaxDocFreq { get; set; }


        /// <summary>
        /// Set the maximum percentage in which words may still appear. Words that appear
        /// in more than this many percent of all docs will be ignored.
        /// </summary>
        /// <param name="maxPercentage"> the maximum percentage of documents (0-100) that a term may appear
        /// in to be still considered relevant </param>
        public int MaxDocFreqPct
        {
            set
            {
                this.MaxDocFreq = value * ir.NumDocs / 100;
            }
        }


        /// <summary>
        /// Returns whether to boost terms in query based on "score" or not. The default is
        /// <seealso cref="#DEFAULT_BOOST"/>.
        /// </summary>
        /// <returns> whether to boost terms in query based on "score" or not. </returns>
        /// <seealso cref= #setBoost </seealso>
        public bool Boost { get; set; }


        /// <summary>
        /// Returns the field names that will be used when generating the 'More Like This' query.
        /// The default field names that will be used is <seealso cref="#DEFAULT_FIELD_NAMES"/>.
        /// </summary>
        /// <returns> the field names that will be used when generating the 'More Like This' query. </returns>
        public string[] FieldNames { get; set; }


        /// <summary>
        /// Returns the minimum word length below which words will be ignored. Set this to 0 for no
        /// minimum word length. The default is <seealso cref="#DEFAULT_MIN_WORD_LENGTH"/>.
        /// </summary>
        /// <returns> the minimum word length below which words will be ignored. </returns>
        public int MinWordLen { get; set; }


        /// <summary>
        /// Returns the maximum word length above which words will be ignored. Set this to 0 for no
        /// maximum word length. The default is <seealso cref="#DEFAULT_MAX_WORD_LENGTH"/>.
        /// </summary>
        /// <returns> the maximum word length above which words will be ignored. </returns>
        public int MaxWordLen { get; set; }


        /// <summary>
        /// Set the set of stopwords.
        /// Any word in this set is considered "uninteresting" and ignored.
        /// Even if your Analyzer allows stopwords, you might want to tell the MoreLikeThis code to ignore them, as
        /// for the purposes of document similarity it seems reasonable to assume that "a stop word is never interesting".
        /// </summary>
        /// <param name="stopWords"> set of stopwords, if null it means to allow stop words </param>
        /// <seealso cref= #getStopWords </seealso>
        public ISet<string> StopWords { get; set; }

        /// <summary>
        /// Returns the maximum number of query terms that will be included in any generated query.
        /// The default is <seealso cref="#DEFAULT_MAX_QUERY_TERMS"/>.
        /// </summary>
        /// <returns> the maximum number of query terms that will be included in any generated query. </returns>
        public int MaxQueryTerms { get; set; }


        /// <returns> The maximum number of tokens to parse in each example doc field that is not stored with TermVector support </returns>
        /// <seealso cref= #DEFAULT_MAX_NUM_TOKENS_PARSED </seealso>
        public int MaxNumTokensParsed { get; set; }



        /// <summary>
        /// Return a query that will return docs like the passed lucene document ID.
        /// </summary>
        /// <param name="docNum"> the documentID of the lucene doc to generate the 'More Like This" query for. </param>
        /// <returns> a query that will return docs like the passed lucene document ID. </returns>
        public Query Like(int docNum)
        {
            if (FieldNames == null)
            {
                // gather list of valid fields from lucene
                ICollection<string> fields = MultiFields.GetIndexedFields(ir);
                FieldNames = fields.ToArray();
            }

            return CreateQuery(RetrieveTerms(docNum));
        }

        /// <summary>
        /// Return a query that will return docs like the passed Reader.
        /// </summary>
        /// <returns> a query that will return docs like the passed Reader. </returns>
        public Query Like(Reader r, string fieldName)
        {
            return CreateQuery(RetrieveTerms(r, fieldName));
        }

        /// <summary>
        /// Create the More like query from a PriorityQueue
        /// </summary>
        private Query CreateQuery(PriorityQueue<object[]> q)
        {
            BooleanQuery query = new BooleanQuery();
            object cur;
            int qterms = 0;
            float bestScore = 0;

            while ((cur = q.Pop()) != null)
            {
                var ar = (object[])cur;
                var tq = new TermQuery(new Term((string)ar[1], (string)ar[0]));

                if (Boost)
                {
                    if (qterms == 0)
                    {
                        bestScore = ((float)ar[2]);
                    }
                    float myScore = ((float)ar[2]);

                    tq.Boost = boostFactor * myScore / bestScore;
                }

                try
                {
                    query.Add(tq, Occur.SHOULD);
                }
                catch (BooleanQuery.TooManyClauses)
                {
                    break;
                }

                qterms++;
                if (MaxQueryTerms > 0 && qterms >= MaxQueryTerms)
                {
                    break;
                }
            }

            return query;
        }

        /// <summary>
        /// Create a PriorityQueue from a word->tf map.
        /// </summary>
        /// <param name="words"> a map of words keyed on the word(String) with Int objects as the values. </param>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private org.apache.lucene.util.PriorityQueue<Object[]> createQueue(Map<String, Int> words) throws IOException
        private PriorityQueue<object[]> createQueue(IDictionary<string, Int> words)
        {
            // have collected all words in doc and their freqs
            int numDocs = ir.NumDocs;
            FreqQ res = new FreqQ(words.Count); // will order words by score

            foreach (string word in words.Keys) // for every word
            {
                int tf = words[word].x; // term freq in the source doc
                if (MinTermFreq > 0 && tf < MinTermFreq)
                {
                    continue; // filter out words that don't occur enough times in the source
                }

                // go through all the fields and find the largest document frequency
                string topField = FieldNames[0];
                int docFreq = 0;
                foreach (string fieldName in FieldNames)
                {
                    int freq = ir.DocFreq(new Term(fieldName, word));
                    topField = (freq > docFreq) ? fieldName : topField;
                    docFreq = (freq > docFreq) ? freq : docFreq;
                }

                if (MinDocFreq > 0 && docFreq < MinDocFreq)
                {
                    continue; // filter out words that don't occur in enough docs
                }

                if (docFreq > MaxDocFreq)
                {
                    continue; // filter out words that occur in too many docs
                }

                if (docFreq == 0)
                {
                    continue; // index update problem?
                }

                float idf = Similarity.Idf(docFreq, numDocs);
                float score = tf * idf;

                // only really need 1st 3 entries, other ones are for troubleshooting
                res.InsertWithOverflow(new object[] { word, topField, score, idf, docFreq, tf }); // freq in all docs -  idf -  overall score -  the top field -  the word
            }
            return res;
        }

        /// <summary>
        /// Describe the parameters that control how the "more like this" query is formed.
        /// </summary>
        public string describeParams()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\t").Append("maxQueryTerms  : ").Append(MaxQueryTerms).Append("\n");
            sb.Append("\t").Append("minWordLen     : ").Append(MinWordLen).Append("\n");
            sb.Append("\t").Append("maxWordLen     : ").Append(MaxWordLen).Append("\n");
            sb.Append("\t").Append("fieldNames     : ");
            string delim = "";
            foreach (string fieldName in FieldNames)
            {
                sb.Append(delim).Append(fieldName);
                delim = ", ";
            }
            sb.Append("\n");
            sb.Append("\t").Append("boost          : ").Append(Boost).Append("\n");
            sb.Append("\t").Append("minTermFreq    : ").Append(MinTermFreq).Append("\n");
            sb.Append("\t").Append("minDocFreq     : ").Append(MinDocFreq).Append("\n");
            return sb.ToString();
        }

        /// <summary>
        /// Find words for a more-like-this query former.
        /// </summary>
        /// <param name="docNum"> the id of the lucene document from which to find terms </param>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public org.apache.lucene.util.PriorityQueue<Object[]> retrieveTerms(int docNum) throws IOException
        public PriorityQueue<object[]> RetrieveTerms(int docNum)
        {
            IDictionary<string, Int> termFreqMap = new Dictionary<string, Int>();
            foreach (string fieldName in FieldNames)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final org.apache.lucene.index.Fields vectors = ir.getTermVectors(docNum);
                Fields vectors = ir.GetTermVectors(docNum);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final org.apache.lucene.index.Terms vector;
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
                    Document d = ir.Document(docNum);
                    IIndexableField[] fields = d.GetFields(fieldName);
                    foreach (IIndexableField field in fields)
                    {
                        string stringValue = field.GetStringValue();
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

            return createQueue(termFreqMap);
        }

        /// <summary>
        /// Adds terms and frequencies found in vector into the Map termFreqMap
        /// </summary>
        /// <param name="termFreqMap"> a Map of terms and their frequencies </param>
        /// <param name="vector"> List of terms and their frequencies for a doc/field </param>
        private void AddTermFrequencies(IDictionary<string, Int> termFreqMap, Terms vector)
        {
            var termsEnum = vector.Iterator(null);
            var spare = new CharsRef();
            BytesRef text;
            while ((text = termsEnum.Next()) != null)
            {
                UnicodeUtil.UTF8toUTF16(text, spare);
                var term = spare.ToString();
                if (IsNoiseWord(term))
                {
                    continue;
                }
                var freq = (int)termsEnum.TotalTermFreq();

                // increment frequency
                Int cnt;
                if (!termFreqMap.TryGetValue(term, out cnt))
                {
                    cnt = new Int();
                    termFreqMap[term] = cnt;
                    cnt.x = freq;
                }
                else
                {
                    cnt.x += freq;
                }
            }
        }

        /// <summary>
        /// Adds term frequencies found by tokenizing text from reader into the Map words
        /// </summary>
        /// <param name="r"> a source of text to be tokenized </param>
        /// <param name="termFreqMap"> a Map of terms and their frequencies </param>
        /// <param name="fieldName"> Used by analyzer for any special per-field analysis </param>
        private void AddTermFrequencies(Reader r, IDictionary<string, Int> termFreqMap, string fieldName)
        {
            if (Analyzer == null)
            {
                throw new System.NotSupportedException("To use MoreLikeThis without " + "term vectors, you must provide an Analyzer");
            }
            var ts = Analyzer.TokenStream(fieldName, r);
            try
            {
                int tokenCount = 0;
                // for every token
                var termAtt = ts.AddAttribute<ICharTermAttribute>();
                ts.Reset();
                while (ts.IncrementToken())
                {
                    string word = termAtt.ToString();
                    tokenCount++;
                    if (tokenCount > MaxNumTokensParsed)
                    {
                        break;
                    }
                    if (IsNoiseWord(word))
                    {
                        continue;
                    }

                    // increment frequency
                    Int cnt;
                    if (!termFreqMap.TryGetValue(word, out cnt))
                    {
                        termFreqMap[word] = new Int();
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


        /// <summary>
        /// determines if the passed term is likely to be of interest in "more like" comparisons
        /// </summary>
        /// <param name="term"> The word being considered </param>
        /// <returns> true if should be ignored, false if should be used in further analysis </returns>
        private bool IsNoiseWord(string term)
        {
            int len = term.Length;
            if (MinWordLen > 0 && len < MinWordLen)
            {
                return true;
            }
            if (MaxWordLen > 0 && len > MaxWordLen)
            {
                return true;
            }
            return StopWords != null && StopWords.Contains(term);
        }


        /// <summary>
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
        /// For an easier method to call see <seealso cref="#retrieveInterestingTerms retrieveInterestingTerms()"/>.
        /// </summary>
        /// <param name="r"> the reader that has the content of the document </param>
        /// <param name="fieldName"> field passed to the analyzer to use when analyzing the content </param>
        /// <returns> the most interesting words in the document ordered by score, with the highest scoring, or best entry, first </returns>
        /// <seealso cref= #retrieveInterestingTerms </seealso>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public org.apache.lucene.util.PriorityQueue<Object[]> retrieveTerms(Reader r, String fieldName) throws IOException
        public PriorityQueue<object[]> RetrieveTerms(Reader r, string fieldName)
        {
            IDictionary<string, Int> words = new Dictionary<string, Int>();
            AddTermFrequencies(r, words, fieldName);
            return createQueue(words);
        }

        /// <seealso cref= #retrieveInterestingTerms(java.io.Reader, String) </seealso>
        public string[] RetrieveInterestingTerms(int docNum)
        {
            var al = new List<string>(MaxQueryTerms);
            var pq = RetrieveTerms(docNum);
            object cur;
            int lim = MaxQueryTerms; // have to be careful, retrieveTerms returns all words but that's probably not useful to our caller...
            // we just want to return the top words
            while (((cur = pq.Pop()) != null) && lim-- > 0)
            {
                var ar = (object[])cur;
                al.Add(ar[0].ToString()); // the 1st entry is the interesting word
            }
            return al.ToArray();
        }

        /// <summary>
        /// Convenience routine to make it easy to return the most interesting words in a document.
        /// More advanced users will call <seealso cref="#retrieveTerms(Reader, String) retrieveTerms()"/> directly.
        /// </summary>
        /// <param name="r"> the source document </param>
        /// <param name="fieldName"> field passed to analyzer to use when analyzing the content </param>
        /// <returns> the most interesting words in the document </returns>
        /// <seealso cref= #retrieveTerms(java.io.Reader, String) </seealso>
        /// <seealso cref= #setMaxQueryTerms </seealso>
        public string[] RetrieveInterestingTerms(Reader r, string fieldName)
        {
            var al = new List<string>(MaxQueryTerms);
            PriorityQueue<object[]> pq = RetrieveTerms(r, fieldName);
            object cur;
            int lim = MaxQueryTerms; // have to be careful, retrieveTerms returns all words but that's probably not useful to our caller...
            // we just want to return the top words
            while (((cur = pq.Pop()) != null) && lim-- > 0)
            {
                var ar = (object[])cur;
                al.Add(ar[0].ToString()); // the 1st entry is the interesting word
            }
            return al.ToArray();
        }

        /// <summary>
        /// PriorityQueue that orders words by score.
        /// </summary>
        private class FreqQ : PriorityQueue<object[]>
        {
            internal FreqQ(int s)
                : base(s)
            {
            }

            protected override bool LessThan(object[] aa, object[] bb)
            {
                float? fa = (float?)aa[2];
                float? fb = (float?)bb[2];
                return fa > fb;
            }
        }

        /// <summary>
        /// Use for frequencies and to avoid renewing Integers.
        /// </summary>
        private class Int
        {
            internal int x;

            internal Int()
            {
                x = 1;
            }
        }
    }

}