using Lucene.Net.Store;
using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
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
    /// An FST <see cref="Outputs{T}"/> implementation where each output
    /// is one or two non-negative long values.  If it's a
    /// <see cref="float"/> output, <see cref="Nullable{Int64}"/> is 
    /// returned; else, TwoLongs.  Order
    /// is preserved in the TwoLongs case, ie .first is the first
    /// input/output added to <see cref="Builder{T}"/>, and .second is the
    /// second.  You cannot store 0 output with this (that's
    /// reserved to mean "no output")!
    /// 
    /// <para>NOTE: the only way to create a TwoLongs output is to
    /// add the same input to the FST twice in a row.  This is
    /// how the FST maps a single input to two outputs (e.g. you
    /// cannot pass a <see cref="TwoLongs"/> to <see cref="Builder.Add(IntsRef, T)"/>.  If you
    /// need more than two then use <see cref="ListOfOutputs{T}"/>, but if
    /// you only have at most 2 then this implementation will
    /// require fewer bytes as it steals one bit from each long
    /// value.
    /// 
    /// </para>
    /// <para>NOTE: the resulting FST is not guaranteed to be minimal!
    /// See <see cref="Builder{T}"/>.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public sealed class UpToTwoPositiveIntOutputs : Outputs<object>
    {

        /// <summary>
        /// Holds two long outputs. </summary>
        public sealed class TwoLongs
        {
            public long First
            {
                get { return first; }
            }
            private readonly long first;

            public long Second
            {
                get { return second; }
            }
            private readonly long second;

            public TwoLongs(long first, long second)
            {
                this.first = first;
                this.second = second;
                Debug.Assert(first >= 0);
                Debug.Assert(second >= 0);
            }

            public override string ToString()
            {
                return "TwoLongs:" + first + "," + second;
            }

            public override bool Equals(object other)
            {
                if (other is TwoLongs)
                {
                    TwoLongs other2 = (TwoLongs)other;
                    return first == other2.first && second == other2.second;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return (int)((first ^ ((long)((ulong)first >> 32))) ^ (second ^ (second >> 32)));
            }
        }

        private static readonly long? NO_OUTPUT = new long?(0);

        private readonly bool doShare;

        private static readonly UpToTwoPositiveIntOutputs singletonShare = new UpToTwoPositiveIntOutputs(true);
        private static readonly UpToTwoPositiveIntOutputs singletonNoShare = new UpToTwoPositiveIntOutputs(false);

        private UpToTwoPositiveIntOutputs(bool doShare)
        {
            this.doShare = doShare;
        }

        public static UpToTwoPositiveIntOutputs GetSingleton(bool doShare)
        {
            return doShare ? singletonShare : singletonNoShare;
        }

        public long? Get(long v)
        {
            if (v == 0)
            {
                return NO_OUTPUT;
            }
            else
            {
                return Convert.ToInt64(v);
            }
        }

        public TwoLongs Get(long first, long second)
        {
            return new TwoLongs(first, second);
        }

        public override object Common(object output1, object output2)
        {
            Debug.Assert(Valid(output1, false));
            Debug.Assert(Valid(output2, false));
            long? output1_ = (long?)output1;
            long? output2_ = (long?)output2;
            if (output1_ == NO_OUTPUT || output2_ == NO_OUTPUT)
            {
                return NO_OUTPUT;
            }
            else if (doShare)
            {
                Debug.Assert(output1_ > 0);
                Debug.Assert(output2_ > 0);
                return Math.Min(output1_.GetValueOrDefault(), output2_.GetValueOrDefault());
            }
            else if (output1_.Equals(output2_))
            {
                return output1_;
            }
            else
            {
                return NO_OUTPUT;
            }
        }

        public override object Subtract(object output, object inc)
        {
            Debug.Assert(Valid(output, false));
            Debug.Assert(Valid(inc, false));
            long? output2 = (long?)output;
            long? inc2 = (long?)inc;
            Debug.Assert(output2 >= inc2);

            if (inc2 == NO_OUTPUT)
            {
                return output2;
            }
            else if (output2.Equals(inc2))
            {
                return NO_OUTPUT;
            }
            else
            {
                return output2 - inc2;
            }
        }

        public override object Add(object prefix, object output)
        {
            Debug.Assert(Valid(prefix, false));
            Debug.Assert(Valid(output, true));
            long? prefix2 = (long?)prefix;
            if (output is long?)
            {
                long? output2 = (long?)output;
                if (prefix2 == NO_OUTPUT)
                {
                    return output2;
                }
                else if (output2 == NO_OUTPUT)
                {
                    return prefix2;
                }
                else
                {
                    return prefix2 + output2;
                }
            }
            else
            {
                TwoLongs output3 = (TwoLongs)output;
                long v = prefix2.Value;
                return new TwoLongs(output3.First + v, output3.Second + v);
            }
        }

        public override void Write(object output, DataOutput @out)
        {
            Debug.Assert(Valid(output, true));
            if (output is long?)
            {
                long? output2 = (long?)output;
                @out.WriteVLong(output2.GetValueOrDefault() << 1);
            }
            else
            {
                TwoLongs output3 = (TwoLongs)output;
                @out.WriteVLong((output3.First << 1) | 1);
                @out.WriteVLong(output3.Second);
            }
        }

        public override object Read(DataInput @in)
        {
            long code = @in.ReadVLong();
            if ((code & 1) == 0)
            {
                // single long
                long v = (long)((ulong)code >> 1);
                if (v == 0)
                {
                    return NO_OUTPUT;
                }
                else
                {
                    return Convert.ToInt64(v);
                }
            }
            else
            {
                // two longs
                long first = (long)((ulong)code >> 1);
                long second = @in.ReadVLong();
                return new TwoLongs(first, second);
            }
        }

        private bool Valid(long? o)
        {
            Debug.Assert(o != null);
            Debug.Assert(o is long?);
            Debug.Assert(o == NO_OUTPUT || o > 0);
            return true;
        }

        // Used only by assert
        private bool Valid(object o, bool allowDouble)
        {
            if (!allowDouble)
            {
                Debug.Assert(o is long?);
                return Valid((long?)o);
            }
            else if (o is TwoLongs)
            {
                return true;
            }
            else
            {
                return Valid((long?)o);
            }
        }

        public override object NoOutput
        {
            get
            {
                return NO_OUTPUT;
            }
        }

        public override string OutputToString(object output)
        {
            return output.ToString();
        }

        public override object Merge(object first, object second)
        {
            Debug.Assert(Valid(first, false));
            Debug.Assert(Valid(second, false));
            return new TwoLongs(((long?)first).GetValueOrDefault(), ((long?)second).GetValueOrDefault());
        }
    }
}