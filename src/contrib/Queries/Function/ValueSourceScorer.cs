using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function
{
    public class ValueSourceScorer : Scorer
    {
        protected readonly IndexReader reader;
        private int doc = -1;
        protected readonly int maxDoc;
        protected readonly FunctionValues values;
        protected bool checkDeletes;
        private readonly IBits liveDocs;

        public ValueSourceScorer(IndexReader reader, FunctionValues values)
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
            get { return reader; }
        }

        public virtual bool CheckDeletes
        {
            get { return this.checkDeletes; }
            set { this.checkDeletes = value && reader.HasDeletions; }
        }

        public virtual bool Matches(int doc)
        {
            return (!checkDeletes || liveDocs[doc]) && MatchesValue(doc);
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
                if (doc >= maxDoc) return doc = NO_MORE_DOCS;
                if (Matches(doc)) return doc;
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

        public override long Cost
        {
            get { return maxDoc; }
        }
    }
}
