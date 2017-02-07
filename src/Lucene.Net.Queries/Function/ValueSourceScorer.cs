using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Queries.Function
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
    /// <see cref="Scorer"/> which returns the result of <see cref="FunctionValues.SingleVal(int)"/> as
    /// the score for a document.
    /// </summary>
    public class ValueSourceScorer : Scorer
    {
        protected readonly IndexReader m_reader;
        private int doc = -1;
        protected readonly int m_maxDoc;
        protected readonly FunctionValues m_values;
        protected bool m_checkDeletes;
        private readonly IBits liveDocs;

        protected internal ValueSourceScorer(IndexReader reader, FunctionValues values)
            : base(null)
        {
            this.m_reader = reader;
            this.m_maxDoc = reader.MaxDoc;
            this.m_values = values;
            SetCheckDeletes(true);
            this.liveDocs = MultiFields.GetLiveDocs(reader);
        }

        public virtual IndexReader Reader
        {
            get
            {
                return m_reader;
            }
        }

        public virtual void SetCheckDeletes(bool checkDeletes)
        {
            this.m_checkDeletes = checkDeletes && m_reader.HasDeletions;
        }

        public virtual bool Matches(int doc)
        {
            return (!m_checkDeletes || liveDocs.Get(doc)) && MatchesValue(doc);
        }

        public virtual bool MatchesValue(int doc)
        {
            return true;
        }

        public override int DocID
        {
            get { return doc; }
        }

        public override int NextDoc()
        {
            for (; ; )
            {
                doc++;
                if (doc >= m_maxDoc)
                {
                    return doc = NO_MORE_DOCS;
                }
                if (Matches(doc))
                {
                    return doc;
                }
            }
        }

        public override int Advance(int target)
        {
            // also works fine when target==NO_MORE_DOCS
            doc = target - 1;
            return NextDoc();
        }

        public override float GetScore()
        {
            return m_values.SingleVal(doc);
        }

        public override int Freq
        {
            get { return 1; }
        }

        public override long GetCost()
        {
            return m_maxDoc;
        }
    }
}