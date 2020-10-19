using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    /// cannot pass a <see cref="TwoInt64s"/> to <see cref="Builder{T}.Add(Int32sRef, T)"/>.  If you
    /// need more than two then use <see cref="ListOfOutputs{T}"/>, but if
    /// you only have at most 2 then this implementation will
    /// require fewer bytes as it steals one bit from each long
    /// value.
    /// 
    /// </para>
    /// <para>NOTE: the resulting FST is not guaranteed to be minimal!
    /// See <see cref="Builder{T}"/>.
    /// </para>
    /// <para>
    /// NOTE: This was UpToTwoPositiveIntOutputs in Lucene - the data type (int) was wrong there - it should have been long
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public sealed class UpToTwoPositiveInt64Outputs : Outputs<object>
    {
        /// <summary>
        /// Holds two long outputs.
        /// <para/>
        /// NOTE: This was TwoLongs in Lucene
        /// </summary>
        public sealed class TwoInt64s
        {
            public long First => first;
            private readonly long first;

            public long Second => second;
            private readonly long second;

            public TwoInt64s(long first, long second)
            {
                this.first = first;
                this.second = second;
                if(Debugging.ShouldAssert(first >= 0)) Debugging.ThrowAssert();

                if(Debugging.ShouldAssert(second >= 0)) Debugging.ThrowAssert();
            }

            public override string ToString()
            {
                return "TwoLongs:" + first + "," + second;
            }

            public override bool Equals(object other)
            {
                if (other is TwoInt64s)
                {
                    TwoInt64s other2 = (TwoInt64s)other;
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

        private static readonly UpToTwoPositiveInt64Outputs singletonShare = new UpToTwoPositiveInt64Outputs(true);
        private static readonly UpToTwoPositiveInt64Outputs singletonNoShare = new UpToTwoPositiveInt64Outputs(false);

        private UpToTwoPositiveInt64Outputs(bool doShare)
        {
            this.doShare = doShare;
        }

        public static UpToTwoPositiveInt64Outputs GetSingleton(bool doShare)
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
                return v;
            }
        }

        public TwoInt64s Get(long first, long second)
        {
            return new TwoInt64s(first, second);
        }

        public override object Common(object output1, object output2)
        {
            if(Debugging.ShouldAssert(Valid(output1, false))) Debugging.ThrowAssert();
            if(Debugging.ShouldAssert(Valid(output2, false))) Debugging.ThrowAssert();

            long? output1_ = (long?)output1;
            long? output2_ = (long?)output2;
            if (output1_ == NO_OUTPUT || output2_ == NO_OUTPUT)
            {
                return NO_OUTPUT;
            }
            else if (doShare)
            {
                if(Debugging.ShouldAssert(output1_ > 0)) Debugging.ThrowAssert();

                if(Debugging.ShouldAssert(output2_ > 0)) Debugging.ThrowAssert();
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
            if(Debugging.ShouldAssert(Valid(output, false))) Debugging.ThrowAssert();
            if(Debugging.ShouldAssert(Valid(inc, false))) Debugging.ThrowAssert();

            long? output2 = (long?)output;
            long? inc2 = (long?)inc;
            if (Debugging.AssertsEnabled && Debugging.ShouldAssert(output2 >= inc2)) Debugging.ThrowAssert();

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
            if (Debugging.AssertsEnabled && Debugging.ShouldAssert(Valid(prefix, false))) Debugging.ThrowAssert();
            if (Debugging.AssertsEnabled && Debugging.ShouldAssert(Valid(output, true))) Debugging.ThrowAssert();

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
                TwoInt64s output3 = (TwoInt64s)output;
                long v = prefix2.Value;
                return new TwoInt64s(output3.First + v, output3.Second + v);
            }
        }

        public override void Write(object output, DataOutput @out)
        {
            Debugging.ThrowAssertIf(Valid(output, true));
            if (output is long?)
            {
                long? output2 = (long?)output;
                @out.WriteVInt64(output2.GetValueOrDefault() << 1);
            }
            else
            {
                TwoInt64s output3 = (TwoInt64s)output;
                @out.WriteVInt64((output3.First << 1) | 1);
                @out.WriteVInt64(output3.Second);
            }
        }

        public override object Read(DataInput @in)
        {
            long code = @in.ReadVInt64();
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
                    return v;
                }
            }
            else
            {
                // two longs
                long first = (long)((ulong)code >> 1);
                long second = @in.ReadVInt64();
                return new TwoInt64s(first, second);
            }
        }

        private bool Valid(long? o)
        {
            Debugging.ThrowAssertIf(o != null);
            Debugging.ThrowAssertIf(o is long?);
            Debugging.ThrowAssertIf(o == NO_OUTPUT || o > 0);
            return true;
        }

        // Used only by assert
        private bool Valid(object o, bool allowDouble)
        {
            if (!allowDouble)
            {
                Debugging.ThrowAssertIf(o is long?);
                return Valid((long?)o);
            }
            else if (o is TwoInt64s)
            {
                return true;
            }
            else
            {
                return Valid((long?)o);
            }
        }

        public override object NoOutput => NO_OUTPUT;

        public override string OutputToString(object output)
        {
            return output.ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object Merge(object first, object second)
        {
            if (Debugging.AssertsEnabled)
            {
                if (Debugging.AssertsEnabled && Debugging.ShouldAssert(Valid(first, false))) Debugging.ThrowAssert();
                if (Debugging.AssertsEnabled && Debugging.ShouldAssert(Valid(second, false))) Debugging.ThrowAssert();
            }

            return new TwoInt64s(((long?)first).GetValueOrDefault(), ((long?)second).GetValueOrDefault());
        }
    }
}