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
    /// A null FST <seealso cref="Outputs"/> implementation; use this if
    /// you just want to build an FSA.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class NoOutputs : Outputs<object>
    {
        internal static readonly object NO_OUTPUT = new ObjectAnonymousInnerClassHelper();

        private class ObjectAnonymousInnerClassHelper : object
        {
            public ObjectAnonymousInnerClassHelper()
            {
            }

            // NodeHash calls hashCode for this output; we fix this
            // so we get deterministic hashing.
            public override int GetHashCode()
            {
                return 42;
            }

            public override bool Equals(object other)
            {
                return other == this;
            }
        }

        private static readonly NoOutputs singleton = new NoOutputs();

        private NoOutputs()
        {
        }

        public static NoOutputs Singleton
        {
            get
            {
                return singleton;
            }
        }

        public override object Common(object output1, object output2)
        {
            Debug.Assert(output1 == NO_OUTPUT);
            Debug.Assert(output2 == NO_OUTPUT);
            return NO_OUTPUT;
        }

        public override object Subtract(object output, object inc)
        {
            Debug.Assert(output == NO_OUTPUT);
            Debug.Assert(inc == NO_OUTPUT);
            return NO_OUTPUT;
        }

        public override object Add(object prefix, object output)
        {
            Debug.Assert(prefix == NO_OUTPUT, "got " + prefix);
            Debug.Assert(output == NO_OUTPUT);
            return NO_OUTPUT;
        }

        public override object Merge(object first, object second)
        {
            Debug.Assert(first == NO_OUTPUT);
            Debug.Assert(second == NO_OUTPUT);
            return NO_OUTPUT;
        }

        public override void Write(object prefix, DataOutput @out)
        {
            //assert false;
        }

        public override object Read(DataInput @in)
        {
            //assert false;
            //return null;
            return NO_OUTPUT;
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
            return "";
        }
    }
}