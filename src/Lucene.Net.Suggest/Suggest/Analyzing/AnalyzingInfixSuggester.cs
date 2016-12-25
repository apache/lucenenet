using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Ngram;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Sorter;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Search.Suggest.Analyzing
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

    // TODO:
    //   - a PostingsFormat that stores super-high-freq terms as
    //     a bitset should be a win for the prefix terms?
    //     (LUCENE-5052)
    //   - we could offer a better integration with
    //     DocumentDictionary and NRT?  so that your suggester
    //     "automatically" keeps in sync w/ your index

    /// <summary>
    /// Analyzes the input text and then suggests matches based
    ///  on prefix matches to any tokens in the indexed text.
    ///  This also highlights the tokens that match.
    /// 
    ///  <para>This suggester supports payloads.  Matches are sorted only
    ///  by the suggest weight; it would be nice to support
    ///  blended score + weight sort in the future.  This means
    ///  this suggester best applies when there is a strong
    ///  a-priori ranking of all the suggestions.
    /// 
    /// </para>
    ///  <para>This suggester supports contexts, however the
    ///  contexts must be valid utf8 (arbitrary binary terms will
    ///  not work).
    /// 
    /// @lucene.experimental 
    /// </para>
    /// </summary>

    public class AnalyzingInfixSuggester : Lookup, IDisposable
    {

        /// <summary>
        /// Field name used for the indexed text. </summary>
        protected internal const string TEXT_FIELD_NAME = "text";

        /// <summary>
        /// Field name used for the indexed text, as a
        /// <see cref="StringField"/>, for exact lookup. 
        /// </summary>
        protected internal const string EXACT_TEXT_FIELD_NAME = "exacttext";

        /// <summary>
        /// Field name used for the indexed context, as a
        /// <see cref="StringField"/> and a <see cref="SortedSetDocValuesField"/>, for filtering. 
        /// </summary>
        protected internal const string CONTEXTS_FIELD_NAME = "contexts";

        /// <summary>
        /// Analyzer used at search time </summary>
        protected internal readonly Analyzer queryAnalyzer;
        /// <summary>
        /// Analyzer used at index time </summary>
        protected internal readonly Analyzer indexAnalyzer;
        internal readonly LuceneVersion matchVersion;
        private readonly Directory dir;
        internal readonly int minPrefixChars;

        /// <summary>
        /// Used for ongoing NRT additions/updates. </summary>
        private IndexWriter writer;

        /// <summary>
        /// <see cref="IndexSearcher"/> used for lookups. </summary>
        protected internal SearcherManager searcherMgr;

        /// <summary>
        /// Default minimum number of leading characters before
        ///  PrefixQuery is used (4). 
        /// </summary>
        public const int DEFAULT_MIN_PREFIX_CHARS = 4;

        /// <summary>
        /// How we sort the postings and search results. </summary>
        private static readonly Sort SORT = new Sort(new SortField("weight", SortFieldType.LONG, true));

        /// <summary>
        /// Create a new instance, loading from a previously built
        /// <see cref="AnalyzingInfixSuggester"/> directory, if it exists. 
        /// This directory must be
        /// private to the infix suggester (i.e., not an external
        /// Lucene index).  Note that <see cref="Dispose()"/>
        /// will also dispose the provided directory. 
        /// </summary>
        public AnalyzingInfixSuggester(LuceneVersion matchVersion, Directory dir, Analyzer analyzer)
            : this(matchVersion, dir, analyzer, analyzer, DEFAULT_MIN_PREFIX_CHARS)
        {
        }

        /// <summary>
        /// Create a new instance, loading from a previously built
        /// <see cref="AnalyzingInfixSuggester"/> directory, if it exists.  This directory must be
        /// private to the infix suggester (i.e., not an external
        /// Lucene index).  Note that <see cref="Dispose()"/>
        /// will also dispose the provided directory.
        /// </summary>
        ///  <param name="minPrefixChars"> Minimum number of leading characters
        ///     before <see cref="PrefixQuery"/> is used (default 4).
        ///     Prefixes shorter than this are indexed as character
        ///     ngrams (increasing index size but making lookups
        ///     faster). </param>
        public AnalyzingInfixSuggester(LuceneVersion matchVersion, Directory dir, Analyzer indexAnalyzer, 
            Analyzer queryAnalyzer, int minPrefixChars)
        {

            if (minPrefixChars < 0)
            {
                throw new System.ArgumentException("minPrefixChars must be >= 0; got: " + minPrefixChars);
            }

            this.queryAnalyzer = queryAnalyzer;
            this.indexAnalyzer = indexAnalyzer;
            this.matchVersion = matchVersion;
            this.dir = dir;
            this.minPrefixChars = minPrefixChars;

            if (DirectoryReader.IndexExists(dir))
            {
                // Already built; open it:
                writer = new IndexWriter(dir, GetIndexWriterConfig(matchVersion, GramAnalyzer, OpenMode.APPEND));
                searcherMgr = new SearcherManager(writer, true, null);
            }
        }

        /// <summary>
        /// Override this to customize index settings, e.g. which
        /// codec to use. 
        /// </summary>
        protected internal virtual IndexWriterConfig GetIndexWriterConfig(LuceneVersion matchVersion, 
            Analyzer indexAnalyzer, OpenMode openMode)
        {
            IndexWriterConfig iwc = new IndexWriterConfig(matchVersion, indexAnalyzer);
            iwc.SetCodec(new Lucene46Codec());
            iwc.SetOpenMode(openMode);

            // This way all merged segments will be sorted at
            // merge time, allow for per-segment early termination
            // when those segments are searched:
            iwc.SetMergePolicy(new SortingMergePolicy(iwc.MergePolicy, SORT));

            return iwc;
        }

        /// <summary>
        /// Subclass can override to choose a specific 
        /// <see cref="Directory"/> implementation. 
        /// </summary>
        protected internal virtual Directory GetDirectory(DirectoryInfo path)
        {
            return FSDirectory.Open(path);
        }

        public override void Build(IInputIterator iter)
        {

            if (searcherMgr != null)
            {
                searcherMgr.Dispose();
                searcherMgr = null;
            }

            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }

            AtomicReader r = null;
            bool success = false;
            try
            {
                // First pass: build a temporary normal Lucene index,
                // just indexing the suggestions as they iterate:
                writer = new IndexWriter(dir, GetIndexWriterConfig(matchVersion, GramAnalyzer, OpenMode.CREATE));
                //long t0 = System.nanoTime();

                // TODO: use threads?
                BytesRef text;
                while ((text = iter.Next()) != null)
                {
                    BytesRef payload;
                    if (iter.HasPayloads)
                    {
                        payload = iter.Payload;
                    }
                    else
                    {
                        payload = null;
                    }

                    Add(text, iter.Contexts, iter.Weight, payload);
                }

                //System.out.println("initial indexing time: " + ((System.nanoTime()-t0)/1000000) + " msec");

                searcherMgr = new SearcherManager(writer, true, null);
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(r);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(writer, r);
                    writer = null;
                }
            }
        }

        private Analyzer GramAnalyzer
        {
            get
            {
                return new AnalyzerWrapperAnonymousInnerClassHelper(this, Analyzer.PER_FIELD_REUSE_STRATEGY);
            }
        }

        private class AnalyzerWrapperAnonymousInnerClassHelper : AnalyzerWrapper
        {
            private readonly AnalyzingInfixSuggester outerInstance;

            public AnalyzerWrapperAnonymousInnerClassHelper(AnalyzingInfixSuggester outerInstance, Analyzer.ReuseStrategy reuseStrategy)
                : base(reuseStrategy)
            {
                this.outerInstance = outerInstance;
            }

            protected override Analyzer GetWrappedAnalyzer(string fieldName)
            {
                return outerInstance.indexAnalyzer;
            }

            protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
            {
                if (fieldName.Equals("textgrams") && outerInstance.minPrefixChars > 0)
                {
                    return new TokenStreamComponents(components.Tokenizer, 
                        new EdgeNGramTokenFilter(
                            outerInstance.matchVersion, 
                            components.TokenStream, 
                            1, 
                            outerInstance.minPrefixChars));
                }
                else
                {
                    return components;
                }
            }
        }

        /// <summary>
        /// Adds a new suggestion.  Be sure to use <see cref="Update"/>
        /// instead if you want to replace a previous suggestion.
        /// After adding or updating a batch of new suggestions,
        /// you must call <see cref="Refresh()"/> in the end in order to
        /// see the suggestions in <see cref="DoLookup"/> 
        /// </summary>
        public virtual void Add(BytesRef text, IEnumerable<BytesRef> contexts, long weight, BytesRef payload)
        {
            writer.AddDocument(BuildDocument(text, contexts, weight, payload));
        }

        /// <summary>
        /// Updates a previous suggestion, matching the exact same
        /// text as before.  Use this to change the weight or
        /// payload of an already added suggstion.  If you know
        /// this text is not already present you can use <see cref="Add"/> 
        /// instead.  After adding or updating a batch of
        /// new suggestions, you must call <see cref="Refresh()"/> in the
        /// end in order to see the suggestions in <see cref="DoLookup"/> 
        /// </summary>
        public virtual void Update(BytesRef text, IEnumerable<BytesRef> contexts, long weight, BytesRef payload)
        {
            writer.UpdateDocument(new Term(EXACT_TEXT_FIELD_NAME, text.Utf8ToString()), BuildDocument(text, contexts, weight, payload));
        }

        private Document BuildDocument(BytesRef text, IEnumerable<BytesRef> contexts, long weight, BytesRef payload)
        {
            string textString = text.Utf8ToString();
            var ft = TextFieldType;
            var doc = new Document
		{
		    new Field(TEXT_FIELD_NAME, textString, ft),
		    new Field("textgrams", textString, ft),
		    new StringField(EXACT_TEXT_FIELD_NAME, textString, Field.Store.NO),
		    new BinaryDocValuesField(TEXT_FIELD_NAME, text),
		    new NumericDocValuesField("weight", weight)
		};
            if (payload != null)
            {
                doc.Add(new BinaryDocValuesField("payloads", payload));
            }
            if (contexts != null)
            {
                foreach (BytesRef context in contexts)
                {
                    // TODO: if we had a BinaryTermField we could fix
                    // this "must be valid ut8f" limitation:
                    doc.Add(new StringField(CONTEXTS_FIELD_NAME, context.Utf8ToString(), Field.Store.NO));
                    doc.Add(new SortedSetDocValuesField(CONTEXTS_FIELD_NAME, context));
                }
            }
            return doc;
        }

        /// <summary>
        /// Reopens the underlying searcher; it's best to "batch
        /// up" many additions/updates, and then call refresh
        /// once in the end. 
        /// </summary>
        public virtual void Refresh()
        {
            searcherMgr.MaybeRefreshBlocking();
        }

        /// <summary>
        /// Subclass can override this method to change the field type of the text field
        /// e.g. to change the index options
        /// </summary>
        protected internal virtual FieldType TextFieldType
        {
            get
            {
                var ft = new FieldType(TextField.TYPE_NOT_STORED);
                ft.IndexOptions = IndexOptions.DOCS_ONLY;
                ft.OmitNorms = true;

                return ft;
            }
        }

        public override List<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            return DoLookup(key, contexts, num, true, true);
        }

        /// <summary>
        /// Lookup, without any context.
        /// </summary>
        public virtual List<LookupResult> DoLookup(string key, int num, bool allTermsRequired, bool doHighlight)
        {
            return DoLookup(key, null, num, allTermsRequired, doHighlight);
        }

        /// <summary>
        /// This is called if the last token isn't ended
        /// (e.g. user did not type a space after it).  Return an
        /// appropriate <see cref="Query"/> clause to add to the <see cref="BooleanQuery"/>. 
        /// </summary>
        protected internal virtual Query GetLastTokenQuery(string token)
        {
            if (token.Length < minPrefixChars)
            {
                // The leading ngram was directly indexed:
                return new TermQuery(new Term("textgrams", token));
            }

            return new PrefixQuery(new Term(TEXT_FIELD_NAME, token));
        }

        /// <summary>
        /// Retrieve suggestions, specifying whether all terms
        ///  must match (<paramref name="allTermsRequired"/>) and whether the hits
        ///  should be highlighted (<paramref name="doHighlight"/>). 
        /// </summary>
        public virtual List<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, int num, bool allTermsRequired, bool doHighlight)
        {

            if (searcherMgr == null)
            {
                throw new InvalidOperationException("suggester was not built");
            }

            Occur occur;
            if (allTermsRequired)
            {
                occur = Occur.MUST;
            }
            else
            {
                occur = Occur.SHOULD;
            }

            TokenStream ts = null;
            BooleanQuery query;
            var matchedTokens = new HashSet<string>();
            string prefixToken = null;

            try
            {
                ts = queryAnalyzer.TokenStream("", new StringReader(key));

                //long t0 = System.currentTimeMillis();
                ts.Reset();
                var termAtt = ts.AddAttribute<ICharTermAttribute>();
                var offsetAtt = ts.AddAttribute<IOffsetAttribute>();
                string lastToken = null;
                query = new BooleanQuery();
                int maxEndOffset = -1;
                matchedTokens = new HashSet<string>();
                while (ts.IncrementToken())
                {
                    if (lastToken != null)
                    {
                        matchedTokens.Add(lastToken);
                        query.Add(new TermQuery(new Term(TEXT_FIELD_NAME, lastToken)), occur);
                    }
                    lastToken = termAtt.ToString();
                    if (lastToken != null)
                    {
                        maxEndOffset = Math.Max(maxEndOffset, offsetAtt.EndOffset);
                    }
                }
                ts.End();

                if (lastToken != null)
                {
                    Query lastQuery;
                    if (maxEndOffset == offsetAtt.EndOffset)
                    {
                        // Use PrefixQuery (or the ngram equivalent) when
                        // there was no trailing discarded chars in the
                        // string (e.g. whitespace), so that if query does
                        // not end with a space we show prefix matches for
                        // that token:
                        lastQuery = GetLastTokenQuery(lastToken);
                        prefixToken = lastToken;
                    }
                    else
                    {
                        // Use TermQuery for an exact match if there were
                        // trailing discarded chars (e.g. whitespace), so
                        // that if query ends with a space we only show
                        // exact matches for that term:
                        matchedTokens.Add(lastToken);
                        lastQuery = new TermQuery(new Term(TEXT_FIELD_NAME, lastToken));
                    }
                    if (lastQuery != null)
                    {
                        query.Add(lastQuery, occur);
                    }
                }

                if (contexts != null)
                {
                    BooleanQuery sub = new BooleanQuery();
                    query.Add(sub, Occur.MUST);
                    foreach (BytesRef context in contexts)
                    {
                        // NOTE: we "should" wrap this in
                        // ConstantScoreQuery, or maybe send this as a
                        // Filter instead to search, but since all of
                        // these are MUST'd, the change to the score won't
                        // affect the overall ranking.  Since we indexed
                        // as DOCS_ONLY, the perf should be the same
                        // either way (no freq int[] blocks to decode):

                        // TODO: if we had a BinaryTermField we could fix
                        // this "must be valid ut8f" limitation:
                        sub.Add(new TermQuery(new Term(CONTEXTS_FIELD_NAME, context.Utf8ToString())), Occur.SHOULD);
                    }
                }
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(ts);
            }

            // TODO: we could allow blended sort here, combining
            // weight w/ score.  Now we ignore score and sort only
            // by weight:

            Query finalQuery = FinishQuery(query, allTermsRequired);

            //System.out.println("finalQuery=" + query);

            // Sort by weight, descending:
            TopFieldCollector c = TopFieldCollector.Create(SORT, num, true, false, false, false);

            // We sorted postings by weight during indexing, so we
            // only retrieve the first num hits now:
            Collector c2 = new EarlyTerminatingSortingCollector(c, SORT, num);
            IndexSearcher searcher = searcherMgr.Acquire();
            List<LookupResult> results = null;
            try
            {
                //System.out.println("got searcher=" + searcher);
                searcher.Search(finalQuery, c2);

                TopFieldDocs hits = (TopFieldDocs)c.TopDocs();

                // Slower way if postings are not pre-sorted by weight:
                // hits = searcher.search(query, null, num, SORT);
                results = CreateResults(searcher, hits, num, key, doHighlight, matchedTokens, prefixToken);
            }
            finally
            {
                searcherMgr.Release(searcher);
            }

            //System.out.println((System.currentTimeMillis() - t0) + " msec for infix suggest");
            //System.out.println(results);

            return results;
        }

        /// <summary>
        /// Create the results based on the search hits.
        /// Can be overridden by subclass to add particular behavior (e.g. weight transformation) </summary>
        /// <exception cref="System.IO.IOException"> If there are problems reading fields from the underlying Lucene index. </exception>
        protected internal virtual List<LookupResult> CreateResults(IndexSearcher searcher, TopFieldDocs hits, int num, string charSequence, bool doHighlight, IEnumerable<string> matchedTokens, string prefixToken)
        {

            BinaryDocValues textDV = MultiDocValues.GetBinaryValues(searcher.IndexReader, TEXT_FIELD_NAME);

            // This will just be null if app didn't pass payloads to build():
            // TODO: maybe just stored fields?  they compress...
            BinaryDocValues payloadsDV = MultiDocValues.GetBinaryValues(searcher.IndexReader, "payloads");
            IList<AtomicReaderContext> leaves = searcher.IndexReader.Leaves;
            List<LookupResult> results = new List<LookupResult>();
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                FieldDoc fd = (FieldDoc)hits.ScoreDocs[i];
                textDV.Get(fd.Doc, scratch);
                string text = scratch.Utf8ToString();
                long score = (long)fd.Fields[0];

                BytesRef payload;
                if (payloadsDV != null)
                {
                    payload = new BytesRef();
                    payloadsDV.Get(fd.Doc, payload);
                }
                else
                {
                    payload = null;
                }

                // Must look up sorted-set by segment:
                int segment = ReaderUtil.SubIndex(fd.Doc, leaves);
                SortedSetDocValues contextsDV = leaves[segment].AtomicReader.GetSortedSetDocValues(CONTEXTS_FIELD_NAME);
                HashSet<BytesRef> contexts;
                if (contextsDV != null)
                {
                    contexts = new HashSet<BytesRef>();
                    contextsDV.SetDocument(fd.Doc - leaves[segment].DocBase);
                    long ord;
                    while ((ord = contextsDV.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        BytesRef context = new BytesRef();
                        contextsDV.LookupOrd(ord, context);
                        contexts.Add(context);
                    }
                }
                else
                {
                    contexts = null;
                }

                LookupResult result;

                if (doHighlight)
                {
                    object highlightKey = Highlight(text, matchedTokens, prefixToken);
                    result = new LookupResult(highlightKey.ToString(), highlightKey, score, payload, contexts);
                }
                else
                {
                    result = new LookupResult(text, score, payload, contexts);
                }

                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Subclass can override this to tweak the Query before
        /// searching. 
        /// </summary>
        protected internal virtual Query FinishQuery(BooleanQuery bq, bool allTermsRequired)
        {
            return bq;
        }

        /// <summary>
        /// Override this method to customize the Object
        /// representing a single highlighted suggestions; the
        /// result is set on each <see cref="Lookup.LookupResult.highlightKey"/>
        /// member. 
        /// </summary>
        protected internal virtual object Highlight(string text, IEnumerable<string> matchedTokens, string prefixToken)
        {
            TokenStream ts = queryAnalyzer.TokenStream("text", new StringReader(text));
            try
            {
                var termAtt = ts.AddAttribute<ICharTermAttribute>();
                var offsetAtt = ts.AddAttribute<IOffsetAttribute>();
                ts.Reset();
                var sb = new StringBuilder();
                int upto = 0;
                while (ts.IncrementToken())
                {
                    string token = termAtt.ToString();
                    int startOffset = offsetAtt.StartOffset;
                    int endOffset = offsetAtt.EndOffset;
                    if (upto < startOffset)
                    {
                        AddNonMatch(sb, text.Substring(upto, startOffset - upto));
                        upto = startOffset;
                    }
                    else if (upto > startOffset)
                    {
                        continue;
                    }

                    if (matchedTokens.Contains(token))
                    {
                        // Token matches.
                        AddWholeMatch(sb, text.Substring(startOffset, endOffset - startOffset), token);
                        upto = endOffset;
                    }
                    else if (prefixToken != null && token.StartsWith(prefixToken, StringComparison.Ordinal))
                    {
                        AddPrefixMatch(sb, text.Substring(startOffset, endOffset - startOffset), token, prefixToken);
                        upto = endOffset;
                    }
                }
                ts.End();
                int endOffset2 = offsetAtt.EndOffset;
                if (upto < endOffset2)
                {
                    AddNonMatch(sb, text.Substring(upto));
                }
                return sb.ToString();
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(ts);
            }
        }

        /// <summary>
        /// Called while highlighting a single result, to append a
        /// non-matching chunk of text from the suggestion to the
        /// provided fragments list. </summary>
        /// <param name="sb"> The <see cref="StringBuilder"/> to append to </param>
        /// <param name="text"> The text chunk to add </param>
        protected internal virtual void AddNonMatch(StringBuilder sb, string text)
        {
            sb.Append(text);
        }

        /// <summary>
        /// Called while highlighting a single result, to append
        /// the whole matched token to the provided fragments list. </summary>
        /// <param name="sb"> The <see cref="StringBuilder"/> to append to </param>
        /// <param name="surface"> The surface form (original) text </param>
        /// <param name="analyzed"> The analyzed token corresponding to the surface form text </param>
        protected internal virtual void AddWholeMatch(StringBuilder sb, string surface, string analyzed)
        {
            sb.Append("<b>");
            sb.Append(surface);
            sb.Append("</b>");
        }

        /// <summary>
        /// Called while highlighting a single result, to append a
        /// matched prefix token, to the provided fragments list. </summary>
        /// <param name="sb"> The <see cref="StringBuilder"/> to append to </param>
        /// <param name="surface"> The fragment of the surface form
        ///        (indexed during <see cref="Build(IInputIterator)"/>, corresponding to
        ///        this match </param>
        /// <param name="analyzed"> The analyzed token that matched </param>
        /// <param name="prefixToken"> The prefix of the token that matched </param>
        protected internal virtual void AddPrefixMatch(StringBuilder sb, string surface, string analyzed, string prefixToken)
        {
            // TODO: apps can try to invert their analysis logic
            // here, e.g. downcase the two before checking prefix:
            sb.Append("<b>");
            sb.Append(surface.Substring(0, prefixToken.Length - 0));
            sb.Append("</b>");
            if (prefixToken.Length < surface.Length)
            {
                sb.Append(surface.Substring(prefixToken.Length));
            }
        }

        public override bool Store(DataOutput @in)
        {
            return false;
        }

        public override bool Load(DataInput @out)
        {
            return false;
        }

        public virtual void Dispose()
        {
            if (searcherMgr != null)
            {
                searcherMgr.Dispose();
                searcherMgr = null;
            }
            if (writer != null)
            {
                writer.Dispose();
                dir.Dispose();
                writer = null;
            }
        }

        public override long GetSizeInBytes()
        {
            long mem = RamUsageEstimator.ShallowSizeOf(this);
            try
            {
                if (searcherMgr != null)
                {
                    IndexSearcher searcher = searcherMgr.Acquire();
                    try
                    {
                        foreach (AtomicReaderContext context in searcher.IndexReader.Leaves)
                        {
                            AtomicReader reader = FilterAtomicReader.Unwrap(context.AtomicReader);
                            if (reader is SegmentReader)
                            {
                                mem += ((SegmentReader)context.Reader).RamBytesUsed();
                            }
                        }
                    }
                    finally
                    {
                        searcherMgr.Release(searcher);
                    }
                }
                return mem;
            }
            catch (IOException ioe)
            {
                throw new Exception(ioe.Message, ioe);
            }
        }

        public override long Count
        {
            get
            {
                IndexSearcher searcher = searcherMgr.Acquire();
                try
                {
                    return searcher.IndexReader.NumDocs;
                }
                finally
                {
                    searcherMgr.Release(searcher);
                }
            }
        }
    }
}