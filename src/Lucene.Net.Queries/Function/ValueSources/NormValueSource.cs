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
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Function that returns <seealso cref="TFIDFSimilarity#decodeNormValue(long)"/>
    /// for every document.
    /// <para>
    /// Note that the configured Similarity for the field must be
    /// a subclass of <seealso cref="TFIDFSimilarity"/>
    /// @lucene.internal 
    /// </para>
    /// </summary>
    public class NormValueSource : ValueSource
    {
        protected internal readonly string field;

        public NormValueSource(string field)
        {
            this.field = field;
        }

        public virtual string Name
        {
            get { return "norm"; }
        }

        public override string GetDescription()
        {
            return Name + '(' + field + ')';
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            context["searcher"] = searcher;
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var searcher = (IndexSearcher)context["searcher"];
            TFIDFSimilarity similarity = IDFValueSource.AsTFIDF(searcher.Similarity, field);
            if (similarity == null)
            {
                throw new System.NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }

            NumericDocValues norms = readerContext.AtomicReader.GetNormValues(field);
            if (norms == null)
            {
                return new ConstDoubleDocValues(0.0, this);
            }

            return new FloatDocValuesAnonymousInnerClassHelper(this, this, similarity, norms);
        }

        private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
        {
            private readonly NormValueSource outerInstance;

            private readonly TFIDFSimilarity similarity;
            private readonly NumericDocValues norms;

            public FloatDocValuesAnonymousInnerClassHelper(NormValueSource outerInstance, NormValueSource @this, TFIDFSimilarity similarity, NumericDocValues norms)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.similarity = similarity;
                this.norms = norms;
            }

            public override float FloatVal(int doc)
            {
                return similarity.DecodeNormValue(norms.Get(doc));
            }
        }

        public override bool Equals(object o)
        {
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            return this.field.Equals(((NormValueSource)o).field);
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode() + field.GetHashCode();
        }
    }
}