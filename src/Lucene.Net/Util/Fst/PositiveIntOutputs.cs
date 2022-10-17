using Lucene.Net.Diagnostics;
using System;
using System.Globalization;
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

    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// An FST <see cref="Outputs{T}"/> implementation where each output
    /// is a non-negative <see cref="Int64"/> value.
    /// <para/>
    /// NOTE: This was PositiveIntOutputs in Lucene
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class PositiveInt32Outputs : Outputs<Int64>
    {
        private static readonly Int64 NO_OUTPUT = Int64.GetInstance(0);

        private static readonly PositiveInt32Outputs singleton = new PositiveInt32Outputs();

        private PositiveInt32Outputs()
        {
        }

        public static PositiveInt32Outputs Singleton => singleton;

        public override Int64 Common(Int64 output1, Int64 output2)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(output1));
                Debugging.Assert(Valid(output2));
            }
            if (output1 == NO_OUTPUT || output2 == NO_OUTPUT)
            {
                return NO_OUTPUT;
            }
            else
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(output1 > 0);
                    Debugging.Assert(output2 > 0);
                }
                return Math.Min(output1, output2);
            }
        }

        public override Int64 Subtract(Int64 output, Int64 inc)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(output));
                Debugging.Assert(Valid(inc));
                Debugging.Assert(output >= inc);
            }

            if (inc == NO_OUTPUT)
            {
                return output;
            }
            else if (output.Equals(inc))
            {
                return NO_OUTPUT;
            }
            else
            {
                return output - inc;
            }
        }

        public override Int64 Add(Int64 prefix, Int64 output)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(prefix));
                Debugging.Assert(Valid(output));
            }
            if (prefix == NO_OUTPUT)
            {
                return output;
            }
            else if (output == NO_OUTPUT)
            {
                return prefix;
            }
            else
            {
                return prefix + output;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(Int64 output, DataOutput @out)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Valid(output));
            @out.WriteVInt64(output);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Int64 Read(DataInput @in)
        {
            long v = @in.ReadVInt64();
            if (v == 0)
            {
                return NO_OUTPUT;
            }
            else
            {
                return v;
            }
        }

        private static bool Valid(Int64 o) // LUCENENET: CA1822: Mark members as static
        {
            Debugging.Assert(o != null, "PositiveIntOutput precondition fail");
            Debugging.Assert(o == NO_OUTPUT || o > 0,"o={0}", o);
            return true;
        }

        public override Int64 NoOutput => NO_OUTPUT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string OutputToString(Int64 output)
        {
            return output.ToString(NumberFormatInfo.InvariantInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return "PositiveIntOutputs";
        }
    }
}