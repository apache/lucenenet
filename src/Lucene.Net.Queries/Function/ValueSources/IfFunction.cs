// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System.Collections;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// Depending on the <see cref="bool"/> value of the <see cref="ifSource"/> function,
    /// returns the value of the <see cref="trueSource"/> or <see cref="falseSource"/> function.
    /// </summary>
    public class IfFunction : BoolFunction
    {
        private readonly ValueSource ifSource;
        private readonly ValueSource trueSource;
        private readonly ValueSource falseSource;

        public IfFunction(ValueSource ifSource, ValueSource trueSource, ValueSource falseSource)
        {
            this.ifSource = ifSource;
            this.trueSource = trueSource;
            this.falseSource = falseSource;
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues ifVals = ifSource.GetValues(context, readerContext);
            FunctionValues trueVals = trueSource.GetValues(context, readerContext);
            FunctionValues falseVals = falseSource.GetValues(context, readerContext);

            return new FunctionValuesAnonymousClass(ifVals, trueVals, falseVals);
        }

        private sealed class FunctionValuesAnonymousClass : FunctionValues
        {
            private readonly FunctionValues ifVals;
            private readonly FunctionValues trueVals;
            private readonly FunctionValues falseVals;

            public FunctionValuesAnonymousClass(FunctionValues ifVals, FunctionValues trueVals, FunctionValues falseVals)
            {
                this.ifVals = ifVals;
                this.trueVals = trueVals;
                this.falseVals = falseVals;
            }

            public override byte ByteVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.ByteVal(doc) : falseVals.ByteVal(doc);
            }

            /// <summary>
            /// NOTE: This was shortVal() in Lucene
            /// </summary>
            public override short Int16Val(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.Int16Val(doc) : falseVals.Int16Val(doc);
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.SingleVal(doc) : falseVals.SingleVal(doc);
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.Int32Val(doc) : falseVals.Int32Val(doc);
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.Int64Val(doc) : falseVals.Int64Val(doc);
            }

            public override double DoubleVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.DoubleVal(doc) : falseVals.DoubleVal(doc);
            }

            public override string StrVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.StrVal(doc) : falseVals.StrVal(doc);
            }

            public override bool BoolVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.BoolVal(doc) : falseVals.BoolVal(doc);
            }

            public override bool BytesVal(int doc, BytesRef target)
            {
                return ifVals.BoolVal(doc) ? trueVals.BytesVal(doc, target) : falseVals.BytesVal(doc, target);
            }

            public override object ObjectVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.ObjectVal(doc) : falseVals.ObjectVal(doc);
            }

            public override bool Exists(int doc)
            {
                return true; // TODO: flow through to any sub-sources?
            }

            public override ValueFiller GetValueFiller()
            {
                // TODO: we need types of trueSource / falseSource to handle this
                // for now, use float.
                return base.GetValueFiller();
            }

            public override string ToString(int doc)
            {
                return "if(" + ifVals.ToString(doc) + ',' + trueVals.ToString(doc) + ',' + falseVals.ToString(doc) + ')';
            }
        }

        public override string GetDescription()
        {
            return "if(" + ifSource.GetDescription() + ',' + trueSource.GetDescription() + ',' + falseSource + ')';
        }

        public override int GetHashCode()
        {
            int h = ifSource.GetHashCode();
            h = h * 31 + trueSource.GetHashCode();
            h = h * 31 + falseSource.GetHashCode();
            return h;
        }

        public override bool Equals(object o)
        {
            if (!(o is IfFunction other))
                return false;
            return ifSource.Equals(other.ifSource) && trueSource.Equals(other.trueSource) && falseSource.Equals(other.falseSource);
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            ifSource.CreateWeight(context, searcher);
            trueSource.CreateWeight(context, searcher);
            falseSource.CreateWeight(context, searcher);
        }
    }
}