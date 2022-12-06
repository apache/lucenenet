// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Queries.Mlt
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
    /// Generate "more like this" similarity queries.
    /// Based on this mail:
    /// <code>
    /// Lucene does let you access the document frequency of terms, with <see cref="IndexReader.DocFreq"/>.
    /// Term frequencies can be computed by re-tokenizing the text, which, for a single document,
    /// is usually fast enough.  But looking up the <see cref="IndexReader.DocFreq"/> of every term in the document is
    /// probably too slow.
    /// <para/>
    /// You can use some heuristics to prune the set of terms, to avoid calling <see cref="IndexReader.DocFreq"/> too much,
    /// or at all.  Since you're trying to maximize a tf*idf score, you're probably most interested
    /// in terms with a high tf. Choosing a tf threshold even as low as two or three will radically
    /// reduce the number of terms under consideration.  Another heuristic is that terms with a
    /// high idf (i.e., a low df) tend to be longer.  So you could threshold the terms by the
    /// number of characters, not selecting anything less than, e.g., six or seven characters.
    /// With these sorts of heuristics you can usually find small set of, e.g., ten or fewer terms
    /// that do a pretty good job of characterizing a document.
    /// <para/>
    /// It all depends on what you're trying to do.  If you're trying to eek out that last percent
    /// of precision and recall regardless of computational difficulty so that you can win a TREC
    /// competition, then the techniques I mention above are useless.  But if you're trying to
    /// provide a "more like this" button on a search results page that does a decent job and has
    /// good performance, such techniques might be useful.
    /// <para/>
    /// An efficient, effective "more-like-this" query generator would be a great contribution, if
    /// anyone's interested.  I'd imagine that it would take a Reader or a String (the document's
    /// text), analyzer Analyzer, and return a set of representative terms using heuristics like those
    /// above.  The frequency and length thresholds could be parameters, etc.
    /// <para/>
    /// Doug
    /// </code>
    /// <para/>
    /// <para/>
    /// <para/>
    /// <b>Initial Usage</b>
    /// <para/>
    /// This class has lots of options to try to make it efficient and flexible.
    /// The simplest possible usage is as follows. The bold
    /// fragment is specific to this class.
    /// <para/>
    /// <code>
    /// IndexReader ir = ...
    /// IndexSearcher is = ...
    /// 
    /// MoreLikeThis mlt = new MoreLikeThis(ir);
    /// TextReader target = ... // orig source of doc you want to find similarities to
    /// Query query = mlt.Like(target);
    /// 
    /// Hits hits = is.Search(query);
    /// // now the usual iteration thru 'hits' - the only thing to watch for is to make sure
    /// //you ignore the doc if it matches your 'target' document, as it should be similar to itself
    /// </code>
    /// <para/>
    /// Thus you:
    /// <list type="bullet">
    ///     <item><description>do your normal, Lucene setup for searching,</description></item>
    ///     <item><description>create a MoreLikeThis,</description></item>
    ///     <item><description>get the text of the doc you want to find similarities to</description></item>
    ///     <item><description>then call one of the <see cref="Like(TextReader, string)"/> calls to generate a similarity query</description></item>
    ///     <item><description>call the searcher to find the similar docs</description></item>
    /// </list>
    /// <para/>
    /// <b>More Advanced Usage</b>
    /// <para/>
    /// You may want to use the setter for <see cref="FieldNames"/> so you can examine
    /// multiple fields (e.g. body and title) for similarity.
    /// <para/>
    /// <para/>
    /// Depending on the size of your index and the size and makeup of your documents you
    /// may want to call the other set methods to control how the similarity queries are
    /// generated:
    /// <list type="bullet">
    ///     <item><description><see cref="MinTermFreq"/></description></item>
    ///     <item><description><see cref="MinDocFreq"/></description></item>
    ///     <item><description><see cref="MaxDocFreq"/></description></item>
    ///     <item><description><see cref="SetMaxDocFreqPct(int)"/></description></item>
    ///     <item><description><see cref="MinWordLen"/></description></item>
    ///     <item><description><see cref="MaxWordLen"/></description></item>
    ///     <item><description><see cref="MaxQueryTerms"/></description></item>
    ///     <item><description><see cref="MaxNumTokensParsed"/></description></item>
    ///     <item><description><see cref="StopWords"/></description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Changes: Mark Harwood 29/02/04
    /// Some bugfixing, some refactoring, some optimisation.
    /// - bugfix: retrieveTerms(int docNum) was not working for indexes without a termvector -added missing code
    /// - bugfix: No significant terms being created for fields with a termvector - because
    /// was only counting one occurrence per term/field pair in calculations(ie not including frequency info from TermVector)
    /// - refactor: moved common code into isNoiseWord()
    /// - optimise: when no termvector support available - used maxNumTermsParsed to limit amount of tokenization
    /// </remarks>
    public sealed class MoreLikeThis
    {
        /// <summary>
        /// Default maximum number of tokens to parse in each example doc field that is not stored with TermVector support.
        /// </summary>
        /// <seealso cref="MaxNumTokensParsed"/>
        public static readonly int DEFAULT_MAX_NUM_TOKENS_PARSED = 5000;

        /// <summary>
        /// Ignore terms with less than this frequency in the source doc.
        /// </summary>
        /// <seealso cref="MinTermFreq"/>
        public static readonly int DEFAULT_MIN_TERM_FREQ = 2;

        /// <summary>
        /// Ignore words which do not occur in at least this many docs.
        /// </summary>
        /// <seealso cref="MinDocFreq"/>
        public static readonly int DEFAULT_MIN_DOC_FREQ = 5;

        /// <summary>
        /// Ignore words which occur in more than this many docs.
        /// </summary>
        /// <seealso cref="MaxDocFreq"/>
        /// <seealso cref="SetMaxDocFreqPct(int)"/>
        public static readonly int DEFAULT_MAX_DOC_FREQ = int.MaxValue;

        /// <summary>
        /// Boost terms in query based on score.
        /// </summary>
        /// <seealso cref="ApplyBoost"/>
        public static readonly bool DEFAULT_BOOST = false;

        /// <summary>
        /// Default field names. Null is used to specify that the field names should be looked
        /// up at runtime from the provided reader.
        /// </summary>
        public static readonly string[] DEFAULT_FIELD_NAMES = new string[] { "contents" };

        /// <summary>
        /// Ignore words less than this length or if 0 then this has no effect.
        /// </summary>
        /// <seealso cref="MinWordLen"/>
        public static readonly int DEFAULT_MIN_WORD_LENGTH = 0;

        /// <summary>
        /// Ignore words greater than this length or if 0 then this has no effect.
        /// </summary>
        /// <seealso cref="MaxWordLen"/>
        public static readonly int DEFAULT_MAX_WORD_LENGTH = 0;

        /// <summary>
        /// Default set of stopwords.
        /// If null means to allow stop words.
        /// </summary>
        /// <seealso cref="StopWords"/>
        public static readonly ISet<string> DEFAULT_STOP_WORDS = null;

        /// <summary>
        /// Return a Query with no more than this many terms.
        /// </summary>
        /// <seealso cref="BooleanQuery.MaxClauseCount"/>
        /// <seealso cref="MaxQueryTerms"/>
        public static readonly int DEFAULT_MAX_QUERY_TERMS = 25;

        // LUCNENENET NOTE: The following fields were made into auto-implemented properties:
        // analyzer, minTermFreq, minDocFreq, maxDocFreq, boost, 
        // fieldNames, maxNumTokensParsed, minWordLen, maxWordLen,
        // maxQueryTerms, similarity

        /// <summary>
        /// <see cref="IndexReader"/> to use
        /// </summary>
        private readonly IndexReader ir;

        /// <summary>
        /// Boost factor to use when boosting the terms
        /// </summary>
        private float boostFactor = 1;

        /// <summary>
        /// Gets or Sets the boost factor used when boosting terms
        /// </summary>
        public float BoostFactor
        {
            get => boostFactor;
            set => this.boostFactor = value;
        }


        /// <summary>
        /// Constructor requiring an <see cref="IndexReader"/>.
        /// </summary>
        public MoreLikeThis(IndexReader ir)
            : this(ir, new DefaultSimilarity())
        {
        }

        public MoreLikeThis(IndexReader ir, TFIDFSimilarity sim)
        {
            this.ir = ir;
            this.Similarity = sim;

            // LUCENENET specific: Set Defaults
            StopWords = DEFAULT_STOP_WORDS;
            MinTermFreq = DEFAULT_MIN_TERM_FREQ;
            MinDocFreq = DEFAULT_MIN_DOC_FREQ;
            MaxDocFreq = DEFAULT_MAX_DOC_FREQ;
            ApplyBoost = DEFAULT_BOOST;
            FieldNames = DEFAULT_FIELD_NAMES;
            MaxNumTokensParsed = DEFAULT_MAX_NUM_TOKENS_PARSED;
            MinWordLen = DEFAULT_MIN_WORD_LENGTH;
            MaxWordLen = DEFAULT_MAX_WORD_LENGTH;
            MaxQueryTerms = DEFAULT_MAX_QUERY_TERMS;
        }

        /// <summary>
        /// For idf() calculations.
        /// </summary>
        public TFIDFSimilarity Similarity { get; set; }


        /// <summary>
        /// Gets or Sets an analyzer that will be used to parse source doc with. The default analyzer
        /// is not set. An analyzer is not required for generating a query with the 
        /// <see cref="Like(int)"/> method, all other 'like' methods require an analyzer.
        /// </summary>
        public Analyzer Analyzer { get; set; }


        /// <summary>
        /// Gets or Sets the frequency below which terms will be ignored in the source doc. The default
        /// frequency is the <see cref="DEFAULT_MIN_TERM_FREQ"/>.
        /// </summary>
        public int MinTermFreq { get; set; }


        /// <summary>
        /// Gets or Sets the frequency at which words will be ignored which do not occur in at least this
        /// many docs. The default frequency is <see cref="DEFAULT_MIN_DOC_FREQ"/>.
        /// </summary>
        public int MinDocFreq { get; set; }


        /// <summary>
        /// Gets or Sets the maximum frequency in which words may still appear.
        /// Words that appear in more than this many docs will be ignored. The default frequency is
        /// <see cref="DEFAULT_MAX_DOC_FREQ"/>.
        /// </summary>
        public int MaxDocFreq { get; set; }


        /// <summary>
        /// Set the maximum percentage in which words may still appear. Words that appear
        /// in more than this many percent of all docs will be ignored.
        /// </summary>
        /// <param name="maxPercentage"> the maximum percentage of documents (0-100) that a term may appear
        /// in to be still considered relevant </param>
        public void SetMaxDocFreqPct(int maxPercentage)
        {
            this.MaxDocFreq = maxPercentage * ir.NumDocs / 100;
        }


        /// <summary>
        /// Gets or Sets whether to boost terms in query based on "score" or not. The default is
        /// <see cref="DEFAULT_BOOST"/>.
        /// </summary>
        public bool ApplyBoost { get; set; }


        /// <summary>
        /// Gets or Sets the field names that will be used when generating the 'More Like This' query.
        /// The default field names that will be used is <see cref="DEFAULT_FIELD_NAMES"/>. 
        /// Set this to null for the field names to be determined at runtime from the <see cref="IndexReader"/>
        /// provided in the constructor.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public string[] FieldNames { get; set; }


        /// <summary>
        /// Gets or Sets the minimum word length below which words will be ignored. Set this to 0 for no
        /// minimum word length. The default is <see cref="DEFAULT_MIN_WORD_LENGTH"/>.
        /// </summary>
        public int MinWordLen { get; set; }


        /// <summary>
        /// Gets or Sets the maximum word length above which words will be ignored. Set this to 0 for no
        /// maximum word length. The default is <see cref="DEFAULT_MAX_WORD_LENGTH"/>.
        /// </summary>
        public int MaxWordLen { get; set; }


        /// <summary>
        /// Gets or Sets the set of stopwords.
        /// Any word in this set is considered "uninteresting" and ignored.
        /// Even if your <see cref="Analysis.Analyzer"/> allows stopwords, you might want to tell the <see cref="MoreLikeThis"/> code to ignore them, as
        /// for the purposes of document similarity it seems reasonable to assume that "a stop word is never interesting".
        /// </summary>
        public ISet<string> StopWords { get; set; }

        /// <summary>
        /// Gets or Sets the maximum number of query terms that will be included in any generated query.
        /// The default is <see cref="DEFAULT_MAX_QUERY_TERMS"/>.
        /// </summary>
        public int MaxQueryTerms { get; set; }


        /// <returns> Gets or Sets the maximum number of tokens to parse in each example doc field that is not stored with TermVector support </returns>
        /// <seealso cref="DEFAULT_MAX_NUM_TOKENS_PARSED"/>
        public int MaxNumTokensParsed { get; set; }



        /// <summary>
        /// Return a query that will return docs like the passed lucene document ID.
        /// </summary>
        /// <param name="docNum"> the documentID of the lucene doc to generate the 'More Like This" query for. </param>
        /// <returns> a query that will return docs like the passed lucene document ID. </returns>
        public Query Like(int docNum)
        {
            if (FieldNames is null)
            {
                // gather list of valid fields from lucene
                ICollection<string> fields = MultiFields.GetIndexedFields(ir);
                FieldNames = fields.ToArray();
            }

            return CreateQuery(RetrieveTerms(docNum));
        }

        /// <summary>
        /// Return a query that will return docs like the passed <see cref="TextReader"/>.
        /// </summary>
        /// <returns> a query that will return docs like the passed <see cref="TextReader"/>. </returns>
        public Query Like(TextReader r, string fieldName)
        {
            return CreateQuery(RetrieveTerms(r, fieldName));
        }

        /// <summary>
        /// Create the More like query from a <see cref="PriorityQueue{T}"/>
        /// </summary>
        // LUCENENET: Factored out the object[] to avoid boxing
        private Query CreateQuery(PriorityQueue<ScoreTerm> q)
        {
            BooleanQuery query = new BooleanQuery();
            ScoreTerm cur;
            int qterms = 0;
            float bestScore = 0;

            while ((cur = q.Pop()) != null)
            {
                var tq = new TermQuery(new Term(cur.TopField, cur.Word));

                if (ApplyBoost)
                {
                    if (qterms == 0)
                    {
                        bestScore = cur.Score;
                    }
                    float myScore = cur.Score;

                    tq.Boost = boostFactor * myScore / bestScore;
                }

                try
                {
                    query.Add(tq, Occur.SHOULD);
                }
                catch (BooleanQuery.TooManyClausesException)
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
        /// Create a <see cref="PriorityQueue{T}"/> from a word-&gt;tf map.
        /// </summary>
        /// <param name="words"> a map of words keyed on the word(<see cref="string"/>) with <see cref="Int32"/> objects as the values. </param>
        /// <exception cref="IOException"/>
        // LUCENENET: Factored out the object[] to avoid boxing
        private PriorityQueue<ScoreTerm> CreateQueue(IDictionary<string, Int32> words)
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
                res.InsertWithOverflow(new ScoreTerm(word, topField, score, idf, docFreq, tf)); // freq in all docs -  idf -  overall score -  the top field -  the word
            }
            return res;
        }

        /// <summary>
        /// Describe the parameters that control how the "more like this" query is formed.
        /// </summary>
        public string DescribeParams()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('\t').Append("maxQueryTerms  : ").Append(MaxQueryTerms).Append('\n');
            sb.Append('\t').Append("minWordLen     : ").Append(MinWordLen).Append('\n');
            sb.Append('\t').Append("maxWordLen     : ").Append(MaxWordLen).Append('\n');
            sb.Append('\t').Append("fieldNames     : ");
            string delim = "";
            foreach (string fieldName in FieldNames)
            {
                sb.Append(delim).Append(fieldName);
                delim = ", ";
            }
            sb.Append('\n');
            sb.Append('\t').Append("boost          : ").Append(ApplyBoost).Append('\n');
            sb.Append('\t').Append("minTermFreq    : ").Append(MinTermFreq).Append('\n');
            sb.Append('\t').Append("minDocFreq     : ").Append(MinDocFreq).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Find words for a more-like-this query former.
        /// </summary>
        /// <param name="docNum"> the id of the lucene document from which to find terms </param>
        /// <exception cref="IOException"/>
        public PriorityQueue<ScoreTerm> RetrieveTerms(int docNum)
        {
            IDictionary<string, Int32> termFreqMap = new Dictionary<string, Int32>();
            foreach (string fieldName in FieldNames)
            {
                Fields vectors = ir.GetTermVectors(docNum);
                Terms vector;
                if (vectors != null)
                {
                    vector = vectors.GetTerms(fieldName);
                }
                else
                {
                    vector = null;
                }

                // field does not store term vector info
                if (vector is null)
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

            return CreateQueue(termFreqMap);
        }

        /// <summary>
        /// Adds terms and frequencies found in vector into the <see cref="T:IDictionary{string, Int32}"/> <paramref name="termFreqMap"/>
        /// </summary>
        /// <param name="termFreqMap"> a <see cref="T:IDictionary{string, Int32}"/> of terms and their frequencies </param>
        /// <param name="vector"> List of terms and their frequencies for a doc/field </param>
        private void AddTermFrequencies(IDictionary<string, Int32> termFreqMap, Terms vector)
        {
            var termsEnum = vector.GetEnumerator();
            var spare = new CharsRef();
            BytesRef text;
            while (termsEnum.MoveNext())
            {
                text = termsEnum.Term;
                UnicodeUtil.UTF8toUTF16(text, spare);
                var term = spare.ToString();
                if (IsNoiseWord(term))
                {
                    continue;
                }
                var freq = (int)termsEnum.TotalTermFreq;

                // increment frequency
                if (!termFreqMap.TryGetValue(term, out Int32 cnt))
                {
                    cnt = new Int32();
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
        /// Adds term frequencies found by tokenizing text from reader into the <see cref="T:IDictionary{string, Int}"/> words
        /// </summary>
        /// <param name="r"> a source of text to be tokenized </param>
        /// <param name="termFreqMap"> a <see cref="T:IDictionary{string, Int}"/> of terms and their frequencies </param>
        /// <param name="fieldName"> Used by analyzer for any special per-field analysis </param>
        private void AddTermFrequencies(TextReader r, IDictionary<string, Int32> termFreqMap, string fieldName)
        {
            if (Analyzer is null)
            {
                throw UnsupportedOperationException.Create("To use MoreLikeThis without " +
                    "term vectors, you must provide an Analyzer");
            }
            var ts = Analyzer.GetTokenStream(fieldName, r);
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
                    if (!termFreqMap.TryGetValue(word, out Int32 cnt))
                    {
                        termFreqMap[word] = new Int32();
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
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        /// <summary>
        /// determines if the passed term is likely to be of interest in "more like" comparisons
        /// </summary>
        /// <param name="term"> The word being considered </param>
        /// <returns> <c>true</c> if should be ignored, <c>false</c> if should be used in further analysis </returns>
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
        /// The result is a priority queue of <see cref="ScoreTerm"/> objects with one entry for <b>every word</b> in the document.
        /// Each object has 6 properties.
        /// The properties are:
        /// <list type="bullet">
        ///     <item><description>The <see cref="ScoreTerm.Word"/> (<see cref="string"/>)</description></item>
        ///     <item><description>The <see cref="ScoreTerm.TopField"/> that this word comes from (<see cref="string"/>)</description></item>
        ///     <item><description>The <see cref="ScoreTerm.Score"/> for this word (<see cref="float"/>)</description></item>
        ///     <item><description>The <see cref="ScoreTerm.Idf"/> value (<see cref="float"/>)</description></item>
        ///     <item><description>The <see cref="ScoreTerm.DocFreq"/> (frequency of this word in the index (<see cref="int"/>))</description></item>
        ///     <item><description>The <see cref="ScoreTerm.Tf"/> (frequency of this word in the source document (<see cref="int"/>))</description></item>
        /// </list>
        /// This is a somewhat "advanced" routine, and in general only the <see cref="ScoreTerm.Word"/> is of interest.
        /// This method is exposed so that you can identify the "interesting words" in a document.
        /// For an easier method to call see <see cref="RetrieveInterestingTerms(TextReader, string)"/>.
        /// </summary>
        /// <param name="r"> the reader that has the content of the document </param>
        /// <param name="fieldName"> field passed to the analyzer to use when analyzing the content </param>
        /// <returns> the most interesting words in the document ordered by score, with the highest scoring, or best entry, first </returns>
        /// <exception cref="IOException"/>
        /// <seealso cref="RetrieveInterestingTerms(TextReader, string)"/>
        // LUCENENET: Factored out the object[] to avoid boxing
        public PriorityQueue<ScoreTerm> RetrieveTerms(TextReader r, string fieldName)
        {
            IDictionary<string, Int32> words = new Dictionary<string, Int32>();
            AddTermFrequencies(r, words, fieldName);
            return CreateQueue(words);
        }

        /// <seealso cref="RetrieveInterestingTerms(TextReader, string)"/>
        public string[] RetrieveInterestingTerms(int docNum)
        {
            var al = new JCG.List<string>(MaxQueryTerms);
            var pq = RetrieveTerms(docNum);
            ScoreTerm scoreTerm;
            int lim = MaxQueryTerms; // have to be careful, retrieveTerms returns all words but that's probably not useful to our caller...
            // we just want to return the top words
            while (((scoreTerm = pq.Pop()) != null) && lim-- > 0)
            {
                al.Add(scoreTerm.Word); // the interesting word
            }
            return al.ToArray();
        }

        /// <summary>
        /// Convenience routine to make it easy to return the most interesting words in a document.
        /// More advanced users will call <see cref="RetrieveTerms(TextReader, string)"/> directly.
        /// </summary>
        /// <param name="r"> the source document </param>
        /// <param name="fieldName"> field passed to analyzer to use when analyzing the content </param>
        /// <returns> the most interesting words in the document </returns>
        /// <seealso cref="RetrieveTerms(TextReader, string)"/>
        /// <seealso cref="MaxQueryTerms"/>
        // LUCENENET: Factored out the object[] to avoid boxing
        public string[] RetrieveInterestingTerms(TextReader r, string fieldName)
        {
            var al = new JCG.List<string>(MaxQueryTerms);
            PriorityQueue<ScoreTerm> pq = RetrieveTerms(r, fieldName);
            ScoreTerm scoreTerm;
            int lim = MaxQueryTerms; // have to be careful, retrieveTerms returns all words but that's probably not useful to our caller...
            // we just want to return the top words
            while (((scoreTerm = pq.Pop()) != null) && lim-- > 0)
            {
                al.Add(scoreTerm.Word); // the interesting word
            }
            return al.ToArray();
        }

        /// <summary>
        /// <see cref="PriorityQueue{T}"/> that orders words by score.
        /// </summary>
        private class FreqQ : PriorityQueue<ScoreTerm>
        {
            internal FreqQ(int s)
                : base(s)
            {
            }

            // LUCENENET: Factored out the object[] to avoid boxing
            protected internal override bool LessThan(ScoreTerm aa, ScoreTerm bb)
            {
                return aa.Score > bb.Score;
            }
        }

        /// <summary>
        /// Use for frequencies and to avoid renewing <see cref="int"/>s.
        /// <para/>
        /// NOTE: This was Int in Lucene
        /// </summary>
        private class Int32
        {
            internal int x;

            internal Int32()
            {
                x = 1;
            }
        }
    }

    /// <summary>
    /// An "interesting word" and related top field, score and frequency information.
    /// </summary>
    // LUCENENET specific - added this class to use as the PriorityQueue element to avoid
    // boxing with value types in an object[] array. This uses the same name as the one
    // from Lucene 8.2.0 for forward compatibility, however in that version it is internal.
    public class ScoreTerm
    {
        internal ScoreTerm(string word, string topField, float score, float idf, int docFreq, int tf)
        {
            Word = word ?? throw new ArgumentNullException(nameof(word));
            TopField = topField ?? throw new ArgumentNullException(nameof(topField));
            Score = score;
            Idf = idf;
            DocFreq = docFreq;
            Tf = tf;
        }

        /// <summary>
        /// Gets the word.
        /// </summary>
        public string Word { get; private set; }

        /// <summary>
        /// Gets the top field that this word comes from.
        /// </summary>
        public string TopField { get; private set; }

        /// <summary>
        /// Gets the score for this word (<see cref="float"/>).
        /// </summary>
        public float Score { get; private set; }

        /// <summary>
        /// Gets the inverse document frequency (IDF) value (<see cref="float"/>).
        /// </summary>
        public float Idf { get; private set; }

        /// <summary>
        /// Gets the frequency of this word in the index (<see cref="int"/>).
        /// </summary>
        public int DocFreq { get; private set; }

        /// <summary>
        /// Gets the frequency of this word in the source document (<see cref="int"/>).
        /// </summary>
        public int Tf { get; private set; }
    }
}