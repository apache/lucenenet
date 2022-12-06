using Lucene.Net.Diagnostics;
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
    /// A null FST <see cref="Outputs{T}"/> implementation; use this if
    /// you just want to build an FSA.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class NoOutputs : Outputs<object>
    {
        internal static readonly object NO_OUTPUT = new ObjectAnonymousClass();

        private sealed class ObjectAnonymousClass : object
        {
            public ObjectAnonymousClass()
            {
            }

            /// <summary>
            /// NodeHash calls hashCode for this output; we fix this
            /// so we get deterministic hashing.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return 42;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object other)
            {
                return other == this;
            }
        }

        private static readonly NoOutputs singleton = new NoOutputs();

        private NoOutputs()
        {
        }

        public static NoOutputs Singleton => singleton;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object Common(object output1, object output2)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(output1 == NO_OUTPUT);
                Debugging.Assert(output2 == NO_OUTPUT);
            }
            return NO_OUTPUT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object Subtract(object output, object inc)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(output == NO_OUTPUT);
                Debugging.Assert(inc == NO_OUTPUT);
            }
            return NO_OUTPUT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object Add(object prefix, object output)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(prefix == NO_OUTPUT, "got {0}", prefix);
                Debugging.Assert(output == NO_OUTPUT);
            }
            return NO_OUTPUT;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object Merge(object first, object second)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(first == NO_OUTPUT);
                Debugging.Assert(second == NO_OUTPUT);
            }
            return NO_OUTPUT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(object prefix, DataOutput @out)
        {
            //assert false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object Read(DataInput @in)
        {
            //assert false;
            //return null;
            return NO_OUTPUT;
        }

        public override object NoOutput => NO_OUTPUT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string OutputToString(object output)
        {
            return "";
        }
    }
}