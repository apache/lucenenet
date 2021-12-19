using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using System;
using System.Runtime.CompilerServices;
using Int64 = J2N.Numerics.Int64;

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
    /// returned; else, <see cref="TwoInt64s"/>.  Order
    /// is preserved in the <see cref="TwoInt64s"/> case, ie .first is the first
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
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(first >= 0);
                    Debugging.Assert(second >= 0);
                }
            }

            public override string ToString()
            {
                return "TwoLongs:" + first + "," + second;
            }

            public override bool Equals(object other)
            {
                if (other is TwoInt64s other2)
                {
                    return first == other2.first && second == other2.second;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return (int)((first ^ (first.TripleShift(32))) ^ (second ^ (second >> 32)));
            }
        }

        private static readonly Int64 NO_OUTPUT = Int64.GetInstance(0);

        private readonly bool doShare;

        private static readonly UpToTwoPositiveInt64Outputs singletonShare = new UpToTwoPositiveInt64Outputs(true);
        private static readonly UpToTwoPositiveInt64Outputs singletonNoShare = new UpToTwoPositiveInt64Outputs(false);

        private UpToTwoPositiveInt64Outputs(bool doShare)
        {
            this.doShare = doShare;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UpToTwoPositiveInt64Outputs GetSingleton(bool doShare)
        {
            return doShare ? singletonShare : singletonNoShare;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a shipped public API")]
        public Int64 Get(long v)
        {
            return v == 0 ? NO_OUTPUT : v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a shipped public API")]
        public TwoInt64s Get(long first, long second)
        {
            return new TwoInt64s(first, second);
        }

        public override object Common(object output1, object output2)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(output1, false));
                Debugging.Assert(Valid(output2, false));
            }
            Int64 output1_ = (Int64)output1;
            Int64 output2_ = (Int64)output2;
            if (output1_ == NO_OUTPUT || output2_ == NO_OUTPUT)
            {
                return NO_OUTPUT;
            }
            else if (doShare)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(output1_ > 0);
                    Debugging.Assert(output2_ > 0);
                }
                return Int64.GetInstance(Math.Min(output1_, output2_));
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
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(output, false));
                Debugging.Assert(Valid(inc, false));
            }
            Int64 output2 = (Int64)output;
            Int64 inc2 = (Int64)inc;
            if (Debugging.AssertsEnabled) Debugging.Assert(output2 >= inc2);

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
                return Int64.GetInstance(output2 - inc2);
            }
        }

        public override object Add(object prefix, object output)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Valid(prefix, false));
            if (Debugging.AssertsEnabled) Debugging.Assert(Valid(output, true));
            Int64 prefix2 = (Int64)prefix;
            if (output is Int64 output2)
            {
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
                    return Int64.GetInstance(prefix2 + output2);
                }
            }
            else
            {
                TwoInt64s output3 = (TwoInt64s)output;
                long v = prefix2;
                return new TwoInt64s(output3.First + v, output3.Second + v);
            }
        }

        public override void Write(object output, DataOutput @out)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Valid(output, true));
            if (output is Int64 output2)
            {
                @out.WriteVInt64(output2 << 1);
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
                long v = code.TripleShift(1);
                if (v == 0)
                {
                    return NO_OUTPUT;
                }
                else
                {
                    return Int64.GetInstance(v);
                }
            }
            else
            {
                // two longs
                long first = code.TripleShift(1);
                long second = @in.ReadVInt64();
                return new TwoInt64s(first, second);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Valid(Int64 o) // LUCENENET: CA1822: Mark members as static
        {
            Debugging.Assert(o != null);
            Debugging.Assert(o is Int64);
            Debugging.Assert(o == NO_OUTPUT || o > 0);
            return true;
        }

        // Used only by assert
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Valid(object o, bool allowDouble) // LUCENENET: CA1822: Mark members as static
        {
            if (!allowDouble)
            {
                Debugging.Assert(o is Int64);
                return Valid((Int64)o);
            }
            else if (o is TwoInt64s)
            {
                return true;
            }
            else
            {
                return Valid((Int64)o);
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
                Debugging.Assert(Valid(first, false));
                Debugging.Assert(Valid(second, false));
            }
            return new TwoInt64s((Int64)first, (Int64)second);
        }
    }
}