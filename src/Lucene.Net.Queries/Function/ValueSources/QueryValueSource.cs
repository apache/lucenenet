// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections;
using System.IO;

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
    /// <see cref="QueryValueSource"/> returns the relevance score of the query
    /// </summary>
    public class QueryValueSource : ValueSource
    {
        internal readonly Query q;
        internal readonly float defVal;

        public QueryValueSource(Query q, float defVal)
        {
            this.q = q;
            this.defVal = defVal;
        }

        public virtual Query Query => q;

        public virtual float DefaultValue => defVal;


        public override string GetDescription()
        {
            return "query(" + q + ",def=" + defVal + ")";
        }

        public override FunctionValues GetValues(IDictionary fcontext, AtomicReaderContext readerContext)
        {
            return new QueryDocValues(this, readerContext, fcontext);
        }

        public override int GetHashCode()
        {
            return q.GetHashCode() * 29;
        }

        public override bool Equals(object o)
        {
            if (!(o is QueryValueSource other))
                return false;
            return this.q.Equals(other.q) && this.defVal == other.defVal;
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            Weight w = searcher.CreateNormalizedWeight(q);
            context[this] = w;
        }
    }


    internal class QueryDocValues : SingleDocValues
    {
        internal readonly AtomicReaderContext readerContext;
        internal readonly IBits acceptDocs;
        internal readonly Weight weight;
        internal readonly float defVal;
        internal readonly IDictionary fcontext;
        internal readonly Query q;

        internal Scorer scorer;
        internal int scorerDoc; // the document the scorer is on
        internal bool noMatches = false;

        // the last document requested... start off with high value
        // to trigger a scorer reset on first access.
        internal int lastDocRequested = int.MaxValue;

        public QueryDocValues(QueryValueSource vs, AtomicReaderContext readerContext, IDictionary fcontext)
            : base(vs)
        {

            this.readerContext = readerContext;
            this.acceptDocs = readerContext.AtomicReader.LiveDocs;
            this.defVal = vs.defVal;
            this.q = vs.q;
            this.fcontext = fcontext;

            Weight w = fcontext is null ? null : (Weight)fcontext[vs];
            if (w is null)
            {
                IndexSearcher weightSearcher;
                if (fcontext is null)
                {
                    weightSearcher = new IndexSearcher(ReaderUtil.GetTopLevelContext(readerContext));
                }
                else
                {
                    weightSearcher = (IndexSearcher)fcontext["searcher"];
                    if (weightSearcher is null)
                    {
                        weightSearcher = new IndexSearcher(ReaderUtil.GetTopLevelContext(readerContext));
                    }
                }
                vs.CreateWeight(fcontext, weightSearcher);
                w = (Weight)fcontext[vs];
            }
            weight = w;
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public override float SingleVal(int doc)
        {
            try
            {
                if (doc < lastDocRequested)
                {
                    if (noMatches)
                    {
                        return defVal;
                    }
                    scorer = weight.GetScorer(readerContext, acceptDocs);
                    if (scorer is null)
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
                    // query doesn't match this document... either because we hit the
                    // end, or because the next doc is after this doc.
                    return defVal;
                }

                // a match!
                return scorer.GetScore();
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create("caught exception in QueryDocVals(" + q + ") doc=" + doc, e);
            }
        }

        public override bool Exists(int doc)
        {
            try
            {
                if (doc < lastDocRequested)
                {
                    if (noMatches)
                    {
                        return false;
                    }
                    scorer = weight.GetScorer(readerContext, acceptDocs);
                    scorerDoc = -1;
                    if (scorer is null)
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
                    // query doesn't match this document... either because we hit the
                    // end, or because the next doc is after this doc.
                    return false;
                }

                // a match!
                return true;
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create("caught exception in QueryDocVals(" + q + ") doc=" + doc, e);
            }
        }

        public override object ObjectVal(int doc)
        {
            try
            {
                return Exists(doc) ? J2N.Numerics.Single.GetInstance(scorer.GetScore()) : null; // LUCENENET: In Java, the conversion to instance of java.util.Float is implicit, but we need to do an explicit conversion
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create("caught exception in QueryDocVals(" + q + ") doc=" + doc, e);
            }
        }

        public override ValueFiller GetValueFiller()
        {
            //
            // TODO: if we want to support more than one value-filler or a value-filler in conjunction with
            // the FunctionValues, then members like "scorer" should be per ValueFiller instance.
            // Or we can say that the user should just instantiate multiple FunctionValues.
            //
            return new ValueFiller.AnonymousValueFiller<MutableValueSingle>(new MutableValueSingle(), fillValue: (doc, mutableValue) =>
            {
                try
                {
                    if (noMatches)
                    {
                        mutableValue.Value = defVal;
                        mutableValue.Exists = false;
                        return;
                    }
                    scorer = weight.GetScorer(readerContext, acceptDocs);
                    scorerDoc = -1;
                    if (scorer is null)
                    {
                        noMatches = true;
                        mutableValue.Value = defVal;
                        mutableValue.Exists = false;
                        return;
                    }
                    lastDocRequested = doc;

                    if (scorerDoc < doc)
                    {
                        scorerDoc = scorer.Advance(doc);
                    }

                    if (scorerDoc > doc)
                    {
                        // query doesn't match this document... either because we hit the
                        // end, or because the next doc is after this doc.
                        mutableValue.Value = defVal;
                        mutableValue.Exists = false;
                        return;
                    }

                    // a match!
                    mutableValue.Value = scorer.GetScore();
                    mutableValue.Exists = true;
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create("caught exception in QueryDocVals(" + q + ") doc=" + doc, e);
                }
            });
        }

        public override string ToString(int doc)
        {
            return "query(" + q + ",def=" + defVal + ")=" + SingleVal(doc);
        }
    }
}