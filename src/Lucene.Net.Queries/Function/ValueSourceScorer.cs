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
    /// <seealso cref="Scorer"/> which returns the result of <seealso cref="FunctionValues#FloatVal(int)"/> as
    /// the score for a document.
    /// </summary>
    public class ValueSourceScorer : Scorer
    {
        protected internal readonly IndexReader reader;
        private int doc = -1;
        protected internal readonly int maxDoc;
        protected internal readonly FunctionValues values;
        protected internal bool checkDeletes;
        private readonly Bits liveDocs;

        protected internal ValueSourceScorer(IndexReader reader, FunctionValues values)
            : base(null)
        {
            this.reader = reader;
            this.maxDoc = reader.MaxDoc;
            this.values = values;
            CheckDeletes = true;
            this.liveDocs = MultiFields.GetLiveDocs(reader);
        }

        public virtual IndexReader Reader
        {
            get
            {
                return reader;
            }
        }

        public virtual bool CheckDeletes
        {
            set
            {
                this.checkDeletes = value && reader.HasDeletions;
            }
        }

        public virtual bool Matches(int doc)
        {
            return (!checkDeletes || liveDocs.Get(doc)) && MatchesValue(doc);
        }

        public virtual bool MatchesValue(int doc)
        {
            return true;
        }

        public override int DocID()
        {
            return doc;
        }

        public override int NextDoc()
        {
            for (; ; )
            {
                doc++;
                if (doc >= maxDoc)
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

        public override float Score()
        {
            return values.FloatVal(doc);
        }

        public override int Freq
        {
            get { return 1; }
        }

        public override long Cost()
        {
            return maxDoc;
        }
    }
}