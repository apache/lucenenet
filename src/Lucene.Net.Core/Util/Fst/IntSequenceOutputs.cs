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

    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// An FST <seealso cref="Outputs"/> implementation where each output
    /// is a sequence of ints.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class IntSequenceOutputs : Outputs<IntsRef>
    {
        private static readonly IntsRef NO_OUTPUT = new IntsRef();
        private static readonly IntSequenceOutputs singleton = new IntSequenceOutputs();

        private IntSequenceOutputs()
        {
        }

        public static IntSequenceOutputs Singleton
        {
            get
            {
                return singleton;
            }
        }

        public override IntsRef Common(IntsRef output1, IntsRef output2)
        {
            Debug.Assert(output1 != null);
            Debug.Assert(output2 != null);

            int pos1 = output1.Offset;
            int pos2 = output2.Offset;
            int stopAt1 = pos1 + Math.Min(output1.Length, output2.Length);
            while (pos1 < stopAt1)
            {
                if (output1.Ints[pos1] != output2.Ints[pos2])
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
                return new IntsRef(output1.Ints, output1.Offset, pos1 - output1.Offset);
            }
        }

        public override IntsRef Subtract(IntsRef output, IntsRef inc)
        {
            Debug.Assert(output != null);
            Debug.Assert(inc != null);
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
                Debug.Assert(inc.Length < output.Length, "inc.length=" + inc.Length + " vs output.length=" + output.Length);
                Debug.Assert(inc.Length > 0);
                return new IntsRef(output.Ints, output.Offset + inc.Length, output.Length - inc.Length);
            }
        }

        public override IntsRef Add(IntsRef prefix, IntsRef output)
        {
            Debug.Assert(prefix != null);
            Debug.Assert(output != null);
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
                Debug.Assert(prefix.Length > 0);
                Debug.Assert(output.Length > 0);
                IntsRef result = new IntsRef(prefix.Length + output.Length);
                Array.Copy(prefix.Ints, prefix.Offset, result.Ints, 0, prefix.Length);
                Array.Copy(output.Ints, output.Offset, result.Ints, prefix.Length, output.Length);
                result.Length = prefix.Length + output.Length;
                return result;
            }
        }

        public override void Write(IntsRef prefix, DataOutput @out)
        {
            Debug.Assert(prefix != null);
            @out.WriteVInt(prefix.Length);
            for (int idx = 0; idx < prefix.Length; idx++)
            {
                @out.WriteVInt(prefix.Ints[prefix.Offset + idx]);
            }
        }

        public override IntsRef Read(DataInput @in)
        {
            int len = @in.ReadVInt();
            if (len == 0)
            {
                return NO_OUTPUT;
            }
            else
            {
                IntsRef output = new IntsRef(len);
                for (int idx = 0; idx < len; idx++)
                {
                    output.Ints[idx] = @in.ReadVInt();
                }
                output.Length = len;
                return output;
            }
        }

        public override IntsRef NoOutput
        {
            get
            {
                return NO_OUTPUT;
            }
        }

        public override string OutputToString(IntsRef output)
        {
            return output.ToString();
        }
    }
}