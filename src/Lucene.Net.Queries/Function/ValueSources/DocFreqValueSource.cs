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
using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Queries.Function.ValueSources
{
    internal class ConstIntDocValues : IntDocValues
    {
        internal readonly int ival;
        internal readonly float fval;
        internal readonly double dval;
        internal readonly long lval;
        internal readonly string sval;
        internal readonly ValueSource parent;

        internal ConstIntDocValues(int val, ValueSource parent)
            : base(parent)
        {
            ival = val;
            fval = val;
            dval = val;
            lval = val;
            sval = Convert.ToString(val);
            this.parent = parent;
        }

        public override float FloatVal(int doc)
        {
            return fval;
        }
        public override int IntVal(int doc)
        {
            return ival;
        }
        public override long LongVal(int doc)
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
            sval = Convert.ToString(val);
            this.parent = parent;
        }

        public override float FloatVal(int doc)
        {
            return fval;
        }
        public override int IntVal(int doc)
        {
            return ival;
        }
        public override long LongVal(int doc)
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
    /// <code>DocFreqValueSource</code> returns the number of documents containing the term.
    /// @lucene.internal
    /// </summary>
    public class DocFreqValueSource : ValueSource
    {
        protected internal readonly string field;
        protected internal readonly string indexedField;
        protected internal readonly string val;
        protected internal readonly BytesRef indexedBytes;

        public DocFreqValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
        {
            this.field = field;
            this.val = val;
            this.indexedField = indexedField;
            this.indexedBytes = indexedBytes;
        }

        public virtual string Name
        {
            get { return "docfreq"; }
        }

        public override string GetDescription()
        {
            return Name + '(' + field + ',' + val + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var searcher = (IndexSearcher)context["searcher"];
            int docfreq = searcher.IndexReader.DocFreq(new Term(indexedField, indexedBytes));
            return new ConstIntDocValues(docfreq, this);
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            context["searcher"] = searcher;
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode() + indexedField.GetHashCode() * 29 + indexedBytes.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            var other = (DocFreqValueSource)o;
            return this.indexedField.Equals(other.indexedField) && this.indexedBytes.Equals(other.indexedBytes);
        }
    }
}