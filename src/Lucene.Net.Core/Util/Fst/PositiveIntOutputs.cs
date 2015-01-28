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
    /// is a non-negative long value.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class PositiveIntOutputs : Outputs<long?>
    {
        private static readonly long NO_OUTPUT = new long();

        private static readonly PositiveIntOutputs singleton = new PositiveIntOutputs();

        private PositiveIntOutputs()
        {
        }

        public static PositiveIntOutputs Singleton
        {
            get
            {
                return singleton;
            }
        }

        public override long? Common(long? output1, long? output2)
        {
            Debug.Assert(Valid(output1));
            Debug.Assert(Valid(output2));
            if (output1 == NO_OUTPUT || output2 == NO_OUTPUT)
            {
                return NO_OUTPUT;
            }
            else
            {
                Debug.Assert(output1 > 0);
                Debug.Assert(output2 > 0);
                return Math.Min(output1.Value, output2.Value);
            }
        }

        public override long? Subtract(long? output, long? inc)
        {
            Debug.Assert(Valid(output));
            Debug.Assert(Valid(inc));
            Debug.Assert(output >= inc);

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

        public override long? Add(long? prefix, long? output)
        {
            Debug.Assert(Valid(prefix));
            Debug.Assert(Valid(output));
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

        public override void Write(long? output, DataOutput @out)
        {
            Debug.Assert(Valid(output));
            @out.WriteVLong(output.Value);
        }

        public override long? Read(DataInput @in)
        {
            long v = @in.ReadVLong();
            if (v == 0)
            {
                return NO_OUTPUT;
            }
            else
            {
                return v;
            }
        }

        private bool Valid(long? o)
        {
            Debug.Assert(o != null, "PositiveIntOutput precondition fail");
            Debug.Assert(o == NO_OUTPUT || o > 0, "o=" + o);
            return true;
        }

        public override long? NoOutput
        {
            get
            {
                return NO_OUTPUT;
            }
        }

        public override string OutputToString(long? output)
        {
            return output.ToString();
        }

        public override string ToString()
        {
            return "PositiveIntOutputs";
        }
    }
}