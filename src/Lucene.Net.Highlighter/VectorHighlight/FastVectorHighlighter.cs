using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    /// Another highlighter implementation.
    /// </summary>
    public class FastVectorHighlighter
    {
        public static readonly bool DEFAULT_PHRASE_HIGHLIGHT = true;
        public static readonly bool DEFAULT_FIELD_MATCH = true;
        private readonly bool phraseHighlight;
        private readonly bool fieldMatch;
        private readonly IFragListBuilder fragListBuilder;
        private readonly IFragmentsBuilder fragmentsBuilder;
        private int phraseLimit = int.MaxValue;

        /**
         * the default constructor.
         */
        public FastVectorHighlighter()
            : this(DEFAULT_PHRASE_HIGHLIGHT, DEFAULT_FIELD_MATCH)
        {
        }

        /**
         * a constructor. Using {@link SimpleFragListBuilder} and {@link ScoreOrderFragmentsBuilder}.
         * 
         * @param phraseHighlight true or false for phrase highlighting
         * @param fieldMatch true of false for field matching
         */
        public FastVectorHighlighter(bool phraseHighlight, bool fieldMatch)
            : this(phraseHighlight, fieldMatch, new SimpleFragListBuilder(), new ScoreOrderFragmentsBuilder())
        {
        }

        /**
         * a constructor. A {@link FragListBuilder} and a {@link FragmentsBuilder} can be specified (plugins).
         * 
         * @param phraseHighlight true of false for phrase highlighting
         * @param fieldMatch true of false for field matching
         * @param fragListBuilder an instance of {@link FragListBuilder}
         * @param fragmentsBuilder an instance of {@link FragmentsBuilder}
         */
        public FastVectorHighlighter(bool phraseHighlight, bool fieldMatch,
            IFragListBuilder fragListBuilder, IFragmentsBuilder fragmentsBuilder)
        {
            this.phraseHighlight = phraseHighlight;
            this.fieldMatch = fieldMatch;
            this.fragListBuilder = fragListBuilder;
            this.fragmentsBuilder = fragmentsBuilder;
        }

        /**
         * create a {@link FieldQuery} object.
         * 
         * @param query a query
         * @return the created {@link FieldQuery} object
         */
        public virtual FieldQuery GetFieldQuery(Query query)
        {
            // TODO: should we deprecate this? 
            // because if there is no reader, then we cannot rewrite MTQ.
            try
            {
                return new FieldQuery(query, null, phraseHighlight, fieldMatch);
            }
            catch (IOException e)
            {
                // should never be thrown when reader is null
                throw new Exception(e.Message, e);
            }
        }

        /**
         * create a {@link FieldQuery} object.
         * 
         * @param query a query
         * @return the created {@link FieldQuery} object
         */
        public virtual FieldQuery GetFieldQuery(Query query, IndexReader reader)
        {
            return new FieldQuery(query, reader, phraseHighlight, fieldMatch);
        }

        /**
         * return the best fragment.
         * 
         * @param fieldQuery {@link FieldQuery} object
         * @param reader {@link IndexReader} of the index
         * @param docId document id to be highlighted
         * @param fieldName field of the document to be highlighted
         * @param fragCharSize the length (number of chars) of a fragment
         * @return the best fragment (snippet) string
         * @throws IOException If there is a low-level I/O error
         */
        public string GetBestFragment(FieldQuery fieldQuery, IndexReader reader, int docId,
            string fieldName, int fragCharSize)
        {
            FieldFragList fieldFragList =
                GetFieldFragList(fragListBuilder, fieldQuery, reader, docId, fieldName, fragCharSize);
            return fragmentsBuilder.CreateFragment(reader, docId, fieldName, fieldFragList);
        }

        /**
         * return the best fragments.
         * 
         * @param fieldQuery {@link FieldQuery} object
         * @param reader {@link IndexReader} of the index
         * @param docId document id to be highlighted
         * @param fieldName field of the document to be highlighted
         * @param fragCharSize the length (number of chars) of a fragment
         * @param maxNumFragments maximum number of fragments
         * @return created fragments or null when no fragments created.
         *         size of the array can be less than maxNumFragments
         * @throws IOException If there is a low-level I/O error
         */
        public string[] GetBestFragments(FieldQuery fieldQuery, IndexReader reader, int docId,
            string fieldName, int fragCharSize, int maxNumFragments)
        {
            FieldFragList fieldFragList =
                GetFieldFragList(fragListBuilder, fieldQuery, reader, docId, fieldName, fragCharSize);
            return fragmentsBuilder.CreateFragments(reader, docId, fieldName, fieldFragList, maxNumFragments);
        }

        /**
         * return the best fragment.
         * 
         * @param fieldQuery {@link FieldQuery} object
         * @param reader {@link IndexReader} of the index
         * @param docId document id to be highlighted
         * @param fieldName field of the document to be highlighted
         * @param fragCharSize the length (number of chars) of a fragment
         * @param fragListBuilder {@link FragListBuilder} object
         * @param fragmentsBuilder {@link FragmentsBuilder} object
         * @param preTags pre-tags to be used to highlight terms
         * @param postTags post-tags to be used to highlight terms
         * @param encoder an encoder that generates encoded text
         * @return the best fragment (snippet) string
         * @throws IOException If there is a low-level I/O error
         */
        public string GetBestFragment(FieldQuery fieldQuery, IndexReader reader, int docId,
            string fieldName, int fragCharSize,
            IFragListBuilder fragListBuilder, IFragmentsBuilder fragmentsBuilder,
            string[] preTags, string[] postTags, IEncoder encoder)
        {
            FieldFragList fieldFragList = GetFieldFragList(fragListBuilder, fieldQuery, reader, docId, fieldName, fragCharSize);
            return fragmentsBuilder.CreateFragment(reader, docId, fieldName, fieldFragList, preTags, postTags, encoder);
        }

        /**
         * return the best fragments.
         * 
         * @param fieldQuery {@link FieldQuery} object
         * @param reader {@link IndexReader} of the index
         * @param docId document id to be highlighted
         * @param fieldName field of the document to be highlighted
         * @param fragCharSize the length (number of chars) of a fragment
         * @param maxNumFragments maximum number of fragments
         * @param fragListBuilder {@link FragListBuilder} object
         * @param fragmentsBuilder {@link FragmentsBuilder} object
         * @param preTags pre-tags to be used to highlight terms
         * @param postTags post-tags to be used to highlight terms
         * @param encoder an encoder that generates encoded text
         * @return created fragments or null when no fragments created.
         *         size of the array can be less than maxNumFragments
         * @throws IOException If there is a low-level I/O error
         */
        public string[] GetBestFragments(FieldQuery fieldQuery, IndexReader reader, int docId,
            string fieldName, int fragCharSize, int maxNumFragments,
            IFragListBuilder fragListBuilder, IFragmentsBuilder fragmentsBuilder,
            string[] preTags, string[] postTags, IEncoder encoder)
        {
            FieldFragList fieldFragList =
                GetFieldFragList(fragListBuilder, fieldQuery, reader, docId, fieldName, fragCharSize);
            return fragmentsBuilder.CreateFragments(reader, docId, fieldName, fieldFragList, maxNumFragments,
                preTags, postTags, encoder);
        }

        /**
         * Return the best fragments.  Matches are scanned from matchedFields and turned into fragments against
         * storedField.  The highlighting may not make sense if matchedFields has matches with offsets that don't
         * correspond features in storedField.  It will outright throw a {@code StringIndexOutOfBoundsException}
         * if matchedFields produces offsets outside of storedField.  As such it is advisable that all
         * matchedFields share the same source as storedField or are at least a prefix of it.
         * 
         * @param fieldQuery {@link FieldQuery} object
         * @param reader {@link IndexReader} of the index
         * @param docId document id to be highlighted
         * @param storedField field of the document that stores the text
         * @param matchedFields fields of the document to scan for matches
         * @param fragCharSize the length (number of chars) of a fragment
         * @param maxNumFragments maximum number of fragments
         * @param fragListBuilder {@link FragListBuilder} object
         * @param fragmentsBuilder {@link FragmentsBuilder} object
         * @param preTags pre-tags to be used to highlight terms
         * @param postTags post-tags to be used to highlight terms
         * @param encoder an encoder that generates encoded text
         * @return created fragments or null when no fragments created.
         *         size of the array can be less than maxNumFragments
         * @throws IOException If there is a low-level I/O error
         */
        public string[] GetBestFragments(FieldQuery fieldQuery, IndexReader reader, int docId,
            string storedField, ISet<string> matchedFields, int fragCharSize, int maxNumFragments,
            IFragListBuilder fragListBuilder, IFragmentsBuilder fragmentsBuilder,
            string[] preTags, string[] postTags, IEncoder encoder)
        {
            FieldFragList fieldFragList =
                GetFieldFragList(fragListBuilder, fieldQuery, reader, docId, matchedFields, fragCharSize);
            return fragmentsBuilder.CreateFragments(reader, docId, storedField, fieldFragList, maxNumFragments,
                preTags, postTags, encoder);
        }

        /**
         * Build a FieldFragList for one field.
         */
        private FieldFragList GetFieldFragList(IFragListBuilder fragListBuilder,
            FieldQuery fieldQuery, IndexReader reader, int docId,
            string matchedField, int fragCharSize)
        {
            FieldTermStack fieldTermStack = new FieldTermStack(reader, docId, matchedField, fieldQuery);
            FieldPhraseList fieldPhraseList = new FieldPhraseList(fieldTermStack, fieldQuery, phraseLimit);
            return fragListBuilder.CreateFieldFragList(fieldPhraseList, fragCharSize);
        }

        /**
         * Build a FieldFragList for more than one field.
         */
        private FieldFragList GetFieldFragList(IFragListBuilder fragListBuilder,
            FieldQuery fieldQuery, IndexReader reader, int docId,
            ISet<string> matchedFields, int fragCharSize)
        {
            IEnumerator<string> matchedFieldsItr = matchedFields.GetEnumerator();
            if (!matchedFields.Any())
            {
                throw new ArgumentException("matchedFields must contain at least on field name.");
            }
            FieldPhraseList[]
            toMerge = new FieldPhraseList[matchedFields.Count];
            int i = 0;
            while (matchedFieldsItr.MoveNext())
            {
                FieldTermStack stack = new FieldTermStack(reader, docId, matchedFieldsItr.Current, fieldQuery);
                toMerge[i++] = new FieldPhraseList(stack, fieldQuery, phraseLimit);
            }
            return fragListBuilder.CreateFieldFragList(new FieldPhraseList(toMerge), fragCharSize);
        }

        /**
         * return whether phraseHighlight or not.
         * 
         * @return whether phraseHighlight or not
         */
        public virtual bool IsPhraseHighlight { get { return phraseHighlight; } }

        /**
         * return whether fieldMatch or not.
         * 
         * @return whether fieldMatch or not
         */
        public virtual bool IsFieldMatch { get { return fieldMatch; } }

        /// <summary>
        /// Gets or Sets the maximum number of phrases to analyze when searching for the highest-scoring phrase.
        /// The default is unlimited (int.MaxValue).
        /// </summary>
        public virtual int PhraseLimit
        {
            get { return phraseLimit; }
            set { phraseLimit = value; }
        }
    }
}
