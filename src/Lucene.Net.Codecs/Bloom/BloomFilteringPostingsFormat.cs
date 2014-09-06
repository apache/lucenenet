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

namespace Lucene.Net.Codecs.Bloom
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using Lucene.Net.Util.Automaton;

    /// <summary>
    /// 
    /// A {@link PostingsFormat} useful for low doc-frequency fields such as primary
    /// keys. Bloom filters are maintained in a ".blm" file which offers "fast-fail"
    /// for reads in segments known to have no record of the key. A choice of
    /// delegate PostingsFormat is used to record all other Postings data.
    /// 
    /// A choice of {@link BloomFilterFactory} can be passed to tailor Bloom Filter
    /// settings on a per-field basis. The default configuration is
    /// {@link DefaultBloomFilterFactory} which allocates a ~8mb bitset and hashes
    /// values using {@link MurmurHash2}. This should be suitable for most purposes.
    ///
    /// The format of the blm file is as follows:
    ///
    /// <ul>
    /// <li>BloomFilter (.blm) --&gt; Header, DelegatePostingsFormatName,
    /// NumFilteredFields, Filter<sup>NumFilteredFields</sup>, Footer</li>
    /// <li>Filter --&gt; FieldNumber, FuzzySet</li>
    /// <li>FuzzySet --&gt;See {@link FuzzySet#serialize(DataOutput)}</li>
    /// <li>Header --&gt; {@link CodecUtil#writeHeader CodecHeader}</li>
    /// <li>DelegatePostingsFormatName --&gt; {@link DataOutput#writeString(String)
    /// String} The name of a ServiceProvider registered {@link PostingsFormat}</li>
    /// <li>NumFilteredFields --&gt; {@link DataOutput#writeInt Uint32}</li>
    /// <li>FieldNumber --&gt; {@link DataOutput#writeInt Uint32} The number of the
    /// field in this segment</li>
    /// <li>Footer --&gt; {@link CodecUtil#writeFooter CodecFooter}</li>
    /// </ul>
    ///
    ///  @lucene.experimental
    /// </summary>
    public sealed class BloomFilteringPostingsFormat : PostingsFormat
    {
        public static readonly String BLOOM_CODEC_NAME = "BloomFilter";
        public static readonly int VERSION_START = 1;
        public static readonly int VERSION_CHECKSUM = 2;
        public static readonly int VERSION_CURRENT = VERSION_CHECKSUM;

        /** Extension of Bloom Filters file */
        private static readonly String BLOOM_EXTENSION = "blm";

        private BloomFilterFactory bloomFilterFactory = new DefaultBloomFilterFactory();
        private PostingsFormat delegatePostingsFormat;
        
        /// <summary>
        ///  Creates Bloom filters for a selection of fields created in the index. This
        /// is recorded as a set of Bitsets held as a segment summary in an additional
        /// "blm" file. This PostingsFormat delegates to a choice of delegate
        /// PostingsFormat for encoding all other postings data.
        /// </summary>
        /// <param name="delegatePostingsFormat">The PostingsFormat that records all the non-bloom filter data i.e. postings info.</param>
        /// <param name="bloomFilterFactory">The {@link BloomFilterFactory} responsible for sizing BloomFilters appropriately</param>
        public BloomFilteringPostingsFormat(PostingsFormat delegatePostingsFormat,
            BloomFilterFactory bloomFilterFactory) : base(BLOOM_CODEC_NAME)
        {
            this.delegatePostingsFormat = delegatePostingsFormat;
            this.bloomFilterFactory = bloomFilterFactory;
        }

        /// <summary>
        /// Creates Bloom filters for a selection of fields created in the index. This
        /// is recorded as a set of Bitsets held as a segment summary in an additional
        /// "blm" file. This PostingsFormat delegates to a choice of delegate
        /// PostingsFormat for encoding all other postings data. This choice of
        /// constructor defaults to the {@link DefaultBloomFilterFactory} for
        /// configuring per-field BloomFilters.
        /// </summary>
        /// <param name="delegatePostingsFormat">The PostingsFormat that records all the non-bloom filter data i.e. postings info.</param>
        public BloomFilteringPostingsFormat(PostingsFormat delegatePostingsFormat)
            : this(delegatePostingsFormat, new DefaultBloomFilterFactory())
        {
        }

        /// <summary>
        /// Used only by core Lucene at read-time via Service Provider instantiation -
        /// do not use at Write-time in application code.
        /// </summary>
        public BloomFilteringPostingsFormat() : base(BLOOM_CODEC_NAME)
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            if (delegatePostingsFormat == null)
            {
                throw new InvalidOperationException("Error - constructed without a choice of PostingsFormat");
            }
            return new BloomFilteredFieldsConsumer(
                delegatePostingsFormat.FieldsConsumer(state), state,
                delegatePostingsFormat);
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new BloomFilteredFieldsProducer(state);
        }

        internal class BloomFilteredFieldsProducer : FieldsProducer
        {
            private FieldsProducer delegateFieldsProducer;
            private HashMap<String, FuzzySet> bloomsByFieldName = new HashMap<String, FuzzySet>();

            public BloomFilteredFieldsProducer(SegmentReadState state)
            {

                String bloomFileName = IndexFileNames.SegmentFileName(
                    state.SegmentInfo.Name, state.SegmentSuffix, BLOOM_EXTENSION);
                ChecksumIndexInput bloomIn = null;
                bool success = false;
                try
                {
                    bloomIn = state.Directory.OpenChecksumInput(bloomFileName, state.Context);
                    int version = CodecUtil.CheckHeader(bloomIn, BLOOM_CODEC_NAME, VERSION_START, VERSION_CURRENT);
                    // // Load the hash function used in the BloomFilter
                    // hashFunction = HashFunction.forName(bloomIn.readString());
                    // Load the delegate postings format
                    PostingsFormat delegatePostingsFormat = PostingsFormat.ForName(bloomIn
                        .ReadString());

                    this.delegateFieldsProducer = delegatePostingsFormat
                        .FieldsProducer(state);
                    int numBlooms = bloomIn.ReadInt();
                    for (int i = 0; i < numBlooms; i++)
                    {
                        int fieldNum = bloomIn.ReadInt();
                        FuzzySet bloom = FuzzySet.Deserialize(bloomIn);
                        FieldInfo fieldInfo = state.FieldInfos.FieldInfo(fieldNum);
                        bloomsByFieldName.Add(fieldInfo.Name, bloom);
                    }
                    if (version >= VERSION_CHECKSUM)
                    {
                        CodecUtil.CheckFooter(bloomIn);
                    }
                    else
                    {
                        CodecUtil.CheckEOF(bloomIn);
                    }
                    IOUtils.Close(bloomIn);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(bloomIn, delegateFieldsProducer);
                    }
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return delegateFieldsProducer.GetEnumerator();
            }

            public override Terms Terms(String field)
            {
                FuzzySet filter = bloomsByFieldName[field];
                if (filter == null)
                {
                    return delegateFieldsProducer.Terms(field);
                }
                else
                {
                    Terms result = delegateFieldsProducer.Terms(field);
                    if (result == null)
                    {
                        return null;
                    }
                    return new BloomFilteredTerms(result, filter);
                }
            }

            public override int Size()
            {
                return delegateFieldsProducer.Size();
            }

            public override long UniqueTermCount
            {
                get { return delegateFieldsProducer.UniqueTermCount; }
            }

            public override void Dispose()
            {
                delegateFieldsProducer.Dispose();
            }

            public override long RamBytesUsed()
            {
                long sizeInBytes = ((delegateFieldsProducer != null) ? delegateFieldsProducer.RamBytesUsed() : 0);
                foreach (var entry in bloomsByFieldName.EntrySet())
                {
                    sizeInBytes += entry.Key.Length*RamUsageEstimator.NUM_BYTES_CHAR;
                    sizeInBytes += entry.Value.RamBytesUsed();
                }
                return sizeInBytes;
            }

            public override void CheckIntegrity()
            {
                delegateFieldsProducer.CheckIntegrity();
            }

            internal class BloomFilteredTerms : Terms
            {
                private Terms delegateTerms;
                private FuzzySet filter;

                public BloomFilteredTerms(Terms terms, FuzzySet filter)
                {
                    this.delegateTerms = terms;
                    this.filter = filter;
                }

                public override TermsEnum Intersect(CompiledAutomaton compiled,
                    BytesRef startTerm)
                {
                    return delegateTerms.Intersect(compiled, startTerm);
                }

                public override TermsEnum Iterator(TermsEnum reuse)
                {
                    if ((reuse != null) && (reuse is BloomFilteredTermsEnum))
                    {
                        // recycle the existing BloomFilteredTermsEnum by asking the delegate
                        // to recycle its contained TermsEnum
                        BloomFilteredTermsEnum bfte = (BloomFilteredTermsEnum) reuse;
                        if (bfte.filter == filter)
                        {
                            bfte.Reset(delegateTerms, bfte.delegateTermsEnum);
                            return bfte;
                        }
                    }
                    // We have been handed something we cannot reuse (either null, wrong
                    // class or wrong filter) so allocate a new object
                    return new BloomFilteredTermsEnum(delegateTerms, reuse, filter);
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return delegateTerms.Comparator; }
                }

                public override long Size()
                {
                    return delegateTerms.Size();
                }

                public override long SumTotalTermFreq
                {
                    get { return delegateTerms.SumTotalTermFreq; }
                }

                public override long SumDocFreq
                {
                    get { return delegateTerms.SumDocFreq; }
                }

                public override int DocCount
                {
                    get { return delegateTerms.DocCount; }
                }

                public override bool HasFreqs()
                {
                    return delegateTerms.HasFreqs();
                }

                public override bool HasOffsets()
                {
                    return delegateTerms.HasOffsets();
                }

                public override bool HasPositions()
                {
                    return delegateTerms.HasPositions();
                }

                public override bool HasPayloads()
                {
                    return delegateTerms.HasPayloads();
                }
            }

            internal sealed class BloomFilteredTermsEnum : TermsEnum
            {
                private Terms delegateTerms;
                internal TermsEnum delegateTermsEnum;
                private TermsEnum reuseDelegate;
                internal readonly FuzzySet filter;

                public BloomFilteredTermsEnum(Terms delegateTerms, TermsEnum reuseDelegate, FuzzySet filter)
                {
                    this.delegateTerms = delegateTerms;
                    this.reuseDelegate = reuseDelegate;
                    this.filter = filter;
                }

                internal void Reset(Terms delegateTerms, TermsEnum reuseDelegate)
                {
                    this.delegateTerms = delegateTerms;
                    this.reuseDelegate = reuseDelegate;
                    this.delegateTermsEnum = null;
                }

                private TermsEnum Delegate()
                {
                    if (delegateTermsEnum == null)
                    {
                        /* pull the iterator only if we really need it -
                    * this can be a relativly heavy operation depending on the 
                    * delegate postings format and they underlying directory
                    * (clone IndexInput) */
                        delegateTermsEnum = delegateTerms.Iterator(reuseDelegate);
                    }

                    return delegateTermsEnum;
                }

                public override BytesRef Next()
                {
                    return Delegate().Next();
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return delegateTerms.Comparator; }
                }

                public override bool SeekExact(BytesRef text)
                {
                    // The magical fail-fast speed up that is the entire point of all of
                    // this code - save a disk seek if there is a match on an in-memory
                    // structure
                    // that may occasionally give a false positive but guaranteed no false
                    // negatives
                    if (filter.Contains(text) == FuzzySet.ContainsResult.No)
                    {
                        return false;
                    }
                    return Delegate().SeekExact(text);
                }

                public override SeekStatus SeekCeil(BytesRef text)
                {
                    return Delegate().SeekCeil(text);
                }

                public override void SeekExact(long ord)
                {
                    Delegate().SeekExact(ord);
                }

                public override BytesRef Term()
                {
                    return Delegate().Term();
                }

                public override long Ord()
                {
                    return Delegate().Ord();
                }

                public override int DocFreq()
                {
                    return Delegate().DocFreq();
                }

                public override long TotalTermFreq()
                {
                    return Delegate().TotalTermFreq();
                }

                public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs,
                    DocsAndPositionsEnum reuse, int flags)
                {
                    return Delegate().DocsAndPositions(liveDocs, reuse, flags);
                }

                public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
                {
                    return Delegate().Docs(liveDocs, reuse, flags);
                }
            }

        }

        internal class BloomFilteredFieldsConsumer : FieldsConsumer
        {
            private FieldsConsumer delegateFieldsConsumer;
            private Dictionary<FieldInfo, FuzzySet> bloomFilters = new Dictionary<FieldInfo, FuzzySet>();
            private SegmentWriteState state;

            public BloomFilteredFieldsConsumer(FieldsConsumer fieldsConsumer,
                SegmentWriteState state, PostingsFormat delegatePostingsFormat)
            {
                this.delegateFieldsConsumer = fieldsConsumer;
                this.state = state;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                FuzzySet bloomFilter = bloomFilterFactory.GetSetForField(state, field);
                if (bloomFilter != null)
                {
                    Debug.Debug.Assert((bloomFilters.ContainsKey(field) == false);
                    bloomFilters.Add(field, bloomFilter);
                    return new WrappedTermsConsumer(delegateFieldsConsumer.AddField(field), bloomFilter);
                }
                else
                {
                    // No, use the unfiltered fieldsConsumer - we are not interested in
                    // recording any term Bitsets.
                    return delegateFieldsConsumer.AddField(field);
                }
            }

            public override void Dispose()
            {
                delegateFieldsConsumer.Dispose();
                // Now we are done accumulating values for these fields
                var nonSaturatedBlooms = new List<KeyValuePair<FieldInfo, FuzzySet>>();

                foreach (var entry in bloomFilters.EntrySet())
                {
                    FuzzySet bloomFilter = entry.Value;
                    if (!bloomFilterFactory.IsSaturated(bloomFilter, entry.Key))
                    {
                        nonSaturatedBlooms.Add(entry);
                    }
                }

                String bloomFileName = IndexFileNames.SegmentFileName(
                    state.SegmentInfo.Name, state.SegmentSuffix, BLOOM_EXTENSION);
                IndexOutput bloomOutput = null;

                try
                {
                    bloomOutput = state.Directory.CreateOutput(bloomFileName, state.Context);
                    CodecUtil.WriteHeader(bloomOutput, BLOOM_CODEC_NAME, VERSION_CURRENT);
                    // remember the name of the postings format we will delegate to
                    bloomOutput.WriteString(delegatePostingsFormat.Name);

                    // First field in the output file is the number of fields+blooms saved
                    bloomOutput.WriteInt(nonSaturatedBlooms.Count);
                    foreach (var entry in nonSaturatedBlooms)
                    {
                        FieldInfo fieldInfo = entry.Key;
                        FuzzySet bloomFilter = entry.Value;
                        bloomOutput.WriteInt(fieldInfo.Number);
                        SaveAppropriatelySizedBloomFilter(bloomOutput, bloomFilter, fieldInfo);
                    }

                    CodecUtil.WriteFooter(bloomOutput);
                }
                finally
                {
                    IOUtils.Close(bloomOutput);
                }
                //We are done with large bitsets so no need to keep them hanging around
                bloomFilters.Clear();
            }

            private void SaveAppropriatelySizedBloomFilter(IndexOutput bloomOutput,
                FuzzySet bloomFilter, FieldInfo fieldInfo)
            {

                FuzzySet rightSizedSet = bloomFilterFactory.Downsize(fieldInfo,
                    bloomFilter);
                if (rightSizedSet == null)
                {
                    rightSizedSet = bloomFilter;
                }
                rightSizedSet.Serialize(bloomOutput);
            }

        }

        internal class WrappedTermsConsumer : TermsConsumer
        {
            private TermsConsumer delegateTermsConsumer;
            private FuzzySet bloomFilter;

            public WrappedTermsConsumer(TermsConsumer termsConsumer, FuzzySet bloomFilter)
            {
                this.delegateTermsConsumer = termsConsumer;
                this.bloomFilter = bloomFilter;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                return delegateTermsConsumer.StartTerm(text);
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                // Record this term in our BloomFilter
                if (stats.DocFreq > 0)
                {
                    bloomFilter.AddValue(text);
                }
                delegateTermsConsumer.FinishTerm(text, stats);
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                delegateTermsConsumer.Finish(sumTotalTermFreq, sumDocFreq, docCount);
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return delegateTermsConsumer.Comparator; }
            }

        }

    }
}