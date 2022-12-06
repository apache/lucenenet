// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// Converts individual <see cref="ValueSource"/> instances to leverage the FunctionValues *Val functions that work with multiple values,
    /// i.e. <see cref="FunctionValues.DoubleVal(int, double[])"/>
    /// </summary>
    //Not crazy about the name, but...
    public class VectorValueSource : MultiValueSource
    {
        protected readonly IList<ValueSource> m_sources;

        public VectorValueSource(IList<ValueSource> sources)
        {
            this.m_sources = sources;
        }

        public virtual IList<ValueSource> Sources => m_sources;

        public override int Dimension => m_sources.Count;

        public virtual string Name => "vector";

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var size = m_sources.Count;

            // special-case x,y and lat,lon since it's so common
            if (size == 2)
            {
                var x = m_sources[0].GetValues(context, readerContext);
                var y = m_sources[1].GetValues(context, readerContext);
                return new FunctionValuesAnonymousClass(this, x, y);
            }

            var valsArr = new FunctionValues[size];
            for (int i = 0; i < size; i++)
            {
                valsArr[i] = m_sources[i].GetValues(context, readerContext);
            }

            return new FunctionValuesAnonymousClass2(this, valsArr);
        }

        private sealed class FunctionValuesAnonymousClass : FunctionValues
        {
            private readonly VectorValueSource outerInstance;

            private readonly FunctionValues x;
            private readonly FunctionValues y;

            public FunctionValuesAnonymousClass(VectorValueSource outerInstance, FunctionValues x, FunctionValues y)
            {
                this.outerInstance = outerInstance;
                this.x = x;
                this.y = y;
            }

            public override void ByteVal(int doc, byte[] vals)
            {
                vals[0] = x.ByteVal(doc);
                vals[1] = y.ByteVal(doc);
            }

            /// <summary>
            /// NOTE: This was shortVal() in Lucene
            /// </summary>
            public override void Int16Val(int doc, short[] vals)
            {
                vals[0] = x.Int16Val(doc);
                vals[1] = y.Int16Val(doc);
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override void Int32Val(int doc, int[] vals)
            {
                vals[0] = x.Int32Val(doc);
                vals[1] = y.Int32Val(doc);
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override void Int64Val(int doc, long[] vals)
            {
                vals[0] = x.Int64Val(doc);
                vals[1] = y.Int64Val(doc);
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override void SingleVal(int doc, float[] vals)
            {
                vals[0] = x.SingleVal(doc);
                vals[1] = y.SingleVal(doc);
            }
            public override void DoubleVal(int doc, double[] vals)
            {
                vals[0] = x.DoubleVal(doc);
                vals[1] = y.DoubleVal(doc);
            }
            public override void StrVal(int doc, string[] vals)
            {
                vals[0] = x.StrVal(doc);
                vals[1] = y.StrVal(doc);
            }
            public override string ToString(int doc)
            {
                return outerInstance.Name + "(" + x.ToString(doc) + "," + y.ToString(doc) + ")";
            }
        }

        private sealed class FunctionValuesAnonymousClass2 : FunctionValues
        {
            private readonly VectorValueSource outerInstance;
            private readonly FunctionValues[] valsArr;

            public FunctionValuesAnonymousClass2(VectorValueSource outerInstance, FunctionValues[] valsArr)
            {
                this.outerInstance = outerInstance;
                this.valsArr = valsArr;
            }

            public override void ByteVal(int doc, byte[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].ByteVal(doc);
                }
            }

            /// <summary>
            /// NOTE: This was shortVal() in Lucene
            /// </summary>
            public override void Int16Val(int doc, short[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].Int16Val(doc);
                }
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override void SingleVal(int doc, float[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].SingleVal(doc);
                }
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override void Int32Val(int doc, int[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].Int32Val(doc);
                }
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override void Int64Val(int doc, long[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].Int64Val(doc);
                }
            }

            public override void DoubleVal(int doc, double[] vals)
            {
                for (var i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].DoubleVal(doc);
                }
            }

            public override void StrVal(int doc, string[] vals)
            {
                for (var i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].StrVal(doc);
                }
            }

            public override string ToString(int doc)
            {
                var sb = new StringBuilder();
                sb.Append(outerInstance.Name).Append('(');
                bool firstTime = true;
                foreach (var vals in valsArr)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }
                    sb.Append(vals.ToString(doc));
                }
                sb.Append(')');
                return sb.ToString();
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            foreach (ValueSource source in m_sources)
            {
                source.CreateWeight(context, searcher);
            }
        }


        public override string GetDescription()
        {
            var sb = new StringBuilder();
            sb.Append(Name).Append('(');
            bool firstTime = true;
            foreach (ValueSource source in m_sources)
            {
                if (firstTime)
                {
                    firstTime = false;
                }
                else
                {
                    sb.Append(',');
                }
                sb.Append(source);
            }
            sb.Append(')');
            return sb.ToString();
        }

        public override bool Equals(object o)
        {
            if (this == o)
                return true;

            if (!(o is VectorValueSource that))
                return false;

            // LUCENENET specific: use structural equality comparison
            return JCG.ListEqualityComparer<ValueSource>.Default.Equals(m_sources, that.m_sources);
        }

        public override int GetHashCode()
        {
            // LUCENENET specific: use structural equality comparison
            return JCG.ListEqualityComparer<ValueSource>.Default.GetHashCode(m_sources);
        }
    }
}