// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Globalization;

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
    /// NOTE: This was ConstIntDocValues in Lucene
    /// </summary>
    internal class ConstInt32DocValues : Int32DocValues
    {
        internal readonly int ival;
        internal readonly float fval;
        internal readonly double dval;
        internal readonly long lval;
        internal readonly string sval;
        internal readonly ValueSource parent;

        internal ConstInt32DocValues(int val, ValueSource parent)
            : base(parent)
        {
            ival = val;
            fval = val;
            dval = val;
            lval = val;
            sval = J2N.Numerics.Int32.ToString(val, NumberFormatInfo.InvariantInfo);
            this.parent = parent;
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public override float SingleVal(int doc)
        {
            return fval;
        }

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public override int Int32Val(int doc)
        {
            return ival;
        }

        /// <summary>
        /// NOTE: This was longVal() in Lucene
        /// </summary>
        public override long Int64Val(int doc)
        {
            return lval;
        }
        public override double DoubleVal(int doc)
        {
            return dval;
        }
        public override string StrVal(int doc)
        {
            return sval;
        }
        public override string ToString(int doc)
        {
            return parent.GetDescription() + '=' + sval;
        }
    }

    internal class ConstDoubleDocValues : DoubleDocValues
    {
        internal readonly int ival;
        internal readonly float fval;
        internal readonly double dval;
        internal readonly long lval;
        internal readonly string sval;
        internal readonly ValueSource parent;

        internal ConstDoubleDocValues(double val, ValueSource parent)
            : base(parent)
        {
            ival = (int)val;
            fval = (float)val;
            dval = val;
            lval = (long)val;
            sval = J2N.Numerics.Double.ToString(val, NumberFormatInfo.InvariantInfo); // LUCENENET: Use J2N to mimic the Java string format using the "J" format
            this.parent = parent;
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public override float SingleVal(int doc)
        {
            return fval;
        }

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public override int Int32Val(int doc)
        {
            return ival;
        }

        /// <summary>
        /// NOTE: This was longVal() in Lucene
        /// </summary>
        public override long Int64Val(int doc)
        {
            return lval;
        }
        public override double DoubleVal(int doc)
        {
            return dval;
        }
        public override string StrVal(int doc)
        {
            return sval;
        }
        public override string ToString(int doc)
        {
            return parent.GetDescription() + '=' + sval;
        }
    }


    /// <summary>
    /// <see cref="DocFreqValueSource"/> returns the number of documents containing the term.
    /// @lucene.internal
    /// </summary>
    public class DocFreqValueSource : ValueSource
    {
        protected readonly string m_field;
        protected readonly string m_indexedField;
        protected readonly string m_val;
        protected readonly BytesRef m_indexedBytes;

        public DocFreqValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
        {
            this.m_field = field;
            this.m_val = val;
            this.m_indexedField = indexedField;
            this.m_indexedBytes = indexedBytes;
        }

        public virtual string Name => "docfreq";

        public override string GetDescription()
        {
            return Name + '(' + m_field + ',' + m_val + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var searcher = (IndexSearcher)context["searcher"];
            int docfreq = searcher.IndexReader.DocFreq(new Term(m_indexedField, m_indexedBytes));
            return new ConstInt32DocValues(docfreq, this);
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            context["searcher"] = searcher;
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode() + m_indexedField.GetHashCode() * 29 + m_indexedBytes.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            var other = (DocFreqValueSource)o;
            return this.m_indexedField.Equals(other.m_indexedField, StringComparison.Ordinal) && this.m_indexedBytes.Equals(other.m_indexedBytes);
        }
    }
}