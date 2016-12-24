using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Join
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
	/// A collector that collects all terms from a specified field matching the query.
	/// 
	/// @lucene.experimental
	/// </summary>
    internal abstract class TermsCollector : Collector
    {
        private readonly string _field;
        private readonly BytesRefHash _collectorTerms = new BytesRefHash();

        internal TermsCollector(string field)
        {
            _field = field;
        }

        public BytesRefHash CollectorTerms
        {
            get
            {
                return _collectorTerms;
            }
        }
        
        public override void SetScorer(Scorer scorer)
        {
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return true;
        }

        /// <summary>
        /// Chooses the right <see cref="TermsCollector"/> implementation.
        /// </summary>
        /// <param name="field">The field to collect terms for.</param>
        /// <param name="multipleValuesPerDocument">Whether the field to collect terms for has multiple values per document.</param>
        /// <returns>A <see cref="TermsCollector"/> instance.</returns>
        internal static TermsCollector Create(string field, bool multipleValuesPerDocument)
        {
            return multipleValuesPerDocument ? (TermsCollector) new MV(field) : new SV(field);
        }

        // impl that works with multiple values per document
        private class MV : TermsCollector
        {
            private readonly BytesRef _scratch = new BytesRef();
            private SortedSetDocValues _docTermOrds;

            internal MV(string field) : base(field)
            {
            }
            
            public override void Collect(int doc)
            {
                _docTermOrds.SetDocument(doc);
                long ord;
                while ((ord = _docTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    _docTermOrds.LookupOrd(ord, _scratch);
                    _collectorTerms.Add(_scratch);
                }
            }
            
            public override void SetNextReader(AtomicReaderContext context)
            {
                _docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, _field);
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }

        // impl that works with single value per document
        private class SV : TermsCollector
        {
            private readonly BytesRef _spare = new BytesRef();
            private BinaryDocValues _fromDocTerms;

            internal SV(string field) : base(field)
            {
            }
            
            public override void Collect(int doc)
            {
                _fromDocTerms.Get(doc, _spare);
                _collectorTerms.Add(_spare);
            }
            
            public override void SetNextReader(AtomicReaderContext context)
            {
                _fromDocTerms = FieldCache.DEFAULT.GetTerms(context.AtomicReader, _field, false);
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return base.AcceptsDocsOutOfOrder();
            }
        }
    }
}