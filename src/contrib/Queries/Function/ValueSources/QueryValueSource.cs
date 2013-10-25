using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class QueryValueSource : ValueSource
    {
        internal readonly Query q;
        internal readonly float defVal;

        public QueryValueSource(Query q, float defVal)
        {
            this.q = q;
            this.defVal = defVal;
        }

        public virtual Query Query
        {
            get
            {
                return q;
            }
        }

        public virtual float DefaultValue
        {
            get
            {
                return defVal;
            }
        }

        public override string Description
        {
            get
            {
                return @"query(" + q + @",def=" + defVal + @")";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> fcontext, AtomicReaderContext readerContext)
        {
            return new QueryDocValues(this, readerContext, fcontext);
        }

        public override int GetHashCode()
        {
            return q.GetHashCode() * 29;
        }

        public override bool Equals(Object o)
        {
            if (typeof(QueryValueSource) != o.GetType())
                return false;
            QueryValueSource other = (QueryValueSource)o;
            return this.q.Equals(other.q) && this.defVal == other.defVal;
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            Weight w = searcher.CreateNormalizedWeight(q);
            context[this] = w;
        }
    }

    class QueryDocValues : FloatDocValues
    {
        readonly AtomicReaderContext readerContext;
        readonly IBits acceptDocs;
        readonly Weight weight;
        readonly float defVal;
        readonly IDictionary<object, object> fcontext;
        readonly Query q;
        Scorer scorer;
        int scorerDoc;
        bool noMatches = false;
        int lastDocRequested = int.MaxValue;

        public QueryDocValues(QueryValueSource vs, AtomicReaderContext readerContext, IDictionary<object, object> fcontext)
            : base(vs)
        {
            this.readerContext = readerContext;
            this.acceptDocs = readerContext.AtomicReader.LiveDocs;
            this.defVal = vs.defVal;
            this.q = vs.q;
            this.fcontext = fcontext;
            Weight w = fcontext == null ? null : (Weight)fcontext[vs];
            if (w == null)
            {
                IndexSearcher weightSearcher;
                if (fcontext == null)
                {
                    weightSearcher = new IndexSearcher(ReaderUtil.GetTopLevelContext(readerContext));
                }
                else
                {
                    weightSearcher = (IndexSearcher)fcontext["searcher"];
                    if (weightSearcher == null)
                    {
                        weightSearcher = new IndexSearcher(ReaderUtil.GetTopLevelContext(readerContext));
                    }
                }

                vs.CreateWeight(fcontext, weightSearcher);
                w = (Weight)fcontext[vs];
            }

            weight = w;
        }

        public override float FloatVal(int doc)
        {
            try
            {
                if (doc < lastDocRequested)
                {
                    if (noMatches)
                        return defVal;
                    scorer = weight.Scorer(readerContext, true, false, acceptDocs);
                    if (scorer == null)
                    {
                        noMatches = true;
                        return defVal;
                    }

                    scorerDoc = -1;
                }

                lastDocRequested = doc;
                if (scorerDoc < doc)
                {
                    scorerDoc = scorer.Advance(doc);
                }

                if (scorerDoc > doc)
                {
                    return defVal;
                }

                return scorer.Score();
            }
            catch (IOException e)
            {
                throw new Exception(@"caught exception in QueryDocVals(" + q + @") doc=" + doc, e);
            }
        }

        public override bool Exists(int doc)
        {
            try
            {
                if (doc < lastDocRequested)
                {
                    if (noMatches)
                        return false;
                    scorer = weight.Scorer(readerContext, true, false, acceptDocs);
                    scorerDoc = -1;
                    if (scorer == null)
                    {
                        noMatches = true;
                        return false;
                    }
                }

                lastDocRequested = doc;
                if (scorerDoc < doc)
                {
                    scorerDoc = scorer.Advance(doc);
                }

                if (scorerDoc > doc)
                {
                    return false;
                }

                return true;
            }
            catch (IOException e)
            {
                throw new Exception(@"caught exception in QueryDocVals(" + q + @") doc=" + doc, e);
            }
        }

        public override Object ObjectVal(int doc)
        {
            try
            {
                return Exists(doc) ? (object)scorer.Score() : null;
            }
            catch (IOException e)
            {
                throw new Exception(@"caught exception in QueryDocVals(" + q + @") doc=" + doc, e);
            }
        }

        public override ValueFiller GetValueFiller()
        {
            return new AnonymousValueFiller(this);
        }

        private sealed class AnonymousValueFiller : ValueFiller
        {
            public AnonymousValueFiller(QueryDocValues parent)
            {
                this.parent = parent;
            }

            private readonly QueryDocValues parent;
            private readonly MutableValueFloat mval = new MutableValueFloat();
            
            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                try
                {
                    if (parent.noMatches)
                    {
                        mval.Value = parent.defVal;
                        mval.Exists = false;
                        return;
                    }

                    parent.scorer = parent.weight.Scorer(parent.readerContext, true, false, parent.acceptDocs);
                    parent.scorerDoc = -1;
                    if (parent.scorer == null)
                    {
                        parent.noMatches = true;
                        mval.Value = parent.defVal;
                        mval.Exists = false;
                        return;
                    }

                    parent.lastDocRequested = doc;
                    if (parent.scorerDoc < doc)
                    {
                        parent.scorerDoc = parent.scorer.Advance(doc);
                    }

                    if (parent.scorerDoc > doc)
                    {
                        mval.Value = parent.defVal;
                        mval.Exists = false;
                        return;
                    }

                    mval.Value = parent.scorer.Score();
                    mval.Exists = true;
                }
                catch (IOException e)
                {
                    throw new Exception(@"caught exception in QueryDocVals(" + parent.q + @") doc=" + doc, e);
                }
            }
        }

        public override string ToString(int doc)
        {
            return @"query(" + q + @",def=" + defVal + @")=" + FloatVal(doc);
        }
    }
}
