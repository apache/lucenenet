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
    /// An FST <see cref="T:Outputs{BytesRef}"/> implementation where each output
    /// is a sequence of bytes.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class ByteSequenceOutputs : Outputs<BytesRef>
    {
        private static readonly BytesRef NO_OUTPUT = new BytesRef();
        private static readonly ByteSequenceOutputs singleton = new ByteSequenceOutputs();

        private ByteSequenceOutputs()
        {
        }

        public static ByteSequenceOutputs Singleton => singleton;

        public override BytesRef Common(BytesRef output1, BytesRef output2)
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
                if (output1.Bytes[pos1] != output2.Bytes[pos2])
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
                return new BytesRef(output1.Bytes, output1.Offset, pos1 - output1.Offset);
            }
        }

        public override BytesRef Subtract(BytesRef output, BytesRef inc)
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
                    Debugging.Assert(inc.Length < output.Length, "inc.length={0} vs output.length={1}", inc.Length, output.Length);
                    Debugging.Assert(inc.Length > 0);
                }
                return new BytesRef(output.Bytes, output.Offset + inc.Length, output.Length - inc.Length);
            }
        }

        public override BytesRef Add(BytesRef prefix, BytesRef output)
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
                if (Debugging.AssertsEnabled) Debugging.Assert(prefix.Length > 0);
                if (Debugging.AssertsEnabled) Debugging.Assert(output.Length > 0);
                BytesRef result = new BytesRef(prefix.Length + output.Length);
                Arrays.Copy(prefix.Bytes, prefix.Offset, result.Bytes, 0, prefix.Length);
                Arrays.Copy(output.Bytes, output.Offset, result.Bytes, prefix.Length, output.Length);
                result.Length = prefix.Length + output.Length;
                return result;
            }
        }

        public override void Write(BytesRef prefix, DataOutput @out)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(prefix != null);
            @out.WriteVInt32(prefix.Length);
            @out.WriteBytes(prefix.Bytes, prefix.Offset, prefix.Length);
        }

        public override BytesRef Read(DataInput @in)
        {
            int len = @in.ReadVInt32();
            if (len == 0)
            {
                return NO_OUTPUT;
            }
            else
            {
                BytesRef output = new BytesRef(len);
                @in.ReadBytes(output.Bytes, 0, len);
                output.Length = len;
                return output;
            }
        }

        public override BytesRef NoOutput => NO_OUTPUT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string OutputToString(BytesRef output)
        {
            return output.ToString();
        }
    }
}