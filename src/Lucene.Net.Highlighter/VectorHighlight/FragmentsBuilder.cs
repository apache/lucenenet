using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using System.IO;

namespace Lucene.Net.Search.VectorHighlight
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
    /// <see cref="IFragmentsBuilder"/> is an interface for fragments (snippets) builder classes.
    /// A <see cref="IFragmentsBuilder"/> class can be plugged in to
    /// <see cref="FastVectorHighlighter"/>.
    /// </summary>
    public interface IFragmentsBuilder
    {
        /// <summary>
        /// create a fragment.
        /// </summary>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fieldFragList"><see cref="FieldFragList"/> object</param>
        /// <returns>a created fragment or null when no fragment created</returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        string CreateFragment(IndexReader reader, int docId, string fieldName,
            FieldFragList fieldFragList);

        /// <summary>
        /// create multiple fragments.
        /// </summary>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighter</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fieldFragList"><see cref="FieldFragList"/> object</param>
        /// <param name="maxNumFragments">maximum number of fragments</param>
        /// <returns>
        /// created fragments or null when no fragments created.
        /// size of the array can be less than <paramref name="maxNumFragments"/>
        /// </returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        string[] CreateFragments(IndexReader reader, int docId, string fieldName,
            FieldFragList fieldFragList, int maxNumFragments);

        /// <summary>
        /// create a fragment.
        /// </summary>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fieldFragList"><see cref="FieldFragList"/> object</param>
        /// <param name="preTags">pre-tags to be used to highlight terms</param>
        /// <param name="postTags">post-tags to be used to highlight terms</param>
        /// <param name="encoder">an encoder that generates encoded text</param>
        /// <returns>a created fragment or null when no fragment created</returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        string CreateFragment(IndexReader reader, int docId, string fieldName,
            FieldFragList fieldFragList, string[] preTags, string[] postTags,
            IEncoder encoder);

        /// <summary>
        /// create multiple fragments.
        /// </summary>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighter</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fieldFragList"><see cref="FieldFragList"/> object</param>
        /// <param name="maxNumFragments">maximum number of fragments</param>
        /// <param name="preTags">pre-tags to be used to highlight terms</param>
        /// <param name="postTags">post-tags to be used to highlight terms</param>
        /// <param name="encoder">an encoder that generates encoded text</param>
        /// <returns>
        /// created fragments or null when no fragments created.
        /// size of the array can be less than <paramref name="maxNumFragments"/>
        /// </returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        string[] CreateFragments(IndexReader reader, int docId, string fieldName,
            FieldFragList fieldFragList, int maxNumFragments, string[] preTags, string[] postTags,
            IEncoder encoder);
    }
}
