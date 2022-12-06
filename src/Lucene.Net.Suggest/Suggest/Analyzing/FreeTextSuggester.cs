using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Codecs;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Directory = Lucene.Net.Store.Directory;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

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

    // TODO
    //   - test w/ syns
    //   - add pruning of low-freq ngrams?

    /// <summary>
    /// Builds an ngram model from the text sent to <see cref="Build(IInputEnumerator, double)"/>
    /// and predicts based on the last grams-1 tokens in
    /// the request sent to <see cref="DoLookup(string, IEnumerable{BytesRef}, bool, int)"/>.  This tries to
    /// handle the "long tail" of suggestions for when the
    /// incoming query is a never before seen query string.
    ///
    /// <para>Likely this suggester would only be used as a
    /// fallback, when the primary suggester fails to find
    /// any suggestions.
    ///
    /// </para>
    /// <para>Note that the weight for each suggestion is unused,
    /// and the suggestions are the analyzed forms (so your
    /// analysis process should normally be very "light").
    ///
    /// </para>
    /// <para>This uses the stupid backoff language model to smooth
    /// scores across ngram models; see
    /// <a href="http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.76.1126">
    /// "Large language models in machine translation"</a> for details.
    ///
    /// </para>
    /// <para> From <see cref="DoLookup(string, IEnumerable{BytesRef}, bool, int)"/>, the key of each result is the
    /// ngram token; the value is <see cref="long.MaxValue"/> * score (fixed
    /// point, cast to long).  Divide by <see cref="long.MaxValue"/> to get
    /// the score back, which ranges from 0.0 to 1.0.
    ///
    /// <c>onlyMorePopular</c> is unused.
    ///
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class FreeTextSuggester : Lookup
    {

        /// <summary>
        /// Codec name used in the header for the saved model. </summary>
        public const string CODEC_NAME = "freetextsuggest";

        /// <summary>
        /// Initial version of the the saved model file format. </summary>
        public const int VERSION_START = 0;

        /// <summary>
        /// Current version of the the saved model file format. </summary>
        public const int VERSION_CURRENT = VERSION_START;

        /// <summary>
        /// By default we use a bigram model. </summary>
        public const int DEFAULT_GRAMS = 2;

        // In general this could vary with gram, but the
        // original paper seems to use this constant:
        /// <summary>
        /// The constant used for backoff smoothing; during
        ///  lookup, this means that if a given trigram did not
        ///  occur, and we backoff to the bigram, the overall score
        ///  will be 0.4 times what the bigram model would have
        ///  assigned.
        /// </summary>
        public const double ALPHA = 0.4;

        /// <summary>
        /// Holds 1gram, 2gram, 3gram models as a single FST. </summary>
        private FST<Int64> fst;

        /// <summary>
        /// Analyzer that will be used for analyzing suggestions at
        /// index time.
        /// </summary>
        private readonly Analyzer indexAnalyzer;

        private long totTokens;

        /// <summary>
        /// Analyzer that will be used for analyzing suggestions at
        /// query time.
        /// </summary>
        private readonly Analyzer queryAnalyzer;

        // 2 = bigram, 3 = trigram
        private readonly int grams;

        private readonly byte separator;

        /// <summary>
        /// Number of entries the lookup was built with </summary>
        private long count = 0;

        /// <summary>
        /// The default character used to join multiple tokens
        /// into a single ngram token.  The input tokens produced
        /// by the analyzer must not contain this character.
        /// </summary>
        public const byte DEFAULT_SEPARATOR = 0x1e;

        /// <summary>
        /// Instantiate, using the provided analyzer for both
        /// indexing and lookup, using bigram model by default.
        /// </summary>
        public FreeTextSuggester(Analyzer analyzer)
              : this(analyzer, analyzer, DEFAULT_GRAMS)
        {
        }

        /// <summary>
        /// Instantiate, using the provided indexing and lookup
        /// analyzers, using bigram model by default.
        /// </summary>
        public FreeTextSuggester(Analyzer indexAnalyzer, Analyzer queryAnalyzer)
              : this(indexAnalyzer, queryAnalyzer, DEFAULT_GRAMS)
        {
        }

        /// <summary>
        /// Instantiate, using the provided indexing and lookup
        /// analyzers, with the specified model (2
        /// = bigram, 3 = trigram, etc.).
        /// </summary>
        public FreeTextSuggester(Analyzer indexAnalyzer, Analyzer queryAnalyzer, int grams)
              : this(indexAnalyzer, queryAnalyzer, grams, DEFAULT_SEPARATOR)
        {
        }

        /// <summary>
        /// Instantiate, using the provided indexing and lookup
        /// analyzers, and specified model (2 = bigram, 3 =
        /// trigram ,etc.).  The <paramref name="separator"/> is passed to <see cref="ShingleFilter.SetTokenSeparator(string)"/>
        /// to join multiple
        /// tokens into a single ngram token; it must be an ascii
        /// (7-bit-clean) byte.  No input tokens should have this
        /// byte, otherwise <see cref="ArgumentException"/> is
        /// thrown.
        /// </summary>
        public FreeTextSuggester(Analyzer indexAnalyzer, Analyzer queryAnalyzer, int grams, byte separator)
        {
            this.grams = grams;
            this.indexAnalyzer = AddShingles(indexAnalyzer);
            this.queryAnalyzer = AddShingles(queryAnalyzer);
            if (grams < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(grams), "grams must be >= 1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if ((separator & 0x80) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(separator), "separator must be simple ascii character"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.separator = separator;
        }

        /// <summary>
        /// Returns byte size of the underlying FST. </summary>
        public override long GetSizeInBytes()
        {
            if (fst is null)
            {
                return 0;
            }
            return fst.GetSizeInBytes();
        }

        // LUCENENET specific - removed AnalyzingComparer because it is not in use.

        private Analyzer AddShingles(Analyzer other)
        {
            if (grams == 1)
            {
                return other;
            }
            else
            {
                // TODO: use ShingleAnalyzerWrapper?
                // Tack on ShingleFilter to the end, to generate token ngrams:
                return new AnalyzerWrapperAnonymousClass(this, other.Strategy, other);
            }
        }

        private sealed class AnalyzerWrapperAnonymousClass : AnalyzerWrapper
        {
            private readonly FreeTextSuggester outerInstance;
            private readonly Analyzer other;

            public AnalyzerWrapperAnonymousClass(FreeTextSuggester outerInstance, ReuseStrategy reuseStrategy, Analyzer other)
                : base(reuseStrategy)
            {
                this.outerInstance = outerInstance;
                this.other = other;
            }

            protected override Analyzer GetWrappedAnalyzer(string fieldName)
            {
                return other;
            }

            protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
            {
                ShingleFilter shingles = new ShingleFilter(components.TokenStream, 2, outerInstance.grams);
                shingles.SetTokenSeparator(char.ToString((char)outerInstance.separator));
                return new TokenStreamComponents(components.Tokenizer, shingles);
            }
        }

        public override void Build(IInputEnumerator enumerator)
        {
            Build(enumerator, IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
        }

        /// <summary>
        /// Build the suggest index, using up to the specified
        /// amount of temporary RAM while building.  Note that
        /// the weights for the suggestions are ignored.
        /// </summary>
        public virtual void Build(IInputEnumerator enumerator, double ramBufferSizeMB)
        {
            // LUCENENET: Added guard clause for null
            if (enumerator is null)
                throw new ArgumentNullException(nameof(enumerator));

            if (enumerator.HasPayloads)
            {
                throw new ArgumentException("this suggester doesn't support payloads");
            }
            if (enumerator.HasContexts)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }

            string prefix = this.GetType().Name;
            var directory = OfflineSorter.DefaultTempDir;

            // LUCENENET specific - using GetRandomFileName() instead of picking a random int
            DirectoryInfo tempIndexPath; // LUCENENET: IDE0059: Remove unnecessary value assignment
            while (true)
            {
                tempIndexPath = new DirectoryInfo(Path.Combine(directory, prefix + ".index." + Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));
                tempIndexPath.Create();
                if (System.IO.Directory.Exists(tempIndexPath.FullName))
                {
                    break;
                }
            }

            Directory dir = FSDirectory.Open(tempIndexPath);
            try
            {
#pragma warning disable 612, 618
                IndexWriterConfig iwc = new IndexWriterConfig(LuceneVersion.LUCENE_CURRENT, indexAnalyzer);
#pragma warning restore 612, 618
                iwc.SetOpenMode(OpenMode.CREATE);
                iwc.SetRAMBufferSizeMB(ramBufferSizeMB);
                IndexWriter writer = new IndexWriter(dir, iwc);

                var ft = new FieldType(TextField.TYPE_NOT_STORED);
                // TODO: if only we had IndexOptions.TERMS_ONLY...
                ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
                ft.OmitNorms = true;
                ft.Freeze();

                Document doc = new Document();
                Field field = new Field("body", "", ft);
                doc.Add(field);

                totTokens = 0;
                IndexReader reader = null;

                bool success = false;
                count = 0;
                try
                {
                    while (enumerator.MoveNext())
                    {
                        BytesRef surfaceForm = enumerator.Current;
                        field.SetStringValue(surfaceForm.Utf8ToString());
                        writer.AddDocument(doc);
                        count++;
                    }

                    reader = DirectoryReader.Open(writer, false);

                    Terms terms = MultiFields.GetTerms(reader, "body");
                    if (terms is null)
                    {
                        throw new ArgumentException("need at least one suggestion");
                    }

                    // Move all ngrams into an FST:
                    TermsEnum termsEnum = terms.GetEnumerator(null);

                    Outputs<Int64> outputs = PositiveInt32Outputs.Singleton;
                    Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, outputs);

                    Int32sRef scratchInts = new Int32sRef();
                    while (termsEnum.MoveNext())
                    {
                        BytesRef term = termsEnum.Term;
                        int ngramCount = CountGrams(term);
                        if (ngramCount > grams)
                        {
                            throw new ArgumentException("tokens must not contain separator byte; got token=" + term + " but gramCount=" + ngramCount + ", which is greater than expected max ngram size=" + grams);
                        }
                        if (ngramCount == 1)
                        {
                            totTokens += termsEnum.TotalTermFreq;
                        }

                        builder.Add(Lucene.Net.Util.Fst.Util.ToInt32sRef(term, scratchInts), EncodeWeight(termsEnum.TotalTermFreq));
                    }

                    fst = builder.Finish();
                    if (fst is null)
                    {
                        throw new ArgumentException("need at least one suggestion");
                    }
                    //System.out.println("FST: " + fst.getNodeCount() + " nodes");

                    /*
                    PrintWriter pw = new PrintWriter("/x/tmp/out.dot");
                    Util.toDot(fst, pw, true, true);
                    pw.close();
                    */

                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Dispose(writer, reader);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(writer, reader);
                    }
                }
            }
            finally
            {
                try
                {
                    IOUtils.Dispose(dir);
                }
                finally
                {
                    // LUCENENET specific - since we are removing the entire directory anyway,
                    // it doesn't make sense to first do a loop in order remove the files.
                    // Let the System.IO.Directory.Delete() method handle that.
                    // We also need to dispose the Directory instance first before deleting from disk.
                    try
                    {
                        System.IO.Directory.Delete(tempIndexPath.FullName, true);
                    }
                    catch (Exception e)
                    {
                        throw IllegalStateException.Create("failed to remove " + tempIndexPath, e);
                    }
                }
            }
        }

        public override bool Store(DataOutput output)
        {
            CodecUtil.WriteHeader(output, CODEC_NAME, VERSION_CURRENT);
            output.WriteVInt64(count);
            output.WriteByte(separator);
            output.WriteVInt32(grams);
            output.WriteVInt64(totTokens);
            fst.Save(output);
            return true;
        }

        public override bool Load(DataInput input)
        {
            CodecUtil.CheckHeader(input, CODEC_NAME, VERSION_START, VERSION_START);
            count = input.ReadVInt64();
            var separatorOrig = (sbyte)input.ReadByte();
            if (separatorOrig != separator)
            {
                throw IllegalStateException.Create("separator=" + separator + " is incorrect: original model was built with separator=" + separatorOrig);
            }
            int gramsOrig = input.ReadVInt32();
            if (gramsOrig != grams)
            {
                throw IllegalStateException.Create("grams=" + grams + " is incorrect: original model was built with grams=" + gramsOrig);
            }
            totTokens = input.ReadVInt64();

            fst = new FST<Int64>(input, PositiveInt32Outputs.Singleton);

            return true;
        }

        public override IList<LookupResult> DoLookup(string key, bool onlyMorePopular, int num) // ignored
        {
            return DoLookup(key, null, onlyMorePopular, num);
        }

        /// <summary>
        /// Lookup, without any context. </summary>
        public virtual IList<LookupResult> DoLookup(string key, int num)
        {
            return DoLookup(key, null, true, num);
        }

        public override IList<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, /* ignored */ bool onlyMorePopular, int num)
        {
            try
            {
                return DoLookup(key, contexts, num);
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                // bogus:
                throw RuntimeException.Create(ioe);
            }
        }

        public override long Count => count;

        private int CountGrams(BytesRef token)
        {
            int count = 1;
            for (int i = 0; i < token.Length; i++)
            {
                if (token.Bytes[token.Offset + i] == separator)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Retrieve suggestions.
        /// </summary>
        public virtual IList<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, int num)
        {
            // LUCENENET: Added guard clause for null
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (contexts != null)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }

            TokenStream ts = queryAnalyzer.GetTokenStream("", key);
            try
            {
                ITermToBytesRefAttribute termBytesAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                IOffsetAttribute offsetAtt = ts.AddAttribute<IOffsetAttribute>();
                IPositionLengthAttribute posLenAtt = ts.AddAttribute<IPositionLengthAttribute>();
                IPositionIncrementAttribute posIncAtt = ts.AddAttribute<IPositionIncrementAttribute>();
                ts.Reset();

                var lastTokens = new BytesRef[grams];
                //System.out.println("lookup: key='" + key + "'");

                // Run full analysis, but save only the
                // last 1gram, last 2gram, etc.:
                BytesRef tokenBytes = termBytesAtt.BytesRef;
                int maxEndOffset = -1;
                bool sawRealToken = false;
                while (ts.IncrementToken())
                {
                    termBytesAtt.FillBytesRef();
                    sawRealToken |= tokenBytes.Length > 0;
                    // TODO: this is somewhat iffy; today, ShingleFilter
                    // sets posLen to the gram count; maybe we should make
                    // a separate dedicated att for this?
                    int gramCount = posLenAtt.PositionLength;

                    if (Debugging.AssertsEnabled) Debugging.Assert(gramCount <= grams);

                    // Safety: make sure the recalculated count "agrees":
                    if (CountGrams(tokenBytes) != gramCount)
                    {
                        throw new ArgumentException("tokens must not contain separator byte; got token=" + tokenBytes + " but gramCount=" + gramCount + " does not match recalculated count=" + CountGrams(tokenBytes));
                    }
                    maxEndOffset = Math.Max(maxEndOffset, offsetAtt.EndOffset);
                    lastTokens[gramCount - 1] = BytesRef.DeepCopyOf(tokenBytes);
                }
                ts.End();

                if (!sawRealToken)
                {
                    throw new ArgumentException("no tokens produced by analyzer, or the only tokens were empty strings");
                }

                // Carefully fill last tokens with _ tokens;
                // ShingleFilter appraently won't emit "only hole"
                // tokens:
                int endPosInc = posIncAtt.PositionIncrement;

                // Note this will also be true if input is the empty
                // string (in which case we saw no tokens and
                // maxEndOffset is still -1), which in fact works out OK
                // because we fill the unigram with an empty BytesRef
                // below:
                bool lastTokenEnded = offsetAtt.EndOffset > maxEndOffset || endPosInc > 0;
                //System.out.println("maxEndOffset=" + maxEndOffset + " vs " + offsetAtt.EndOffset);

                if (lastTokenEnded)
                {
                    //System.out.println("  lastTokenEnded");
                    // If user hit space after the last token, then
                    // "upgrade" all tokens.  This way "foo " will suggest
                    // all bigrams starting w/ foo, and not any unigrams
                    // starting with "foo":
                    for (int i = grams - 1; i > 0; i--)
                    {
                        BytesRef token = lastTokens[i - 1];
                        if (token is null)
                        {
                            continue;
                        }
                        token.Grow(token.Length + 1);
                        token.Bytes[token.Length] = separator;
                        token.Length++;
                        lastTokens[i] = token;
                    }
                    lastTokens[0] = new BytesRef();
                }

                var arc = new FST.Arc<Int64>();

                var bytesReader = fst.GetBytesReader();

                // Try highest order models first, and if they return
                // results, return that; else, fallback:
                double backoff = 1.0;

                JCG.List<LookupResult> results = new JCG.List<LookupResult>(num);

                // We only add a given suffix once, from the highest
                // order model that saw it; for subsequent lower order
                // models we skip it:
                var seen = new JCG.HashSet<BytesRef>();

                for (int gram = grams - 1; gram >= 0; gram--)
                {
                    BytesRef token = lastTokens[gram];
                    // Don't make unigram predictions from empty string:
                    if (token is null || (token.Length == 0 && key.Length > 0))
                    {
                        // Input didn't have enough tokens:
                        //System.out.println("  gram=" + gram + ": skip: not enough input");
                        continue;
                    }

                    if (endPosInc > 0 && gram <= endPosInc)
                    {
                        // Skip hole-only predictions; in theory we
                        // shouldn't have to do this, but we'd need to fix
                        // ShingleFilter to produce only-hole tokens:
                        //System.out.println("  break: only holes now");
                        break;
                    }

                    //System.out.println("try " + (gram+1) + " gram token=" + token.utf8ToString());

                    // TODO: we could add fuzziness here
                    // match the prefix portion exactly
                    //Pair<Long,BytesRef> prefixOutput = null;
                    Int64 prefixOutput = null;
                    try
                    {
                        prefixOutput = LookupPrefix(fst, bytesReader, token, arc);
                    }
                    catch (Exception bogus) when (bogus.IsIOException())
                    {
                        throw RuntimeException.Create(bogus);
                    }
                    //System.out.println("  prefixOutput=" + prefixOutput);

                    if (prefixOutput is null)
                    {
                        // This model never saw this prefix, e.g. the
                        // trigram model never saw context "purple mushroom"
                        backoff *= ALPHA;
                        continue;
                    }

                    // TODO: we could do this division at build time, and
                    // bake it into the FST?

                    // Denominator for computing scores from current
                    // model's predictions:
                    long contextCount = totTokens;

                    BytesRef lastTokenFragment = null;

                    for (int i = token.Length - 1; i >= 0; i--)
                    {
                        if (token.Bytes[token.Offset + i] == separator)
                        {
                            BytesRef context = new BytesRef(token.Bytes, token.Offset, i);
                            long? output = Lucene.Net.Util.Fst.Util.Get(fst, Lucene.Net.Util.Fst.Util.ToInt32sRef(context, new Int32sRef()));
                            if (Debugging.AssertsEnabled) Debugging.Assert(output != null);
                            contextCount = DecodeWeight(output);
                            lastTokenFragment = new BytesRef(token.Bytes, token.Offset + i + 1, token.Length - i - 1);
                            break;
                        }
                    }

                    BytesRef finalLastToken;

                    if (lastTokenFragment is null)
                    {
                        finalLastToken = BytesRef.DeepCopyOf(token);
                    }
                    else
                    {
                        finalLastToken = BytesRef.DeepCopyOf(lastTokenFragment);
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(finalLastToken.Offset == 0);

                    CharsRef spare = new CharsRef();

                    // complete top-N
                    Util.Fst.Util.TopResults<Int64> completions = null;
                    try
                    {

                        // Because we store multiple models in one FST
                        // (1gram, 2gram, 3gram), we must restrict the
                        // search so that it only considers the current
                        // model.  For highest order model, this is not
                        // necessary since all completions in the FST
                        // must be from this model, but for lower order
                        // models we have to filter out the higher order
                        // ones:

                        // Must do num+seen.size() for queue depth because we may
                        // reject up to seen.size() paths in acceptResult():
                        Util.Fst.Util.TopNSearcher<Int64> searcher = new TopNSearcherAnonymousClass(this, fst, num, num + seen.Count, weightComparer, seen, finalLastToken);

                        // since this search is initialized with a single start node
                        // it is okay to start with an empty input path here
                        searcher.AddStartPaths(arc, prefixOutput, true, new Int32sRef());

                        completions = searcher.Search();
                        if (Debugging.AssertsEnabled) Debugging.Assert(completions.IsComplete);
                    }
                    catch (Exception bogus) when (bogus.IsIOException())
                    {
                        throw RuntimeException.Create(bogus);
                    }

                    int prefixLength = token.Length;

                    BytesRef suffix = new BytesRef(8);
                    //System.out.println("    " + completions.length + " completions");

                    foreach (Util.Fst.Util.Result<Int64> completion in completions)
                    {
                        token.Length = prefixLength;
                        // append suffix
                        Util.Fst.Util.ToBytesRef(completion.Input, suffix);
                        token.Append(suffix);

                        //System.out.println("    completion " + token.utf8ToString());

                        // Skip this path if a higher-order model already
                        // saw/predicted its last token:
                        BytesRef lastToken = token;
                        for (int i = token.Length - 1; i >= 0; i--)
                        {
                            if (token.Bytes[token.Offset + i] == separator)
                            {
                                if (Debugging.AssertsEnabled) Debugging.Assert(token.Length - i - 1 > 0);
                                lastToken = new BytesRef(token.Bytes, token.Offset + i + 1, token.Length - i - 1);
                                break;
                            }
                        }
                        if (seen.Contains(lastToken))
                        {
                            //System.out.println("      skip dup " + lastToken.utf8ToString());
                            goto nextCompletionContinue;
                        }
                        seen.Add(BytesRef.DeepCopyOf(lastToken));
                        spare.Grow(token.Length);
                        UnicodeUtil.UTF8toUTF16(token, spare);
                        LookupResult result = new LookupResult(spare.ToString(),
                            // LUCENENET NOTE: We need to calculate this as decimal because when using double it can sometimes
                            // return numbers that are greater than long.MaxValue, which results in a negative long number.
                            (long)(long.MaxValue * (decimal)backoff * ((decimal)DecodeWeight(completion.Output)) / contextCount));
                        results.Add(result);
                        if (Debugging.AssertsEnabled) Debugging.Assert(results.Count == seen.Count);
                    //System.out.println("  add result=" + result);
                    nextCompletionContinue: {/* LUCENENET: intentionally blank */}
                    }
                    backoff *= ALPHA;
                }

                results.Sort(Comparer<Lookup.LookupResult>.Create((a, b) =>
                {
                    if (a.Value > b.Value)
                    {
                        return -1;
                    }
                    else if (a.Value < b.Value)
                    {
                        return 1;
                    }
                    else
                    {
                        // Tie break by UTF16 sort order:
                        return a.Key.CompareToOrdinal(b.Key);
                    }
                }));

                if (results.Count > num)
                {
                    results.RemoveRange(num, results.Count - num); // LUCENENET: Converted end index to length
                }

                return results;
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        private sealed class TopNSearcherAnonymousClass : Util.Fst.Util.TopNSearcher<Int64>
        {
            private readonly FreeTextSuggester outerInstance;

            private readonly ISet<BytesRef> seen;
            private readonly BytesRef finalLastToken;

            public TopNSearcherAnonymousClass(
                FreeTextSuggester outerInstance,
                FST<Int64> fst,
                int num,
                int size,
                IComparer<Int64> weightComparer,
                ISet<BytesRef> seen,
                BytesRef finalLastToken)
                : base(fst, num, size, weightComparer)
            {
                this.outerInstance = outerInstance;
                this.seen = seen;
                this.finalLastToken = finalLastToken;
                scratchBytes = new BytesRef();
            }


            private readonly BytesRef scratchBytes;

            protected override void AddIfCompetitive(Util.Fst.Util.FSTPath<Int64> path)
            {
                if (path.Arc.Label != outerInstance.separator)
                {
                    //System.out.println("    keep path: " + Util.toBytesRef(path.input, new BytesRef()).utf8ToString() + "; " + path + "; arc=" + path.arc);
                    base.AddIfCompetitive(path);
                }
                else
                {
                    //System.out.println("    prevent path: " + Util.toBytesRef(path.input, new BytesRef()).utf8ToString() + "; " + path + "; arc=" + path.arc);
                }
            }

            protected override bool AcceptResult(Int32sRef input, Int64 output)
            {
                Util.Fst.Util.ToBytesRef(input, scratchBytes);
                finalLastToken.Grow(finalLastToken.Length + scratchBytes.Length);
                int lenSav = finalLastToken.Length;
                finalLastToken.Append(scratchBytes);
                //System.out.println("    accept? input='" + scratchBytes.utf8ToString() + "'; lastToken='" + finalLastToken.utf8ToString() + "'; return " + (seen.contains(finalLastToken) == false));
                bool ret = seen.Contains(finalLastToken) == false;

                finalLastToken.Length = lenSav;
                return ret;
            }
        }

        /// <summary>
        /// weight -> cost </summary>
        private static long EncodeWeight(long ngramCount) // LUCENENET: CA1822: Mark members as static
        {
            return long.MaxValue - ngramCount;
        }

        /// <summary>
        /// cost -> weight </summary>
        //private long decodeWeight(Pair<Long,BytesRef> output) {
        private static long DecodeWeight(long? output)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(output != null);
            return (int)(long.MaxValue - output); // LUCENENET TODO: Perhaps a Java Lucene bug? Why cast to int when returning long?
        }

        // NOTE: copied from WFSTCompletionLookup & tweaked
        private static Int64 LookupPrefix(FST<Int64> fst, FST.BytesReader bytesReader, BytesRef scratch, FST.Arc<Int64> arc) // LUCENENET: CA1822: Mark members as static
        {

            Int64 output = fst.Outputs.NoOutput;

            fst.GetFirstArc(arc);

            var bytes = scratch.Bytes;
            var pos = scratch.Offset;
            var end = pos + scratch.Length;
            while (pos < end)
            {
                if (fst.FindTargetArc(bytes[pos++] & 0xff, arc, arc, bytesReader) is null)
                {
                    return null;
                }
                else
                {
                    output = fst.Outputs.Add(output, arc.Output);
                }
            }

            return output;
        }

        internal static readonly IComparer<Int64> weightComparer =  Comparer<Int64>.Default;

        /// <summary>
        /// Returns the weight associated with an input string,
        /// or null if it does not exist.
        /// </summary>
        public virtual object Get(string key)
        {
            throw UnsupportedOperationException.Create();
        }
    }
}