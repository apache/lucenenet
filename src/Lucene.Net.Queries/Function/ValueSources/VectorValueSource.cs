using System.Collections;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;

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
    /// Converts individual ValueSource instances to leverage the FunctionValues *Val functions that work with multiple values,
    /// i.e. <seealso cref="FunctionValues#DoubleVal(int, double[])"/>
    /// </summary>
    //Not crazy about the name, but...
    public class VectorValueSource : MultiValueSource
    {
        protected internal readonly IList<ValueSource> sources;


        public VectorValueSource(IList<ValueSource> sources)
        {
            this.sources = sources;
        }

        public virtual IList<ValueSource> Sources
        {
            get
            {
                return sources;
            }
        }

        public override int Dimension
        {
            get { return sources.Count; }
        }

        public virtual string Name
        {
            get { return "vector"; }
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var size = sources.Count;

            // special-case x,y and lat,lon since it's so common
            if (size == 2)
            {
                var x = sources[0].GetValues(context, readerContext);
                var y = sources[1].GetValues(context, readerContext);
                return new FunctionValuesAnonymousInnerClassHelper(this, x, y);
            }

            var valsArr = new FunctionValues[size];
            for (int i = 0; i < size; i++)
            {
                valsArr[i] = sources[i].GetValues(context, readerContext);
            }

            return new FunctionValuesAnonymousInnerClassHelper2(this, valsArr);
        }

        private class FunctionValuesAnonymousInnerClassHelper : FunctionValues
        {
            private readonly VectorValueSource outerInstance;

            private readonly FunctionValues x;
            private readonly FunctionValues y;

            public FunctionValuesAnonymousInnerClassHelper(VectorValueSource outerInstance, FunctionValues x, FunctionValues y)
            {
                this.outerInstance = outerInstance;
                this.x = x;
                this.y = y;
            }

            public override void ByteVal(int doc, sbyte[] vals)
            {
                vals[0] = x.ByteVal(doc);
                vals[1] = y.ByteVal(doc);
            }

            public override void ShortVal(int doc, short[] vals)
            {
                vals[0] = x.ShortVal(doc);
                vals[1] = y.ShortVal(doc);
            }
            public override void IntVal(int doc, int[] vals)
            {
                vals[0] = x.IntVal(doc);
                vals[1] = y.IntVal(doc);
            }
            public override void LongVal(int doc, long[] vals)
            {
                vals[0] = x.LongVal(doc);
                vals[1] = y.LongVal(doc);
            }
            public override void FloatVal(int doc, float[] vals)
            {
                vals[0] = x.FloatVal(doc);
                vals[1] = y.FloatVal(doc);
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

        private class FunctionValuesAnonymousInnerClassHelper2 : FunctionValues
        {
            private readonly VectorValueSource outerInstance;
            private readonly FunctionValues[] valsArr;

            public FunctionValuesAnonymousInnerClassHelper2(VectorValueSource outerInstance, FunctionValues[] valsArr)
            {
                this.outerInstance = outerInstance;
                this.valsArr = valsArr;
            }

            public override void ByteVal(int doc, sbyte[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].ByteVal(doc);
                }
            }

            public override void ShortVal(int doc, short[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].ShortVal(doc);
                }
            }

            public override void FloatVal(int doc, float[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].FloatVal(doc);
                }
            }

            public override void IntVal(int doc, int[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].IntVal(doc);
                }
            }

            public override void LongVal(int doc, long[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].LongVal(doc);
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
            foreach (ValueSource source in sources)
            {
                source.CreateWeight(context, searcher);
            }
        }


        public override string GetDescription()
        {
            var sb = new StringBuilder();
            sb.Append(Name).Append('(');
            bool firstTime = true;
            foreach (ValueSource source in sources)
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
            sb.Append(")");
            return sb.ToString();
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is VectorValueSource))
            {
                return false;
            }

            var that = (VectorValueSource)o;
            return sources.Equals(that.sources);
        }

        public override int GetHashCode()
        {
            return sources.GetHashCode();
        }
    }
}