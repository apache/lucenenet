using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Search.Suggest.Fst
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
    /// Suggester based on a weighted FST: it first traverses the prefix, 
    /// then walks the <i>n</i> shortest paths to retrieve top-ranked
    /// suggestions.
    /// <para>
    /// <b>NOTE</b>:
    /// Input weights must be between 0 and <see cref="int.MaxValue"/>, any
    /// other values will be rejected.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class WFSTCompletionLookup : Lookup
    {

        /// <summary>
        /// FST{long?}, weights are encoded as costs: (int.MaxValue-weight)
        /// </summary>
        // NOTE: like FSTSuggester, this is really a WFSA, if you want to
        // customize the code to add some output you should use PairOutputs.
        private FST<Int64> fst = null;

        /// <summary>
        /// True if exact match suggestions should always be returned first.
        /// </summary>
        private readonly bool exactFirst;

        /// <summary>
        /// Number of entries the lookup was built with </summary>
        private long count = 0;

        /// <summary>
        /// Calls <see cref="WFSTCompletionLookup(bool)">WFSTCompletionLookup(true)</see>
        /// </summary>
        public WFSTCompletionLookup()
            : this(true)
        {
        }

        /// <summary>
        /// Creates a new suggester.
        /// </summary>
        /// <param name="exactFirst"> <code>true</code> if suggestions that match the 
        ///        prefix exactly should always be returned first, regardless
        ///        of score. This has no performance impact, but could result
        ///        in low-quality suggestions. </param>
        public WFSTCompletionLookup(bool exactFirst)
        {
            this.exactFirst = exactFirst;
        }

        public override void Build(IInputEnumerator enumerator)
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
            count = 0;
            BytesRef scratch;
            IInputEnumerator iter = new WFSTInputEnumerator(enumerator);
            var scratchInts = new Int32sRef();
            BytesRef previous = null;
            var outputs = PositiveInt32Outputs.Singleton;
            var builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, outputs);
            while (iter.MoveNext())
            {
                scratch = iter.Current;
                long cost = iter.Weight;

                if (previous is null)
                {
                    previous = new BytesRef();
                }
                else if (scratch.Equals(previous))
                {
                    continue; // for duplicate suggestions, the best weight is actually
                    // added
                }
                Lucene.Net.Util.Fst.Util.ToInt32sRef(scratch, scratchInts);
                builder.Add(scratchInts, cost);
                previous.CopyBytes(scratch);
                count++;
            }
            fst = builder.Finish();
        }

        public override bool Store(DataOutput output)
        {
            output.WriteVInt64(count);
            if (fst is null)
            {
                return false;
            }
            fst.Save(output);
            return true;
        }

        public override bool Load(DataInput input)
        {
            count = input.ReadVInt64();
            this.fst = new FST<Int64>(input, PositiveInt32Outputs.Singleton);
            return true;
        }

        public override IList<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            // LUCENENET: Added guard clause for null
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (contexts != null)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(num > 0);

            if (onlyMorePopular)
            {
                throw new ArgumentException("this suggester only works with onlyMorePopular=false");
            }

            if (fst is null)
            {
                return Collections.EmptyList<LookupResult>();
            }

            BytesRef scratch = new BytesRef(key);
            int prefixLength = scratch.Length;
            FST.Arc<Int64> arc = new FST.Arc<Int64>();

            // match the prefix portion exactly
            Int64 prefixOutput;
            try
            {
                prefixOutput = LookupPrefix(scratch, arc);
            }
            catch (Exception bogus) when (bogus.IsIOException())
            {
                throw RuntimeException.Create(bogus);
            }

            if (prefixOutput is null)
            {
                return Collections.EmptyList<LookupResult>();
            }

            IList<LookupResult> results = new JCG.List<LookupResult>(num);
            CharsRef spare = new CharsRef();
            if (exactFirst && arc.IsFinal)
            {
                spare.Grow(scratch.Length);
                UnicodeUtil.UTF8toUTF16(scratch, spare);
                results.Add(new LookupResult(spare.ToString(), DecodeWeight(prefixOutput + arc.NextFinalOutput)));
                if (--num == 0)
                {
                    return results; // that was quick
                }
            }

            // complete top-N
            Util.Fst.Util.TopResults<Int64> completions;
            try
            {
                completions = Lucene.Net.Util.Fst.Util.ShortestPaths(fst, arc, prefixOutput, weightComparer, num, !exactFirst);
                if (Debugging.AssertsEnabled) Debugging.Assert(completions.IsComplete);
            }
            catch (Exception bogus) when (bogus.IsIOException())
            {
                throw RuntimeException.Create(bogus);
            }

            BytesRef suffix = new BytesRef(8);
            foreach (Util.Fst.Util.Result<Int64> completion in completions)
            {
                scratch.Length = prefixLength;
                // append suffix
                Lucene.Net.Util.Fst.Util.ToBytesRef(completion.Input, suffix);
                scratch.Append(suffix);
                spare.Grow(scratch.Length);
                UnicodeUtil.UTF8toUTF16(scratch, spare);
                results.Add(new LookupResult(spare.ToString(), DecodeWeight(completion.Output)));
            }
            return results;
        }

        private Int64 LookupPrefix(BytesRef scratch, FST.Arc<Int64> arc) //Bogus
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(0 == fst.Outputs.NoOutput);
            long output = 0;
            var bytesReader = fst.GetBytesReader();

            fst.GetFirstArc(arc);

            byte[] bytes = scratch.Bytes;
            int pos = scratch.Offset;
            int end = pos + scratch.Length;
            while (pos < end)
            {
                if (fst.FindTargetArc(bytes[pos++] & 0xff, arc, arc, bytesReader) is null)
                {
                    return null;
                }
                else
                {
                    output += arc.Output;
                }
            }

            return output;
        }

        /// <summary>
        /// Returns the weight associated with an input string,
        /// or null if it does not exist.
        /// </summary>
        public virtual long? Get(string key)
        {
            if (fst is null)
            {
                return null;
            }
            FST.Arc<Int64> arc = new FST.Arc<Int64>();
            long? result;
            try
            {
                result = LookupPrefix(new BytesRef(key), arc);
            }
            catch (Exception bogus) when (bogus.IsIOException())
            {
                throw RuntimeException.Create(bogus);
            }
            if (!result.HasValue || !arc.IsFinal)
            {
                return null;
            }
            else
            {
                return DecodeWeight(result.Value + arc.NextFinalOutput);
            }
        }

        /// <summary>
        /// cost -> weight </summary>
        private static int DecodeWeight(long encoded)
        {
            return (int)(int.MaxValue - encoded);
        }

        /// <summary>
        /// weight -> cost </summary>
        private static int EncodeWeight(long value)
        {
            if (value < 0 || value > int.MaxValue)
            {
                throw UnsupportedOperationException.Create("cannot encode value: " + value);
            }
            return int.MaxValue - (int)value;
        }

        private sealed class WFSTInputEnumerator : SortedInputEnumerator
        {
            internal WFSTInputEnumerator(IInputEnumerator source)
                : base(source)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(source.HasPayloads == false);
            }

            protected internal override void Encode(OfflineSorter.ByteSequencesWriter writer, ByteArrayDataOutput output, byte[] buffer, BytesRef spare, BytesRef payload, ICollection<BytesRef> contexts, long weight)
            {
                if (spare.Length + 4 >= buffer.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, spare.Length + 4);
                }
                output.Reset(buffer);
                output.WriteBytes(spare.Bytes, spare.Offset, spare.Length);
                output.WriteInt32(EncodeWeight(weight));
                writer.Write(buffer, 0, output.Position);
            }

            protected internal override long Decode(BytesRef scratch, ByteArrayDataInput tmpInput)
            {
                scratch.Length -= 4; // int
                // skip suggestion:
                tmpInput.Reset(scratch.Bytes, scratch.Offset + scratch.Length, 4);
                return tmpInput.ReadInt32();
            }
        }

        internal static readonly IComparer<Int64> weightComparer = Comparer<Int64>.Create((left, right) => Comparer<Int64>.Default.Compare(left, right));
        
        /// <summary>
        /// Returns byte size of the underlying FST. </summary>
        public override long GetSizeInBytes()
        {
            return (fst is null) ? 0 : fst.GetSizeInBytes();
        }

        public override long Count => count;
    }
}