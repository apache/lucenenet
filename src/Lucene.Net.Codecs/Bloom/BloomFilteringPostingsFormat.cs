using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Codecs.Bloom
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
    [PostingsFormatName("BloomFilter")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class BloomFilteringPostingsFormat : PostingsFormat
    {
        // LUCENENET specific - removed this static variable because our name is determined by the PostingsFormatNameAttribute
        //public static readonly string BLOOM_CODEC_NAME = "BloomFilter";
        public static readonly int VERSION_START = 1;
        public static readonly int VERSION_CHECKSUM = 2;
        public static readonly int VERSION_CURRENT = VERSION_CHECKSUM;

        /// <summary>Extension of Bloom Filters file</summary>
        private const string BLOOM_EXTENSION = "blm";

        private readonly BloomFilterFactory _bloomFilterFactory = new DefaultBloomFilterFactory();
        private readonly PostingsFormat _delegatePostingsFormat;
        
        /// <summary>
        ///  Creates Bloom filters for a selection of fields created in the index. This
        /// is recorded as a set of Bitsets held as a segment summary in an additional
        /// "blm" file. This PostingsFormat delegates to a choice of delegate
        /// PostingsFormat for encoding all other postings data.
        /// </summary>
        /// <param name="delegatePostingsFormat">The PostingsFormat that records all the non-bloom filter data i.e. postings info.</param>
        /// <param name="bloomFilterFactory">The {@link BloomFilterFactory} responsible for sizing BloomFilters appropriately</param>
        public BloomFilteringPostingsFormat(PostingsFormat delegatePostingsFormat,
            BloomFilterFactory bloomFilterFactory) : base()
        {
            _delegatePostingsFormat = delegatePostingsFormat;
            _bloomFilterFactory = bloomFilterFactory;
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
        public BloomFilteringPostingsFormat() 
            : base()
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            if (_delegatePostingsFormat == null)
            {
                throw new InvalidOperationException("Error - constructed without a choice of PostingsFormat");
            }
            return new BloomFilteredFieldsConsumer(this, _delegatePostingsFormat.FieldsConsumer(state), state);
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new BloomFilteredFieldsProducer(this, state);
        }

        internal class BloomFilteredFieldsProducer : FieldsProducer
        {
            private readonly BloomFilteringPostingsFormat outerInstance;
            private readonly FieldsProducer _delegateFieldsProducer;
            private readonly HashMap<string, FuzzySet> _bloomsByFieldName = new HashMap<string, FuzzySet>();

            public BloomFilteredFieldsProducer(BloomFilteringPostingsFormat outerInstance, SegmentReadState state)
            {
                this.outerInstance = outerInstance;
                var bloomFileName = IndexFileNames.SegmentFileName(
                    state.SegmentInfo.Name, state.SegmentSuffix, BLOOM_EXTENSION);
                ChecksumIndexInput bloomIn = null;
                var success = false;
                try
                {
                    bloomIn = state.Directory.OpenChecksumInput(bloomFileName, state.Context);
                    var version = CodecUtil.CheckHeader(bloomIn, /*BLOOM_CODEC_NAME*/ outerInstance.Name, VERSION_START, VERSION_CURRENT);
                    // Load the hash function used in the BloomFilter
                    // hashFunction = HashFunction.forName(bloomIn.readString());
                    // Load the delegate postings format
                    var delegatePostingsFormat = ForName(bloomIn.ReadString());

                    _delegateFieldsProducer = delegatePostingsFormat
                        .FieldsProducer(state);
                    var numBlooms = bloomIn.ReadInt32();
                    for (var i = 0; i < numBlooms; i++)
                    {
                        var fieldNum = bloomIn.ReadInt32();
                        var bloom = FuzzySet.Deserialize(bloomIn);
                        var fieldInfo = state.FieldInfos.FieldInfo(fieldNum);
                        _bloomsByFieldName.Add(fieldInfo.Name, bloom);
                    }
                    
                    if (version >= VERSION_CHECKSUM)
                    {
                        CodecUtil.CheckFooter(bloomIn);
                    }
                    else
                    {
#pragma warning disable 612, 618
                        CodecUtil.CheckEOF(bloomIn);
#pragma warning restore 612, 618
                    }

                    IOUtils.Close(bloomIn);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(bloomIn, _delegateFieldsProducer);
                    }
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return _delegateFieldsProducer.GetEnumerator();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _delegateFieldsProducer.Dispose();
                }
            }

            public override Terms GetTerms(string field)
            {
                FuzzySet filter;
                if (!_bloomsByFieldName.TryGetValue(field, out filter) || filter == null)
                {
                    return _delegateFieldsProducer.GetTerms(field);
                }
                else
                {
                    var result = _delegateFieldsProducer.GetTerms(field);
                    return result == null ? null : new BloomFilteredTerms(result, filter);
                }
            }

            public override int Count
            {
                get
                {
                    return _delegateFieldsProducer.Count;
                }
            }

            [Obsolete("iterate fields and add their Count instead.")]
            public override long UniqueTermCount
            {
                get { return _delegateFieldsProducer.UniqueTermCount; }
            }

            internal class BloomFilteredTerms : Terms
            {
                private readonly Terms _delegateTerms;
                private readonly FuzzySet _filter;

                public BloomFilteredTerms(Terms terms, FuzzySet filter)
                {
                    _delegateTerms = terms;
                    _filter = filter;
                }

                public override TermsEnum Intersect(CompiledAutomaton compiled,
                    BytesRef startTerm)
                {
                    return _delegateTerms.Intersect(compiled, startTerm);
                }

                public override TermsEnum GetIterator(TermsEnum reuse)
                {
                    if (!(reuse is BloomFilteredTermsEnum))
                        return new BloomFilteredTermsEnum(_delegateTerms, reuse, _filter);

                    // recycle the existing BloomFilteredTermsEnum by asking the delegate
                    // to recycle its contained TermsEnum
                    var bfte = (BloomFilteredTermsEnum) reuse;

                    // We have been handed something we cannot reuse (either null, wrong
                    // class or wrong filter) so allocate a new object
                    if (bfte.filter != _filter) return new BloomFilteredTermsEnum(_delegateTerms, reuse, _filter);
                    bfte.Reset(_delegateTerms, bfte.delegateTermsEnum);
                    return bfte;
                    
                }

                public override IComparer<BytesRef> Comparer
                {
                    get { return _delegateTerms.Comparer; }
                }

                public override long Count
                {
                    get { return _delegateTerms.Count; }
                }

                public override long SumTotalTermFreq
                {
                    get { return _delegateTerms.SumTotalTermFreq; }
                }

                public override long SumDocFreq
                {
                    get { return _delegateTerms.SumDocFreq; }
                }

                public override int DocCount
                {
                    get { return _delegateTerms.DocCount; }
                }

                public override bool HasFreqs
                {
                    get { return _delegateTerms.HasFreqs; }
                }

                public override bool HasOffsets
                {
                    get { return _delegateTerms.HasOffsets; }
                }

                public override bool HasPositions
                {
                    get { return _delegateTerms.HasPositions; }
                }

                public override bool HasPayloads
                {
                    get { return _delegateTerms.HasPayloads; }
                }
            }

            internal sealed class BloomFilteredTermsEnum : TermsEnum
            {
                private Terms _delegateTerms;
                internal TermsEnum delegateTermsEnum;
                private TermsEnum _reuseDelegate;
                internal readonly FuzzySet filter; 

                public BloomFilteredTermsEnum(Terms delegateTerms, TermsEnum reuseDelegate, FuzzySet filter)
                {
                    _delegateTerms = delegateTerms;
                    _reuseDelegate = reuseDelegate;
                    this.filter = filter;
                }

                internal void Reset(Terms delegateTerms, TermsEnum reuseDelegate)
                {
                    _delegateTerms = delegateTerms;
                    _reuseDelegate = reuseDelegate;
                    delegateTermsEnum = null;
                }

                private TermsEnum Delegate
                {
                    get
                    {
                        // pull the iterator only if we really need it -
                        // this can be a relativly heavy operation depending on the 
                        // delegate postings format and they underlying directory
                        // (clone IndexInput)
                        return delegateTermsEnum ?? (delegateTermsEnum = _delegateTerms.GetIterator(_reuseDelegate));
                    }
                }

                public override sealed BytesRef Next()
                {
                    return Delegate.Next();
                }

                public override sealed IComparer<BytesRef> Comparer
                {
                    get { return _delegateTerms.Comparer; }
                }

                public override sealed bool SeekExact(BytesRef text)
                {
                    // The magical fail-fast speed up that is the entire point of all of
                    // this code - save a disk seek if there is a match on an in-memory
                    // structure
                    // that may occasionally give a false positive but guaranteed no false
                    // negatives
                    if (filter.Contains(text) == FuzzySet.ContainsResult.NO)
                    {
                        return false;
                    }
                    return Delegate.SeekExact(text);
                }

                public override sealed SeekStatus SeekCeil(BytesRef text)
                {
                    return Delegate.SeekCeil(text);
                }

                public override sealed void SeekExact(long ord)
                {
                    Delegate.SeekExact(ord);
                }

                public override sealed BytesRef Term
                {
                    get { return Delegate.Term; }
                }

                public override sealed long Ord
                {
                    get { return Delegate.Ord; }
                }

                public override sealed int DocFreq
                {
                    get { return Delegate.DocFreq; }
                }

                public override sealed long TotalTermFreq
                {
                    get { return Delegate.TotalTermFreq; }
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs,
                    DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
                {
                    return Delegate.DocsAndPositions(liveDocs, reuse, flags);
                }

                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
                {
                    return Delegate.Docs(liveDocs, reuse, flags);
                }
            }

            public override long RamBytesUsed()
            {
                var sizeInBytes = ((_delegateFieldsProducer != null) ? _delegateFieldsProducer.RamBytesUsed() : 0);
                foreach (var entry in _bloomsByFieldName.EntrySet())
                {
                    sizeInBytes += entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR;
                    sizeInBytes += entry.Value.RamBytesUsed();
                }
                return sizeInBytes;
            }

            public override void CheckIntegrity()
            {
                _delegateFieldsProducer.CheckIntegrity();
            }
        }

        internal class BloomFilteredFieldsConsumer : FieldsConsumer
        {
            private readonly BloomFilteringPostingsFormat outerInstance;

            private readonly FieldsConsumer _delegateFieldsConsumer;
            private readonly Dictionary<FieldInfo, FuzzySet> _bloomFilters = new Dictionary<FieldInfo, FuzzySet>();
            private readonly SegmentWriteState _state;
            
            public BloomFilteredFieldsConsumer(BloomFilteringPostingsFormat outerInstance, FieldsConsumer fieldsConsumer,
                SegmentWriteState state)
            {
                this.outerInstance = outerInstance;
                _delegateFieldsConsumer = fieldsConsumer;
                _state = state;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                var bloomFilter = outerInstance._bloomFilterFactory.GetSetForField(_state, field);
                if (bloomFilter != null)
                {
                    Debug.Assert((_bloomFilters.ContainsKey(field) == false));

                    _bloomFilters.Add(field, bloomFilter);
                    return new WrappedTermsConsumer(_delegateFieldsConsumer.AddField(field), bloomFilter);
                }

                // No, use the unfiltered fieldsConsumer - we are not interested in
                // recording any term Bitsets.
                return _delegateFieldsConsumer.AddField(field);
            }

            public override void Dispose()
            {
                _delegateFieldsConsumer.Dispose();
                // Now we are done accumulating values for these fields
                var nonSaturatedBlooms = (from entry in _bloomFilters.EntrySet() let bloomFilter = entry.Value where !outerInstance._bloomFilterFactory.IsSaturated(bloomFilter, entry.Key) select entry).ToList();

                var bloomFileName = IndexFileNames.SegmentFileName(
                    _state.SegmentInfo.Name, _state.SegmentSuffix, BLOOM_EXTENSION);
                IndexOutput bloomOutput = null;

                try
                {
                    bloomOutput = _state.Directory.CreateOutput(bloomFileName, _state.Context);
                    CodecUtil.WriteHeader(bloomOutput, /*BLOOM_CODEC_NAME*/ outerInstance.Name, VERSION_CURRENT);
                    // remember the name of the postings format we will delegate to
                    bloomOutput.WriteString(outerInstance._delegatePostingsFormat.Name);

                    // First field in the output file is the number of fields+blooms saved
                    bloomOutput.WriteInt32(nonSaturatedBlooms.Count);
                    foreach (var entry in nonSaturatedBlooms)
                    {
                        var fieldInfo = entry.Key;
                        var bloomFilter = entry.Value;
                        bloomOutput.WriteInt32(fieldInfo.Number);
                        SaveAppropriatelySizedBloomFilter(bloomOutput, bloomFilter, fieldInfo);
                    }

                    CodecUtil.WriteFooter(bloomOutput);
                }
                finally
                {
                    IOUtils.Close(bloomOutput);
                }
                //We are done with large bitsets so no need to keep them hanging around
                _bloomFilters.Clear();
            }

            private void SaveAppropriatelySizedBloomFilter(DataOutput bloomOutput,
                FuzzySet bloomFilter, FieldInfo fieldInfo)
            {
                var rightSizedSet = outerInstance._bloomFilterFactory.Downsize(fieldInfo,
                    bloomFilter) ?? bloomFilter;

                rightSizedSet.Serialize(bloomOutput);
            }
        }

        internal class WrappedTermsConsumer : TermsConsumer
        {
            private readonly TermsConsumer _delegateTermsConsumer;
            private readonly FuzzySet _bloomFilter;

            public WrappedTermsConsumer(TermsConsumer termsConsumer, FuzzySet bloomFilter)
            {
                _delegateTermsConsumer = termsConsumer;
                _bloomFilter = bloomFilter;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                return _delegateTermsConsumer.StartTerm(text);
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                // Record this term in our BloomFilter
                if (stats.DocFreq > 0)
                {
                    _bloomFilter.AddValue(text);
                }
                _delegateTermsConsumer.FinishTerm(text, stats);
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                _delegateTermsConsumer.Finish(sumTotalTermFreq, sumDocFreq, docCount);
            }

            public override IComparer<BytesRef> Comparer
            {
                get { return _delegateTermsConsumer.Comparer; }
            }
        }
    }
}