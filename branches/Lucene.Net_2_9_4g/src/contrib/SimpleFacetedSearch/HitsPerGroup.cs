/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    public partial class SimpleFacetedSearch
    {
        public class HitsPerGroup : IEnumerable<Document>, IEnumerator<Document>
        {
            IndexReader _Reader;
            int _MaxDocPerGroup;
            int _ItemsReturned = 0;
            DocIdSetIterator _ResultIterator;
            OpenBitSetDISI _ResultBitSet;
            int _CurrentDocId;

            GroupName _GroupName;
            long _HitCount = -1;

            internal HitsPerGroup(GroupName group, IndexReader reader, DocIdSet queryDocidSet, OpenBitSetDISI groupBitSet, int maxDocPerGroup)
            {
                this._GroupName = group;
                this._Reader = reader;
                this._MaxDocPerGroup = maxDocPerGroup;

                _ResultBitSet = new OpenBitSetDISI(queryDocidSet.Iterator(), _Reader.MaxDoc());
                _ResultBitSet.And(groupBitSet);

                _ResultIterator = _ResultBitSet.Iterator();
            }

            public GroupName Name
            {
                get { return _GroupName; }
            }

            public long HitCount
            {
                get
                {
                    if (_HitCount == -1) _HitCount = _ResultBitSet.Cardinality();
                    return _HitCount;
                }
            }

            public Document Current
            {
                get { return _Reader.Document(_CurrentDocId); }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return _Reader.Document(_CurrentDocId); }
            }

            public bool MoveNext()
            {
                _CurrentDocId = _ResultIterator.NextDoc();
                return _CurrentDocId != DocIdSetIterator.NO_MORE_DOCS && ++_ItemsReturned <= _MaxDocPerGroup;
            }

            public IEnumerator<Document> GetEnumerator()
            {
                return this;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {

            }

            public HitsPerGroup Documents
            {
                get { return this; }
            }
        }
    }
}
