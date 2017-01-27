using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    /// <seealso cref="ValueSource"/> implementation which only returns the values from the provided
    /// ValueSources which are available for a particular docId.  Consequently, when combined
    /// with a <seealso cref="ConstValueSource"/>, this function serves as a way to return a default
    /// value when the values for a field are unavailable.
    /// </summary>
    public class DefFunction : MultiFunction
    {
        public DefFunction(IList<ValueSource> sources)
            : base(sources)
        {
        }

        protected override string Name
        {
            get { return "def"; }
        }

        public override FunctionValues GetValues(IDictionary fcontext, AtomicReaderContext readerContext)
        {
            return new ValuesAnonymousInnerClassHelper(this, ValsArr(sources, fcontext, readerContext));
        }

        private class ValuesAnonymousInnerClassHelper : Values
        {
            private readonly DefFunction outerInstance;

            public ValuesAnonymousInnerClassHelper(DefFunction outerInstance, FunctionValues[] valsArr)
                : base(outerInstance, valsArr)
            {
                this.outerInstance = outerInstance;
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

            public override sbyte ByteVal(int doc)
            {
                return Get(doc).ByteVal(doc);
            }

            public override short ShortVal(int doc)
            {
                return Get(doc).ShortVal(doc);
            }

            public override float FloatVal(int doc)
            {
                return Get(doc).FloatVal(doc);
            }

            public override int IntVal(int doc)
            {
                return Get(doc).IntVal(doc);
            }

            public override long LongVal(int doc)
            {
                return Get(doc).LongVal(doc);
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
                return valsArr.Any(vals => vals.Exists(doc));
            }

            public override AbstractValueFiller ValueFiller
            {
                get
                {
                    // TODO: need ValueSource.type() to determine correct type
                    return base.ValueFiller;
                }
            }
        }
    }
}