using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
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

    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// An FST <see cref="Outputs{T}"/> implementation where each output
    /// is a sequence of <see cref="int"/>s.
    /// <para/>
    /// NOTE: This was IntSequenceOutputs in Lucene
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class Int32SequenceOutputs : Outputs<Int32sRef>
    {
        private static readonly Int32sRef NO_OUTPUT = new Int32sRef();
        private static readonly Int32SequenceOutputs singleton = new Int32SequenceOutputs();

        private Int32SequenceOutputs()
        {
        }

        public static Int32SequenceOutputs Singleton => singleton;

        public override Int32sRef Common(Int32sRef output1, Int32sRef output2)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(output1 != null);
                Debugging.Assert(output2 != null);
            }

            int pos1 = output1.Offset;
            int pos2 = output2.Offset;
            int stopAt1 = pos1 + Math.Min(output1.Length, output2.Length);
            while (pos1 < stopAt1)
            {
                if (output1.Int32s[pos1] != output2.Int32s[pos2])
                {
                    break;
                }
                pos1++;
                pos2++;
            }

            if (pos1 == output1.Offset)
            {
                // no common prefix
                return NO_OUTPUT;
            }
            else if (pos1 == output1.Offset + output1.Length)
            {
                // output1 is a prefix of output2
                return output1;
            }
            else if (pos2 == output2.Offset + output2.Length)
            {
                // output2 is a prefix of output1
                return output2;
            }
            else
            {
                return new Int32sRef(output1.Int32s, output1.Offset, pos1 - output1.Offset);
            }
        }

        public override Int32sRef Subtract(Int32sRef output, Int32sRef inc)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(output != null);
                Debugging.Assert(inc != null);
            }
            if (inc == NO_OUTPUT)
            {
                // no prefix removed
                return output;
            }
            else if (inc.Length == output.Length)
            {
                // entire output removed
                return NO_OUTPUT;
            }
            else
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(inc.Length < output.Length,"inc.length={0} vs output.length={1}", inc.Length, output.Length);
                    Debugging.Assert(inc.Length > 0);
                }
                return new Int32sRef(output.Int32s, output.Offset + inc.Length, output.Length - inc.Length);
            }
        }

        public override Int32sRef Add(Int32sRef prefix, Int32sRef output)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(prefix != null);
                Debugging.Assert(output != null);
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
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(prefix.Length > 0);
                    Debugging.Assert(output.Length > 0);
                }
                Int32sRef result = new Int32sRef(prefix.Length + output.Length);
                Arrays.Copy(prefix.Int32s, prefix.Offset, result.Int32s, 0, prefix.Length);
                Arrays.Copy(output.Int32s, output.Offset, result.Int32s, prefix.Length, output.Length);
                result.Length = prefix.Length + output.Length;
                return result;
            }
        }

        public override void Write(Int32sRef prefix, DataOutput @out)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(prefix != null);
            @out.WriteVInt32(prefix.Length);
            for (int idx = 0; idx < prefix.Length; idx++)
            {
                @out.WriteVInt32(prefix.Int32s[prefix.Offset + idx]);
            }
        }

        public override Int32sRef Read(DataInput @in)
        {
            int len = @in.ReadVInt32();
            if (len == 0)
            {
                return NO_OUTPUT;
            }
            else
            {
                Int32sRef output = new Int32sRef(len);
                for (int idx = 0; idx < len; idx++)
                {
                    output.Int32s[idx] = @in.ReadVInt32();
                }
                output.Length = len;
                return output;
            }
        }

        public override Int32sRef NoOutput => NO_OUTPUT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string OutputToString(Int32sRef output)
        {
            return output.ToString();
        }
    }
}