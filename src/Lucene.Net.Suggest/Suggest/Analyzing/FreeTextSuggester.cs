using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    // TODO
    //   - test w/ syns
    //   - add pruning of low-freq ngrams?   

    /// <summary>
    /// Builds an ngram model from the text sent to <see cref="Build"/>
    /// and predicts based on the last grams-1 tokens in
    /// the request sent to <see cref="DoLookup"/>.  This tries to
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
    /// <para> From <see cref="DoLookup"/>, the key of each result is the
    /// ngram token; the value is <see cref="long.MaxValue"/> * score (fixed
    /// point, cast to long).  Divide by <see cref="long.MaxValue"/> to get
    /// the score back, which ranges from 0.0 to 1.0.
    /// 
    /// <paramref name="onlyMorePopular"/> is unused.
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
        private FST<long?> fst;

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
        /// trigram ,etc.).  The separator is passed to <see cref="ShingleFilter.TokenSeparator"/>
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
                throw new System.ArgumentException("grams must be >= 1");
            }
            if ((separator & 0x80) != 0)
            {
                throw new System.ArgumentException("separator must be simple ascii character");
            }
            this.separator = separator;
        }

        /// <summary>
        /// Returns byte size of the underlying FST. </summary>
        public override long GetSizeInBytes()
        {
            if (fst == null)
            {
                return 0;
            }
            return fst.SizeInBytes();
        }

        private class AnalyzingComparator : IComparer<BytesRef>
        {
            private readonly ByteArrayDataInput readerA = new ByteArrayDataInput();
            private readonly ByteArrayDataInput readerB = new ByteArrayDataInput();
            private readonly BytesRef scratchA = new BytesRef();
            private readonly BytesRef scratchB = new BytesRef();

            public virtual int Compare(BytesRef a, BytesRef b)
            {
                readerA.Reset(a.Bytes, a.Offset, a.Length);
                readerB.Reset(b.Bytes, b.Offset, b.Length);

                // By token:
                scratchA.Length = (ushort)readerA.ReadShort();
                scratchA.Bytes = a.Bytes;
                scratchA.Offset = readerA.Position;

                scratchB.Bytes = b.Bytes;
                scratchB.Length = (ushort)readerB.ReadShort();
                scratchB.Offset = readerB.Position;

                int cmp = scratchA.CompareTo(scratchB);
                if (cmp != 0)
                {
                    return cmp;
                }
                readerA.SkipBytes(scratchA.Length);
                readerB.SkipBytes(scratchB.Length);

                // By length (smaller surface forms sorted first):
                cmp = a.Length - b.Length;
                if (cmp != 0)
                {
                    return cmp;
                }

                // By surface form:
                scratchA.Offset = readerA.Position;
                scratchA.Length = a.Length - scratchA.Offset;
                scratchB.Offset = readerB.Position;
                scratchB.Length = b.Length - scratchB.Offset;

                return scratchA.CompareTo(scratchB);
            }
        }

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
                return new AnalyzerWrapperAnonymousInnerClassHelper(this, other.Strategy, other);
            }
        }

        private class AnalyzerWrapperAnonymousInnerClassHelper : AnalyzerWrapper
        {
            private readonly FreeTextSuggester outerInstance;
            private readonly Analyzer other;

            public AnalyzerWrapperAnonymousInnerClassHelper(FreeTextSuggester outerInstance, ReuseStrategy reuseStrategy, Analyzer other)
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
                shingles.TokenSeparator = char.ToString((char)outerInstance.separator);
                return new TokenStreamComponents(components.Tokenizer, shingles);
            }
        }

        public override void Build(IInputIterator iterator)
        {
            Build(iterator, IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
        }

        /// <summary>
        /// Build the suggest index, using up to the specified
        ///  amount of temporary RAM while building.  Note that
        ///  the weights for the suggestions are ignored. 
        /// </summary>
        public virtual void Build(IInputIterator iterator, double ramBufferSizeMB)
        {
            if (iterator.HasPayloads)
            {
                throw new System.ArgumentException("this suggester doesn't support payloads");
            }
            if (iterator.HasContexts)
            {
                throw new System.ArgumentException("this suggester doesn't support contexts");
            }

            string prefix = this.GetType().Name;
            var directory = OfflineSorter.DefaultTempDir();
            // TODO: messy ... java7 has Files.createTempDirectory
            // ... but 4.x is java6:
            DirectoryInfo tempIndexPath = null;
            Random random = new Random();
            while (true)
            {
                tempIndexPath = new DirectoryInfo(Path.Combine(directory.FullName, prefix + ".index." + random.Next(int.MaxValue)));
                tempIndexPath.Create();
                if (System.IO.Directory.Exists(tempIndexPath.FullName))
                {
                    break;
                }
            }

            using (Directory dir = FSDirectory.Open(tempIndexPath))
            {
#pragma warning disable 612, 618
                IndexWriterConfig iwc = new IndexWriterConfig(LuceneVersion.LUCENE_CURRENT, indexAnalyzer);
#pragma warning restore 612, 618
                iwc.SetOpenMode(IndexWriterConfig.OpenMode_e.CREATE);
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
                    while (true)
                    {
                        BytesRef surfaceForm = iterator.Next();
                        if (surfaceForm == null)
                        {
                            break;
                        }
                        field.SetStringValue(surfaceForm.Utf8ToString());
                        writer.AddDocument(doc);
                        count++;
                    }
                    reader = DirectoryReader.Open(writer, false);

                    Terms terms = MultiFields.GetTerms(reader, "body");
                    if (terms == null)
                    {
                        throw new System.ArgumentException("need at least one suggestion");
                    }

                    // Move all ngrams into an FST:
                    TermsEnum termsEnum = terms.Iterator(null);

                    Outputs<long?> outputs = PositiveIntOutputs.Singleton;
                    Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

                    IntsRef scratchInts = new IntsRef();
                    while (true)
                    {
                        BytesRef term = termsEnum.Next();
                        if (term == null)
                        {
                            break;
                        }
                        int ngramCount = CountGrams(term);
                        if (ngramCount > grams)
                        {
                            throw new System.ArgumentException("tokens must not contain separator byte; got token=" + term + " but gramCount=" + ngramCount + ", which is greater than expected max ngram size=" + grams);
                        }
                        if (ngramCount == 1)
                        {
                            totTokens += termsEnum.TotalTermFreq();
                        }

                        builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(term, scratchInts), EncodeWeight(termsEnum.TotalTermFreq()));
                    }

                    fst = builder.Finish();
                    if (fst == null)
                    {
                        throw new System.ArgumentException("need at least one suggestion");
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
                    try
                    {
                        if (success)
                        {
                            IOUtils.Close(writer, reader);
                        }
                        else
                        {
                            IOUtils.CloseWhileHandlingException(writer, reader);
                        }
                    }
                    finally
                    {
                        foreach (string file in dir.ListAll())
                        {
                            FileInfo path = new FileInfo(Path.Combine(tempIndexPath.FullName, file));
                            try
                            {
                                path.Delete();
                            }
                            catch (Exception e)
                            {
                                throw new InvalidOperationException("failed to remove " + path, e);
                            }
                        }

                        try
                        {
                            tempIndexPath.Delete();
                        }
                        catch (Exception e)
                        {
                            throw new InvalidOperationException("failed to remove " + tempIndexPath, e);
                        }

                    }
                }
            }
        }

        public override bool Store(DataOutput output)
        {
            CodecUtil.WriteHeader(output, CODEC_NAME, VERSION_CURRENT);
            output.WriteVLong(count);
            output.WriteByte(separator);
            output.WriteVInt(grams);
            output.WriteVLong(totTokens);
            fst.Save(output);
            return true;
        }

        public override bool Load(DataInput input)
        {
            CodecUtil.CheckHeader(input, CODEC_NAME, VERSION_START, VERSION_START);
            count = input.ReadVLong();
            var separatorOrig = (sbyte)input.ReadByte();
            if (separatorOrig != separator)
            {
                throw new InvalidOperationException("separator=" + separator + " is incorrect: original model was built with separator=" + separatorOrig);
            }
            int gramsOrig = input.ReadVInt();
            if (gramsOrig != grams)
            {
                throw new InvalidOperationException("grams=" + grams + " is incorrect: original model was built with grams=" + gramsOrig);
            }
            totTokens = input.ReadVLong();

            fst = new FST<long?>(input, PositiveIntOutputs.Singleton);

            return true;
        }

        public override List<LookupResult> DoLookup(string key, bool onlyMorePopular, int num) // ignored
        {
            return DoLookup(key, null, onlyMorePopular, num);
        }

        /// <summary>
        /// Lookup, without any context. </summary>
        public virtual List<LookupResult> DoLookup(string key, int num)
        {
            return DoLookup(key, null, true, num);
        }

        public override List<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, /* ignored */ bool onlyMorePopular, int num)
        {
            return DoLookup(key, contexts, num);
        }

        public override long Count
        {
            get
            {
                return count;
            }
        }

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
        public virtual List<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, int num)
        {
            if (contexts != null)
            {
                throw new System.ArgumentException("this suggester doesn't support contexts");
            }

            TokenStream ts = queryAnalyzer.TokenStream("", key.ToString());
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

                    Debug.Assert(gramCount <= grams);

                    // Safety: make sure the recalculated count "agrees":
                    if (CountGrams(tokenBytes) != gramCount)
                    {
                        throw new System.ArgumentException("tokens must not contain separator byte; got token=" + tokenBytes + " but gramCount=" + gramCount + " does not match recalculated count=" + CountGrams(tokenBytes));
                    }
                    maxEndOffset = Math.Max(maxEndOffset, offsetAtt.EndOffset);
                    lastTokens[gramCount - 1] = BytesRef.DeepCopyOf(tokenBytes);
                }
                ts.End();

                if (!sawRealToken)
                {
                    throw new System.ArgumentException("no tokens produced by analyzer, or the only tokens were empty strings");
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
                        if (token == null)
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

                var arc = new FST.Arc<long?>();

                var bytesReader = fst.BytesReader;

                // Try highest order models first, and if they return
                // results, return that; else, fallback:
                double backoff = 1.0;

                List<LookupResult> results = new List<LookupResult>(num);

                // We only add a given suffix once, from the highest
                // order model that saw it; for subsequent lower order
                // models we skip it:
                var seen = new HashSet<BytesRef>();

                for (int gram = grams - 1; gram >= 0; gram--)
                {
                    BytesRef token = lastTokens[gram];
                    // Don't make unigram predictions from empty string:
                    if (token == null || (token.Length == 0 && key.Length > 0))
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
                    long? prefixOutput = null;
                    prefixOutput = LookupPrefix(fst, bytesReader, token, arc);
                    //System.out.println("  prefixOutput=" + prefixOutput);

                    if (prefixOutput == null)
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
                            long? output = Lucene.Net.Util.Fst.Util.Get(fst, Lucene.Net.Util.Fst.Util.ToIntsRef(context, new IntsRef()));
                            Debug.Assert(output != null);
                            contextCount = DecodeWeight(output);
                            lastTokenFragment = new BytesRef(token.Bytes, token.Offset + i + 1, token.Length - i - 1);
                            break;
                        }
                    }

                    BytesRef finalLastToken;

                    if (lastTokenFragment == null)
                    {
                        finalLastToken = BytesRef.DeepCopyOf(token);
                    }
                    else
                    {
                        finalLastToken = BytesRef.DeepCopyOf(lastTokenFragment);
                    }
                    Debug.Assert(finalLastToken.Offset == 0);

                    CharsRef spare = new CharsRef();

                    // complete top-N
                    Util.Fst.Util.TopResults<long?> completions = null;
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
                        Util.Fst.Util.TopNSearcher<long?> searcher = new TopNSearcherAnonymousInnerClassHelper(this, fst, num, num + seen.Count, weightComparator, seen, finalLastToken);

                        // since this search is initialized with a single start node 
                        // it is okay to start with an empty input path here
                        searcher.AddStartPaths(arc, prefixOutput, true, new IntsRef());

                        completions = searcher.Search();
                        Debug.Assert(completions.IsComplete);
                    }
                    catch (IOException bogus)
                    {
                        throw new Exception(bogus.Message, bogus);
                    }

                    int prefixLength = token.Length;

                    BytesRef suffix = new BytesRef(8);
                    //System.out.println("    " + completions.length + " completions");

                    foreach (Util.Fst.Util.Result<long?> completion in completions)
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
                                Debug.Assert(token.Length - i - 1 > 0);
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
                        Debug.Assert(results.Count == seen.Count);
                        //System.out.println("  add result=" + result);
                        nextCompletionContinue:;
                    }
                    backoff *= ALPHA;
                }

                results.Sort(new ComparatorAnonymousInnerClassHelper(this));

                if (results.Count > num)
                {
                    results.SubList(num, results.Count).Clear();
                }

                return results;
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(ts);
            }
        }

        private class TopNSearcherAnonymousInnerClassHelper : Util.Fst.Util.TopNSearcher<long?>
        {
            private readonly FreeTextSuggester outerInstance;

            private IEnumerable<BytesRef> seen;
            private BytesRef finalLastToken;

            public TopNSearcherAnonymousInnerClassHelper(
                FreeTextSuggester outerInstance,
                FST<long?> fst,
                int num,
                int size,
                IComparer<long?> weightComparator,
                IEnumerable<BytesRef> seen,
                BytesRef finalLastToken)
                : base(fst, num, size, weightComparator)
            {
                this.outerInstance = outerInstance;
                this.seen = seen;
                this.finalLastToken = finalLastToken;
                scratchBytes = new BytesRef();
            }


            private BytesRef scratchBytes;

            protected override void AddIfCompetitive(Util.Fst.Util.FSTPath<long?> path)
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

            protected override bool AcceptResult(IntsRef input, long? output)
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

        private sealed class ComparatorAnonymousInnerClassHelper : IComparer<Lookup.LookupResult>
        {
            private readonly FreeTextSuggester outerInstance;

            public ComparatorAnonymousInnerClassHelper(FreeTextSuggester outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Compare(LookupResult a, LookupResult b)
            {
                if (a.value > b.value)
                {
                    return -1;
                }
                else if (a.value < b.value)
                {
                    return 1;
                }
                else
                {
                    // Tie break by UTF16 sort order:
                    return a.key.CompareToOrdinal(b.key);
                }
            }
        }

        /// <summary>
        /// weight -> cost </summary>
        private long EncodeWeight(long ngramCount)
        {
            return long.MaxValue - ngramCount;
        }

        /// <summary>
        /// cost -> weight </summary>
        //private long decodeWeight(Pair<Long,BytesRef> output) {
        private static long DecodeWeight(long? output)
        {
            Debug.Assert(output != null);
            return (int)(long.MaxValue - output); // LUCENENET TODO: Perhaps a Java Lucene bug? Why cast to int when returning long?
        }

        // NOTE: copied from WFSTCompletionLookup & tweaked
        private long? LookupPrefix(FST<long?> fst, FST.BytesReader bytesReader, BytesRef scratch, FST.Arc<long?> arc)
        {

            long? output = fst.Outputs.NoOutput;

            fst.GetFirstArc(arc);

            var bytes = scratch.Bytes;
            var pos = scratch.Offset;
            var end = pos + scratch.Length;
            while (pos < end)
            {
                if (fst.FindTargetArc(bytes[pos++] & 0xff, arc, arc, bytesReader) == null)
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

        internal static readonly IComparer<long?> weightComparator = new ComparatorAnonymousInnerClassHelper2();

        private sealed class ComparatorAnonymousInnerClassHelper2 : IComparer<long?>
        {
            public ComparatorAnonymousInnerClassHelper2()
            {
            }

            public int Compare(long? left, long? right)
            {
                return Comparer<long?>.Default.Compare(left, right);
            }
        }

        /// <summary>
        /// Returns the weight associated with an input string,
        /// or null if it does not exist.
        /// </summary>
        public virtual object Get(string key)
        {
            throw new NotSupportedException();
        }
    }
}