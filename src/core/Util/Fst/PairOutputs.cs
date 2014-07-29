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
    /// An FST <seealso cref="Outputs"/> implementation, holding two other outputs.
    ///
    /// @lucene.experimental
    /// </summary>

    public class PairOutputs<A, B> : Outputs<PairOutputs<A, B>.Pair<A, B>>
    {
        private readonly Pair<A, B> NO_OUTPUT;
        private readonly Outputs<A> Outputs1;
        private readonly Outputs<B> Outputs2;

        /// <summary>
        /// Holds a single pair of two outputs. </summary>
        public class Pair<A, B>
        {
            public readonly A Output1;
            public readonly B Output2;

            // use newPair
            internal Pair(A output1, B output2)
            {
                this.Output1 = output1;
                this.Output2 = output2;
            }

            public override bool Equals(object other)
            {
                if (other == this)
                {
                    return true;
                }
                else if (other is Pair<A, B>)
                {
                    Pair<A, B> pair = (Pair<A, B>)other;
                    return Output1.Equals(pair.Output1) && Output2.Equals(pair.Output2);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return Output1.GetHashCode() + Output2.GetHashCode();
            }
        }

        public PairOutputs(Outputs<A> outputs1, Outputs<B> outputs2)
        {
            this.Outputs1 = outputs1;
            this.Outputs2 = outputs2;
            NO_OUTPUT = new Pair<A, B>(outputs1.NoOutput, outputs2.NoOutput);
        }

        /// <summary>
        /// Create a new Pair </summary>
        public virtual Pair<A, B> NewPair(A a, B b)
        {
            if (a.Equals(Outputs1.NoOutput))
            {
                a = Outputs1.NoOutput;
            }
            if (b.Equals(Outputs2.NoOutput))
            {
                b = Outputs2.NoOutput;
            }

            if ((object)a == (object)Outputs1.NoOutput && (object)b == (object)Outputs2.NoOutput)
            {
                return NO_OUTPUT;
            }
            else
            {
                Pair<A, B> p = new Pair<A, B>(a, b);
                Debug.Assert(Valid(p));
                return p;
            }
        }

        // for assert
        private bool Valid(Pair<A, B> pair)
        {
            bool noOutput1 = pair.Output1.Equals(Outputs1.NoOutput);
            bool noOutput2 = pair.Output2.Equals(Outputs2.NoOutput);

            if (noOutput1 && (object)pair.Output1 != (object)Outputs1.NoOutput)
            {
                return false;
            }

            if (noOutput2 && (object)pair.Output2 != (object)Outputs2.NoOutput)
            {
                return false;
            }

            if (noOutput1 && noOutput2)
            {
                if (pair != NO_OUTPUT)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        public override Pair<A, B> Common(Pair<A, B> pair1, Pair<A, B> pair2)
        {
            Debug.Assert(Valid(pair1));
            Debug.Assert(Valid(pair2));
            return NewPair(Outputs1.Common(pair1.Output1, pair2.Output1), Outputs2.Common(pair1.Output2, pair2.Output2));
        }

        public override Pair<A, B> Subtract(Pair<A, B> output, Pair<A, B> inc)
        {
            Debug.Assert(Valid(output));
            Debug.Assert(Valid(inc));
            return NewPair(Outputs1.Subtract(output.Output1, inc.Output1), Outputs2.Subtract(output.Output2, inc.Output2));
        }

        public override Pair<A, B> Add(Pair<A, B> prefix, Pair<A, B> output)
        {
            Debug.Assert(Valid(prefix));
            Debug.Assert(Valid(output));
            return NewPair(Outputs1.Add(prefix.Output1, output.Output1), Outputs2.Add(prefix.Output2, output.Output2));
        }

        public override void Write(Pair<A, B> output, DataOutput writer)
        {
            Debug.Assert(Valid(output));
            Outputs1.Write(output.Output1, writer);
            Outputs2.Write(output.Output2, writer);
        }

        public override Pair<A, B> Read(DataInput @in)
        {
            A output1 = Outputs1.Read(@in);
            B output2 = Outputs2.Read(@in);
            return NewPair(output1, output2);
        }

        public override Pair<A, B> NoOutput
        {
            get
            {
                return NO_OUTPUT;
            }
        }

        public override string OutputToString(Pair<A, B> output)
        {
            Debug.Assert(Valid(output));
            return "<pair:" + Outputs1.OutputToString(output.Output1) + "," + Outputs2.OutputToString(output.Output2) + ">";
        }

        public override string ToString()
        {
            return "PairOutputs<" + Outputs1 + "," + Outputs2 + ">";
        }
    }
}