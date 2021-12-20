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
    /// An FST <see cref="Outputs{T}"/> implementation, holding two other outputs.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class PairOutputs<A, B> : Outputs<PairOutputs<A, B>.Pair>
        where A : class // LUCENENET specific - added class constraints because we compare reference equality
        where B : class
    {
        private readonly Pair NO_OUTPUT;
        private readonly Outputs<A> outputs1;
        private readonly Outputs<B> outputs2;

        /// <summary>
        /// Holds a single pair of two outputs. </summary>
        public class Pair
        {
            public A Output1 { get; private set; }
            public B Output2 { get; private set; }

            // use newPair
            internal Pair(A output1, B output2)
            {
                this.Output1 = output1;
                this.Output2 = output2;
            }

            public override bool Equals(object other)
            {
                // LUCENENET specific - simplified expression
                return ReferenceEquals(other, this) || (other is Pair pair && Output1.Equals(pair.Output1) && Output2.Equals(pair.Output2));
            }

            public override int GetHashCode()
            {
                return Output1.GetHashCode() + Output2.GetHashCode();
            }
        }

        public PairOutputs(Outputs<A> outputs1, Outputs<B> outputs2)
        {
            this.outputs1 = outputs1;
            this.outputs2 = outputs2;
            NO_OUTPUT = new Pair(outputs1.NoOutput, outputs2.NoOutput);
        }

        /// <summary>
        /// Create a new <see cref="Pair"/> </summary>
        public virtual Pair NewPair(A a, B b)
        {
            if (a.Equals(outputs1.NoOutput))
            {
                a = outputs1.NoOutput;
            }
            if (b.Equals(outputs2.NoOutput))
            {
                b = outputs2.NoOutput;
            }

            if (a == outputs1.NoOutput && b == outputs2.NoOutput)
            {
                return NO_OUTPUT;
            }
            else
            {
                var p = new Pair(a, b);
                if (Debugging.AssertsEnabled) Debugging.Assert(Valid(p));
                return p;
            }
        }

        // for assert
        private bool Valid(Pair pair)
        {
            bool noOutput1 = pair.Output1.Equals(outputs1.NoOutput);
            bool noOutput2 = pair.Output2.Equals(outputs2.NoOutput);

            if (noOutput1 && pair.Output1 != outputs1.NoOutput)
            {
                return false;
            }

            if (noOutput2 && pair.Output2 != outputs2.NoOutput)
            {
                return false;
            }

            if (noOutput1 && noOutput2)
            {
                if (pair != NO_OUTPUT)
                    return false;

                return true;
            }
            else
            {
                return true;
            }
        }

        public override Pair Common(Pair pair1, Pair pair2)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(pair1));
                Debugging.Assert(Valid(pair2));
            }
            return NewPair(outputs1.Common(pair1.Output1, pair2.Output1),
                           outputs2.Common(pair1.Output2, pair2.Output2));
        }

        public override Pair Subtract(Pair output, Pair inc)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(output));
                Debugging.Assert(Valid(inc));
            }
            return NewPair(outputs1.Subtract(output.Output1, inc.Output1),
                           outputs2.Subtract(output.Output2, inc.Output2));
        }

        public override Pair Add(Pair prefix, Pair output)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(prefix));
                Debugging.Assert(Valid(output));
            }
            return NewPair(outputs1.Add(prefix.Output1, output.Output1),
                           outputs2.Add(prefix.Output2, output.Output2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(Pair output, DataOutput writer)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Valid(output));
            outputs1.Write(output.Output1, writer);
            outputs2.Write(output.Output2, writer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Pair Read(DataInput @in)
        {
            A output1 = outputs1.Read(@in);
            B output2 = outputs2.Read(@in);
            return NewPair(output1, output2);
        }

        public override Pair NoOutput => NO_OUTPUT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string OutputToString(Pair output)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Valid(output));
            return "<pair:" + outputs1.OutputToString(output.Output1) + "," + outputs2.OutputToString(output.Output2) + ">";
        }

        public override string ToString()
        {
            return "PairOutputs<" + outputs1 + "," + outputs2 + ">";
        }
    }
}