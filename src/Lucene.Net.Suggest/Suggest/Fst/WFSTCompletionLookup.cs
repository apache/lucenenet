using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
        /// FST<Long>, weights are encoded as costs: (Integer.MAX_VALUE-weight)
        /// </summary>
        // NOTE: like FSTSuggester, this is really a WFSA, if you want to
        // customize the code to add some output you should use PairOutputs.
        private FST<long?> fst = null;

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

        public override void Build(IInputIterator iterator)
        {
            if (iterator.HasPayloads)
            {
                throw new ArgumentException("this suggester doesn't support payloads");
            }
            if (iterator.HasContexts)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }
            count = 0;
            var scratch = new BytesRef();
            IInputIterator iter = new WFSTInputIterator(this, iterator);
            var scratchInts = new IntsRef();
            BytesRef previous = null;
            var outputs = PositiveIntOutputs.Singleton;
            var builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);
            while ((scratch = iter.Next()) != null)
            {
                long cost = iter.Weight;

                if (previous == null)
                {
                    previous = new BytesRef();
                }
                else if (scratch.Equals(previous))
                {
                    continue; // for duplicate suggestions, the best weight is actually
                    // added
                }
                Lucene.Net.Util.Fst.Util.ToIntsRef(scratch, scratchInts);
                builder.Add(scratchInts, cost);
                previous.CopyBytes(scratch);
                count++;
            }
            fst = builder.Finish();
        }

        public override bool Store(DataOutput output)
        {
            output.WriteVLong(count);
            if (fst == null)
            {
                return false;
            }
            fst.Save(output);
            return true;
        }

        public override bool Load(DataInput input)
        {
            count = input.ReadVLong();
            this.fst = new FST<long?>(input, PositiveIntOutputs.Singleton);
            return true;
        }

        public override List<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            if (contexts != null)
            {
                throw new System.ArgumentException("this suggester doesn't support contexts");
            }
            Debug.Assert(num > 0);

            if (onlyMorePopular)
            {
                throw new System.ArgumentException("this suggester only works with onlyMorePopular=false");
            }

            if (fst == null)
            {
                return new List<LookupResult>();
            }

            BytesRef scratch = new BytesRef(key);
            int prefixLength = scratch.Length;
            FST.Arc<long?> arc = new FST.Arc<long?>();

            // match the prefix portion exactly
            long? prefixOutput = null;
            try
            {
                prefixOutput = LookupPrefix(scratch, arc);
            }
            catch (IOException bogus)
            {
                throw new Exception(bogus.Message, bogus);
            }

            if (prefixOutput == null)
            {
                return new List<LookupResult>();
            }

            List<LookupResult> results = new List<LookupResult>(num);
            CharsRef spare = new CharsRef();
            if (exactFirst && arc.IsFinal)
            {
                spare.Grow(scratch.Length);
                UnicodeUtil.UTF8toUTF16(scratch, spare);
                results.Add(new LookupResult(spare.ToString(), DecodeWeight(prefixOutput.GetValueOrDefault() + arc.NextFinalOutput.GetValueOrDefault())));
                if (--num == 0)
                {
                    return results; // that was quick
                }
            }

            // complete top-N
            Util.Fst.Util.TopResults<long?> completions = null;
            try
            {
                completions = Lucene.Net.Util.Fst.Util.ShortestPaths(fst, arc, prefixOutput, weightComparer, num, !exactFirst);
                Debug.Assert(completions.IsComplete);
            }
            catch (IOException bogus)
            {
                throw new Exception(bogus.Message, bogus);
            }

            BytesRef suffix = new BytesRef(8);
            foreach (Util.Fst.Util.Result<long?> completion in completions)
            {
                scratch.Length = prefixLength;
                // append suffix
                Lucene.Net.Util.Fst.Util.ToBytesRef(completion.Input, suffix);
                scratch.Append(suffix);
                spare.Grow(scratch.Length);
                UnicodeUtil.UTF8toUTF16(scratch, spare);
                results.Add(new LookupResult(spare.ToString(), DecodeWeight(completion.Output.GetValueOrDefault())));
            }
            return results;
        }

        private long? LookupPrefix(BytesRef scratch, FST.Arc<long?> arc) //Bogus
        {
            Debug.Assert(0 == (long)fst.Outputs.NoOutput);
            long output = 0;
            var bytesReader = fst.GetBytesReader();

            fst.GetFirstArc(arc);

            byte[] bytes = scratch.Bytes;
            int pos = scratch.Offset;
            int end = pos + scratch.Length;
            while (pos < end)
            {
                if (fst.FindTargetArc(bytes[pos++] & 0xff, arc, arc, bytesReader) == null)
                {
                    return null;
                }
                else
                {
                    output += (long)arc.Output;
                }
            }

            return output;
        }

        /// <summary>
        /// Returns the weight associated with an input string,
        /// or null if it does not exist.
        /// </summary>
        public virtual object Get(string key)
        {
            if (fst == null)
            {
                return null;
            }
            FST.Arc<long?> arc = new FST.Arc<long?>();
            long? result = null;
            try
            {
                result = LookupPrefix(new BytesRef(key), arc);
            }
            catch (IOException bogus)
            {
                throw new Exception(bogus.Message, bogus);
            }
            if (result == null || !arc.IsFinal)
            {
                return null;
            }
            else
            {
                return Convert.ToInt32(DecodeWeight(result.GetValueOrDefault() + arc.NextFinalOutput.GetValueOrDefault()));
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
                throw new System.NotSupportedException("cannot encode value: " + value);
            }
            return int.MaxValue - (int)value;
        }

        private sealed class WFSTInputIterator : SortedInputIterator
        {
            private readonly WFSTCompletionLookup outerInstance;


            internal WFSTInputIterator(WFSTCompletionLookup outerInstance, IInputIterator source)
                : base(source)
            {
                this.outerInstance = outerInstance;
                Debug.Assert(source.HasPayloads == false);
            }

            protected internal override void Encode(OfflineSorter.ByteSequencesWriter writer, ByteArrayDataOutput output, byte[] buffer, BytesRef spare, BytesRef payload, IEnumerable<BytesRef> contexts, long weight)
            {
                if (spare.Length + 4 >= buffer.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, spare.Length + 4);
                }
                output.Reset(buffer);
                output.WriteBytes(spare.Bytes, spare.Offset, spare.Length);
                output.WriteInt(EncodeWeight(weight));
                writer.Write(buffer, 0, output.Position);
            }

            protected internal override long Decode(BytesRef scratch, ByteArrayDataInput tmpInput)
            {
                scratch.Length -= 4; // int
                // skip suggestion:
                tmpInput.Reset(scratch.Bytes, scratch.Offset + scratch.Length, 4);
                return tmpInput.ReadInt();
            }
        }

        internal static readonly IComparer<long?> weightComparer = new ComparerAnonymousInnerClassHelper();

        private class ComparerAnonymousInnerClassHelper : IComparer<long?>
        {
            public ComparerAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(long? left, long? right)
            {
                return Comparer<long?>.Default.Compare(left, right);
            }
        }

        /// <summary>
        /// Returns byte size of the underlying FST. </summary>
        public override long GetSizeInBytes()
        {
            return (fst == null) ? 0 : fst.SizeInBytes();
        }

        public override long Count
        {
            get
            {
                return count;
            }
        }
    }
}