using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// {@link org.apache.lucene.search.vectorhighlight.FragmentsBuilder} is an interface for fragments (snippets) builder classes.
    /// A {@link org.apache.lucene.search.vectorhighlight.FragmentsBuilder} class can be plugged in to
    /// {@link org.apache.lucene.search.vectorhighlight.FastVectorHighlighter}.
    /// </summary>
    public interface IFragmentsBuilder
    {
        /**
   * create a fragment.
   * 
   * @param reader IndexReader of the index
   * @param docId document id to be highlighted
   * @param fieldName field of the document to be highlighted
   * @param fieldFragList FieldFragList object
   * @return a created fragment or null when no fragment created
   * @throws IOException If there is a low-level I/O error
   */
        string CreateFragment(IndexReader reader, int docId, string fieldName,
            FieldFragList fieldFragList);

        /**
         * create multiple fragments.
         * 
         * @param reader IndexReader of the index
         * @param docId document id to be highlighter
         * @param fieldName field of the document to be highlighted
         * @param fieldFragList FieldFragList object
         * @param maxNumFragments maximum number of fragments
         * @return created fragments or null when no fragments created.
         *         size of the array can be less than maxNumFragments
         * @throws IOException If there is a low-level I/O error
         */
        string[] CreateFragments(IndexReader reader, int docId, string fieldName,
            FieldFragList fieldFragList, int maxNumFragments);

        /**
         * create a fragment.
         * 
         * @param reader IndexReader of the index
         * @param docId document id to be highlighted
         * @param fieldName field of the document to be highlighted
         * @param fieldFragList FieldFragList object
         * @param preTags pre-tags to be used to highlight terms
         * @param postTags post-tags to be used to highlight terms
         * @param encoder an encoder that generates encoded text
         * @return a created fragment or null when no fragment created
         * @throws IOException If there is a low-level I/O error
         */
        string CreateFragment(IndexReader reader, int docId, string fieldName,
            FieldFragList fieldFragList, string[] preTags, string[] postTags,
            IEncoder encoder);

        /**
         * create multiple fragments.
         * 
         * @param reader IndexReader of the index
         * @param docId document id to be highlighter
         * @param fieldName field of the document to be highlighted
         * @param fieldFragList FieldFragList object
         * @param maxNumFragments maximum number of fragments
         * @param preTags pre-tags to be used to highlight terms
         * @param postTags post-tags to be used to highlight terms
         * @param encoder an encoder that generates encoded text
         * @return created fragments or null when no fragments created.
         *         size of the array can be less than maxNumFragments
         * @throws IOException If there is a low-level I/O error
         */
        string[] CreateFragments(IndexReader reader, int docId, string fieldName,
            FieldFragList fieldFragList, int maxNumFragments, string[] preTags, string[] postTags,
            IEncoder encoder);
    }
}
