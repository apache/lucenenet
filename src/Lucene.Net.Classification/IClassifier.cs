using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;

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
    /// A classifier, see <a href="http://en.wikipedia.org/wiki/Classifier_(mathematics)">http://en.wikipedia.org/wiki/Classifier_(mathematics)</a>,
    /// which assign classes of type <typeparam name="T"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public interface IClassifier<T>
    {
        /// <summary>
        /// Assign a class (with score) to the given text string
        /// </summary>
        /// <param name="text">a string containing text to be classified</param>
        /// <returns>a <see cref="ClassificationResult{T}"/> holding assigned class of type <typeparamref name="T"/> and score</returns>
        ClassificationResult<T> AssignClass(string text);

        /// <summary>
        /// Train the classifier using the underlying Lucene index
        /// </summary>
        /// <param name="analyzer"> the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="textFieldName">the name of the field used to compare documents</param>
        void Train(AtomicReader atomicReader, string textFieldName, string classFieldName, Analyzer analyzer);

        /// <summary>Train the classifier using the underlying Lucene index</summary>
        /// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="query">the query to filter which documents use for training</param>
        /// <param name="textFieldName">the name of the field used to compare documents</param>
        void Train(AtomicReader atomicReader, string textFieldName, string classFieldName, Analyzer analyzer, Query query);

        /// <summary>Train the classifier using the underlying Lucene index</summary>
        /// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="query">the query to filter which documents use for training</param>
        /// <param name="textFieldNames">the names of the fields to be used to compare documents</param>
        void Train(AtomicReader atomicReader, string[] textFieldNames, string classFieldName, Analyzer analyzer,
                   Query query);
    }
}