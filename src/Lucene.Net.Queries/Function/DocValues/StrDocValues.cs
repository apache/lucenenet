// Lucene version compatibility level 4.8.1
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Queries.Function.DocValues
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
    /// Abstract <see cref="FunctionValues"/> implementation which supports retrieving <see cref="string"/> values.
    /// Implementations can control how the <see cref="string"/> values are loaded through <see cref="StrVal(int)"/>
    /// </summary>
    public abstract class StrDocValues : FunctionValues
    {
        protected readonly ValueSource m_vs;

        protected StrDocValues(ValueSource vs) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_vs = vs;
        }

        public override abstract string StrVal(int doc);

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? StrVal(doc) : null;
        }

        public override bool BoolVal(int doc)
        {
            return Exists(doc);
        }

        public override string ToString(int doc)
        {
            return m_vs.GetDescription() + "='" + StrVal(doc) + "'";
        }

        public override ValueFiller GetValueFiller()
        {
            return new ValueFiller.AnonymousValueFiller<MutableValueStr>(new MutableValueStr(), fillValue: (doc, mutableValue) =>
            {
                mutableValue.Exists = BytesVal(doc, mutableValue.Value);
            });
        }
    }
}