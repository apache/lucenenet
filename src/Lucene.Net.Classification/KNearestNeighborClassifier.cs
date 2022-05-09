using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Queries.Mlt;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Classification
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
    /// A k-Nearest Neighbor classifier (see
    /// <a href="http://en.wikipedia.org/wiki/K-nearest_neighbors">http://en.wikipedia.org/wiki/K-nearest_neighbors</a>) based
    /// on <see cref="MoreLikeThis"/>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class KNearestNeighborClassifier : IClassifier<BytesRef>
    {

        private MoreLikeThis mlt;
        private string[] textFieldNames;
        private string classFieldName;
        private IndexSearcher indexSearcher;
        private readonly int k;
        private Query query;

        private readonly int minDocsFreq; // LUCENENET: marked readonly
        private readonly int minTermFreq; // LUCENENET: marked readonly

        /// <summary>Create a <see cref="IClassifier{T}"/> using kNN algorithm</summary>
        /// <param name="k">the number of neighbors to analyze as an <see cref="int"/></param>
        public KNearestNeighborClassifier(int k)
        {
            this.k = k;
        }

        /// <summary>Create a <see cref="IClassifier{T}"/> using kNN algorithm</summary>
        /// <param name="k">the number of neighbors to analyze as an <see cref="int"/></param>
        /// <param name="minDocsFreq">the minimum number of docs frequency for MLT to be set with <see cref="MoreLikeThis.MinDocFreq"/></param>
        /// <param name="minTermFreq">the minimum number of term frequency for MLT to be set with <see cref="MoreLikeThis.MinTermFreq"/></param>
        public KNearestNeighborClassifier(int k, int minDocsFreq, int minTermFreq)
        {
            this.k = k;
            this.minDocsFreq = minDocsFreq;
            this.minTermFreq = minTermFreq;
        }

        /// <summary>
        /// Assign a class (with score) to the given text string
        /// </summary>
        /// <param name="text">a string containing text to be classified</param>
        /// <returns>a <see cref="ClassificationResult{BytesRef}"/> holding assigned class of type <see cref="BytesRef"/> and score</returns>
        public virtual ClassificationResult<BytesRef> AssignClass(string text)
        {
            if (mlt is null)
            {
                throw new IOException("You must first call Classifier#train");
            }

            BooleanQuery mltQuery = new BooleanQuery();
            foreach (string textFieldName in textFieldNames)
            {
                mltQuery.Add(new BooleanClause(mlt.Like(new StringReader(text), textFieldName), Occur.SHOULD));
            }
            Query classFieldQuery = new WildcardQuery(new Term(classFieldName, "*"));
            mltQuery.Add(new BooleanClause(classFieldQuery, Occur.MUST));
            if (query != null)
            {
                mltQuery.Add(query, Occur.MUST);
            }
            TopDocs topDocs = indexSearcher.Search(mltQuery, k);
            return SelectClassFromNeighbors(topDocs);
        }

        private ClassificationResult<BytesRef> SelectClassFromNeighbors(TopDocs topDocs)
        {
            // TODO : improve the nearest neighbor selection
            Dictionary<BytesRef, int> classCounts = new Dictionary<BytesRef, int>();

            foreach (ScoreDoc scoreDoc in topDocs.ScoreDocs)
            {
                BytesRef cl = new BytesRef(indexSearcher.Doc(scoreDoc.Doc).GetField(classFieldName).GetStringValue());
                if (classCounts.TryGetValue(cl, out int value))
                {
                    classCounts[cl] = value + 1;
                }
                else
                {
                    classCounts.Add(cl, 1);
                }
            }
            double max = 0;
            BytesRef assignedClass = new BytesRef();
            foreach (KeyValuePair<BytesRef, int> entry in classCounts)
            {
                int count = entry.Value;
                if (count > max)
                {
                    max = count;
                    assignedClass = (BytesRef)entry.Key.Clone();
                }
            }
            double score = max / (double) k;
            return new ClassificationResult<BytesRef>(assignedClass, score);
        }

        /// <summary>
        /// Train the classifier using the underlying Lucene index
        /// </summary>
        /// <param name="analyzer"> the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="textFieldName">the name of the field used to compare documents</param>
        public virtual void Train(AtomicReader atomicReader, string textFieldName, string classFieldName, Analyzer analyzer)
        {
            Train(atomicReader, textFieldName, classFieldName, analyzer, null);
        }

        /// <summary>Train the classifier using the underlying Lucene index</summary>
        /// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="query">the query to filter which documents use for training</param>
        /// <param name="textFieldName">the name of the field used to compare documents</param>
        public virtual void Train(AtomicReader atomicReader, string textFieldName, string classFieldName, Analyzer analyzer, Query query)
        {
            Train(atomicReader, new string[]{textFieldName}, classFieldName, analyzer, query);
        }

        /// <summary>Train the classifier using the underlying Lucene index</summary>
        /// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="query">the query to filter which documents use for training</param>
        /// <param name="textFieldNames">the names of the fields to be used to compare documents</param>
        public virtual void Train(AtomicReader atomicReader, string[] textFieldNames, string classFieldName, Analyzer analyzer, Query query)
        {
            this.textFieldNames = textFieldNames;
            this.classFieldName = classFieldName;
            mlt = new MoreLikeThis(atomicReader);
            mlt.Analyzer = analyzer;
            mlt.FieldNames = this.textFieldNames;
            indexSearcher = new IndexSearcher(atomicReader);
            if (minDocsFreq > 0)
            {
                mlt.MinDocFreq = minDocsFreq;
            }
            if (minTermFreq > 0)
            {
                mlt.MinTermFreq = minTermFreq;
            }
            this.query = query;
        }
    }
}