using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using JCG = J2N.Collections.Generic;

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
    /// A <see cref="PostingsFormat"/> useful for low doc-frequency fields such as primary
    /// keys. Bloom filters are maintained in a ".blm" file which offers "fast-fail"
    /// for reads in segments known to have no record of the key. A choice of
    /// delegate <see cref="PostingsFormat"/> is used to record all other Postings data.
    /// <para/>
    /// A choice of <see cref="BloomFilterFactory"/> can be passed to tailor Bloom Filter
    /// settings on a per-field basis. The default configuration is
    /// <see cref="DefaultBloomFilterFactory"/> which allocates a ~8mb bitset and hashes
    /// values using <see cref="MurmurHash2"/>. This should be suitable for most purposes.
    /// <para/>
    /// The format of the blm file is as follows:
    ///
    /// <list type="bullet">
    ///     <item><description>BloomFilter (.blm) --&gt; Header, DelegatePostingsFormatName,
    ///         NumFilteredFields, Filter<sup>NumFilteredFields</sup>, Footer</description></item>
    ///     <item><description>Filter --&gt; FieldNumber, FuzzySet</description></item>
    ///     <item><description>FuzzySet --&gt;See <see cref="FuzzySet.Serialize(DataOutput)"/></description></item>
    ///     <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(DataOutput, string, int)"/>) </description></item>
    ///     <item><description>DelegatePostingsFormatName --&gt; String (<see cref="DataOutput.WriteString(string)"/>)
    ///         The name of a ServiceProvider registered <see cref="PostingsFormat"/></description></item>
    ///     <item><description>NumFilteredFields --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>) </description></item>
    ///     <item><description>FieldNumber --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>) The number of the
    ///         field in this segment</description></item>
    ///     <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(IndexOutput)"/>) </description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [PostingsFormatName("BloomFilter")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class BloomFilteringPostingsFormat : PostingsFormat
    {
        // LUCENENET specific - removed this static variable because our name is determined by the PostingsFormatNameAttribute
        //public static readonly string BLOOM_CODEC_NAME = "BloomFilter";
        public static readonly int VERSION_START = 1;
        public static readonly int VERSION_CHECKSUM = 2;
        public static readonly int VERSION_CURRENT = VERSION_CHECKSUM;

        /// <summary>Extension of Bloom Filters file.</summary>
        private const string BLOOM_EXTENSION = "blm";

        private readonly BloomFilterFactory _bloomFilterFactory = new DefaultBloomFilterFactory();
        private readonly PostingsFormat _delegatePostingsFormat;

        /// <summary>
        /// Creates Bloom filters for a selection of fields created in the index. This
        /// is recorded as a set of Bitsets held as a segment summary in an additional
        /// "blm" file. This <see cref="PostingsFormat"/> delegates to a choice of delegate
        /// <see cref="PostingsFormat"/> for encoding all other postings data.
        /// </summary>
        /// <param name="delegatePostingsFormat">The <see cref="PostingsFormat"/> that records all the non-bloom filter data i.e. postings info.</param>
        /// <param name="bloomFilterFactory">The <see cref="BloomFilterFactory"/> responsible for sizing BloomFilters appropriately.</param>
        public BloomFilteringPostingsFormat(PostingsFormat delegatePostingsFormat,
            BloomFilterFactory bloomFilterFactory) : base()
        {
            _delegatePostingsFormat = delegatePostingsFormat;
            _bloomFilterFactory = bloomFilterFactory;
        }

        /// <summary>
        /// Creates Bloom filters for a selection of fields created in the index. This
        /// is recorded as a set of Bitsets held as a segment summary in an additional
        /// "blm" file. This <see cref="PostingsFormat"/> delegates to a choice of delegate
        /// <see cref="PostingsFormat"/> for encoding all other postings data. This choice of
        /// constructor defaults to the <see cref="DefaultBloomFilterFactory"/> for
        /// configuring per-field BloomFilters.
        /// </summary>
        /// <param name="delegatePostingsFormat">The <see cref="PostingsFormat"/> that records all the non-bloom filter data i.e. postings info.</param>
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
            if (_delegatePostingsFormat is null)
            {
                throw UnsupportedOperationException.Create("Error - constructed without a choice of PostingsFormat");
            }
            return new BloomFilteredFieldsConsumer(this, _delegatePostingsFormat.FieldsConsumer(state), state);
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new BloomFilteredFieldsProducer(this, state);
        }

        internal class BloomFilteredFieldsProducer : FieldsProducer
        {
            private readonly FieldsProducer _delegateFieldsProducer;
            private readonly JCG.Dictionary<string, FuzzySet> _bloomsByFieldName = new JCG.Dictionary<string, FuzzySet>();

            public BloomFilteredFieldsProducer(BloomFilteringPostingsFormat outerInstance, SegmentReadState state)
            {
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

                    IOUtils.Dispose(bloomIn);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.DisposeWhileHandlingException(bloomIn, _delegateFieldsProducer);
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
                if (!_bloomsByFieldName.TryGetValue(field, out FuzzySet filter) || filter is null)
                {
                    return _delegateFieldsProducer.GetTerms(field);
                }
                else
                {
                    var result = _delegateFieldsProducer.GetTerms(field);
                    return result is null ? null : new BloomFilteredTerms(result, filter);
                }
            }

            public override int Count => _delegateFieldsProducer.Count;

            [Obsolete("iterate fields and add their Count instead.")]
            public override long UniqueTermCount => _delegateFieldsProducer.UniqueTermCount;

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

                public override TermsEnum GetEnumerator()
                {
                    return new BloomFilteredTermsEnum(_delegateTerms, reuseDelegate: null, _filter);
                }

                public override TermsEnum GetEnumerator(TermsEnum reuse)
                {
                    if (!(reuse is null) && reuse is BloomFilteredTermsEnum bfte && bfte.filter == _filter)
                    {
                        // recycle the existing BloomFilteredTermsEnum by asking the delegate
                        // to recycle its contained TermsEnum
                        bfte.Reset(_delegateTerms, bfte.delegateTermsEnum);
                        return bfte;
                    }

                    // We have been handed something we cannot reuse (either wrong
                    // class or wrong filter) so allocate a new object
                    return new BloomFilteredTermsEnum(_delegateTerms, reuse, _filter);
                }


                public override IComparer<BytesRef> Comparer => _delegateTerms.Comparer;

                public override long Count => _delegateTerms.Count;

                public override long SumTotalTermFreq => _delegateTerms.SumTotalTermFreq;

                public override long SumDocFreq => _delegateTerms.SumDocFreq;

                public override int DocCount => _delegateTerms.DocCount;

                public override bool HasFreqs => _delegateTerms.HasFreqs;

                public override bool HasOffsets => _delegateTerms.HasOffsets;

                public override bool HasPositions => _delegateTerms.HasPositions;

                public override bool HasPayloads => _delegateTerms.HasPayloads;
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

                private TermsEnum @delegate =>
                    // pull the iterator only if we really need it -
                    // this can be a relativly heavy operation depending on the 
                    // delegate postings format and they underlying directory
                    // (clone IndexInput)
                    delegateTermsEnum ?? (delegateTermsEnum = _delegateTerms.GetEnumerator(_reuseDelegate));

                public override bool MoveNext()
                {
                    return @delegate.MoveNext();
                }

                [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override sealed BytesRef Next()
                {
                    return @delegate.Next();
                }

                public override sealed IComparer<BytesRef> Comparer => _delegateTerms.Comparer;

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
                    return @delegate.SeekExact(text);
                }

                public override sealed SeekStatus SeekCeil(BytesRef text)
                {
                    return @delegate.SeekCeil(text);
                }

                public override sealed void SeekExact(long ord)
                {
                    @delegate.SeekExact(ord);
                }

                public override sealed BytesRef Term => @delegate.Term;

                public override sealed long Ord => @delegate.Ord;

                public override sealed int DocFreq => @delegate.DocFreq;

                public override sealed long TotalTermFreq => @delegate.TotalTermFreq;

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs,
                    DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
                {
                    return @delegate.DocsAndPositions(liveDocs, reuse, flags);
                }

                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
                {
                    return @delegate.Docs(liveDocs, reuse, flags);
                }
            }

            public override long RamBytesUsed()
            {
                var sizeInBytes = ((_delegateFieldsProducer != null) ? _delegateFieldsProducer.RamBytesUsed() : 0);
                foreach (var entry in _bloomsByFieldName)
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
                    if (Debugging.AssertsEnabled) Debugging.Assert((_bloomFilters.ContainsKey(field) == false));

                    _bloomFilters.Add(field, bloomFilter);
                    return new WrappedTermsConsumer(_delegateFieldsConsumer.AddField(field), bloomFilter);
                }

                // No, use the unfiltered fieldsConsumer - we are not interested in
                // recording any term Bitsets.
                return _delegateFieldsConsumer.AddField(field);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _delegateFieldsConsumer.Dispose();
                    // Now we are done accumulating values for these fields
                    var nonSaturatedBlooms = new JCG.List<KeyValuePair<FieldInfo, FuzzySet>>();

                    foreach (var entry in _bloomFilters)
                    {
                        var bloomFilter = entry.Value;
                        if (!outerInstance._bloomFilterFactory.IsSaturated(bloomFilter, entry.Key))
                            nonSaturatedBlooms.Add(entry);
                    }

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
                        IOUtils.Dispose(bloomOutput);
                    }
                    //We are done with large bitsets so no need to keep them hanging around
                    _bloomFilters.Clear();
                }
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

            public override IComparer<BytesRef> Comparer => _delegateTermsConsumer.Comparer;
        }
    }
}