// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections;
using System.Collections.Generic;

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
    /// <see cref="ValueSource"/> implementation which only returns the values from the provided
    /// <see cref="ValueSource"/>s which are available for a particular docId.  Consequently, when combined
    /// with a <see cref="ConstValueSource"/>, this function serves as a way to return a default
    /// value when the values for a field are unavailable.
    /// </summary>
    public class DefFunction : MultiFunction
    {
        public DefFunction(IList<ValueSource> sources)
            : base(sources)
        {
        }

        protected override string Name => "def";

        public override FunctionValues GetValues(IDictionary fcontext, AtomicReaderContext readerContext)
        {
            return new ValuesAnonymousClass(this, ValsArr(m_sources, fcontext, readerContext));
        }

        private sealed class ValuesAnonymousClass : Values
        {
            public ValuesAnonymousClass(DefFunction outerInstance, FunctionValues[] valsArr)
                : base(outerInstance, valsArr)
            {
                upto = valsArr.Length - 1;
            }

            private readonly int upto;

            private FunctionValues Get(int doc)
            {
                for (int i = 0; i < upto; i++)
                {
                    FunctionValues vals = valsArr[i];
                    if (vals.Exists(doc))
                    {
                        return vals;
                    }
                }
                return valsArr[upto];
            }

            public override byte ByteVal(int doc)
            {
                return Get(doc).ByteVal(doc);
            }

            /// <summary>
            /// NOTE: This was shortVal() in Lucene
            /// </summary>
            public override short Int16Val(int doc)
            {
                return Get(doc).Int16Val(doc);
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return Get(doc).SingleVal(doc);
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return Get(doc).Int32Val(doc);
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return Get(doc).Int64Val(doc);
            }

            public override double DoubleVal(int doc)
            {
                return Get(doc).DoubleVal(doc);
            }

            public override string StrVal(int doc)
            {
                return Get(doc).StrVal(doc);
            }

            public override bool BoolVal(int doc)
            {
                return Get(doc).BoolVal(doc);
            }

            public override bool BytesVal(int doc, BytesRef target)
            {
                return Get(doc).BytesVal(doc, target);
            }

            public override object ObjectVal(int doc)
            {
                return Get(doc).ObjectVal(doc);
            }

            public override bool Exists(int doc)
            {
                // return true if any source is exists?
                foreach (FunctionValues vals in valsArr)
                {
                    if (vals.Exists(doc))
                        return true;
                }
                return false;
            }

            public override ValueFiller GetValueFiller()
            {
                // TODO: need ValueSource.type() to determine correct type
                return base.GetValueFiller();
            }
        }
    }
}