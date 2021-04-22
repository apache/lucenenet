using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// the default constructor.
        /// </summary>
        public FastVectorHighlighter()
            : this(DEFAULT_PHRASE_HIGHLIGHT, DEFAULT_FIELD_MATCH)
        {
        }

        /// <summary>
        /// a constructor. Using <see cref="SimpleFragListBuilder"/> and <see cref="ScoreOrderFragmentsBuilder"/>.
        /// </summary>
        /// <param name="phraseHighlight">true or false for phrase highlighting</param>
        /// <param name="fieldMatch">true of false for field matching</param>
        public FastVectorHighlighter(bool phraseHighlight, bool fieldMatch)
            : this(phraseHighlight, fieldMatch, new SimpleFragListBuilder(), new ScoreOrderFragmentsBuilder())
        {
        }

        /// <summary>
        /// a constructor. A <see cref="IFragListBuilder"/> and a <see cref="IFragmentsBuilder"/> can be specified (plugins).
        /// </summary>
        /// <param name="phraseHighlight">true of false for phrase highlighting</param>
        /// <param name="fieldMatch">true of false for field matching</param>
        /// <param name="fragListBuilder">an instance of <see cref="IFragmentsBuilder"/></param>
        /// <param name="fragmentsBuilder">an instance of <see cref="IFragmentsBuilder"/></param>
        public FastVectorHighlighter(bool phraseHighlight, bool fieldMatch,
            IFragListBuilder fragListBuilder, IFragmentsBuilder fragmentsBuilder)
        {
            this.phraseHighlight = phraseHighlight;
            this.fieldMatch = fieldMatch;
            this.fragListBuilder = fragListBuilder;
            this.fragmentsBuilder = fragmentsBuilder;
        }

        /// <summary>
        /// create a <see cref="FieldQuery"/> object.
        /// </summary>
        /// <param name="query">a query</param>
        /// <returns>the created <see cref="FieldQuery"/> object</returns>
        public virtual FieldQuery GetFieldQuery(Query query)
        {
            // TODO: should we deprecate this? 
            // because if there is no reader, then we cannot rewrite MTQ.
            try
            {
                return new FieldQuery(query, null, phraseHighlight, fieldMatch);
            }
            catch (Exception e) when (e.IsIOException())
            {
                // should never be thrown when reader is null
                throw RuntimeException.Create(e);
            }
        }

        /// <summary>
        /// create a <see cref="FieldQuery"/> object.
        /// </summary>
        /// <param name="query">a query</param>
        /// <param name="reader"></param>
        /// <returns>the created <see cref="FieldQuery"/> object</returns>
        public virtual FieldQuery GetFieldQuery(Query query, IndexReader reader)
        {
            return new FieldQuery(query, reader, phraseHighlight, fieldMatch);
        }

        /// <summary>
        /// return the best fragment.
        /// </summary>
        /// <param name="fieldQuery"><see cref="FieldQuery"/> object</param>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
        /// <returns>the best fragment (snippet) string</returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        public string GetBestFragment(FieldQuery fieldQuery, IndexReader reader, int docId,
            string fieldName, int fragCharSize)
        {
            FieldFragList fieldFragList =
                GetFieldFragList(fragListBuilder, fieldQuery, reader, docId, fieldName, fragCharSize);
            return fragmentsBuilder.CreateFragment(reader, docId, fieldName, fieldFragList);
        }

        /// <summary>
        /// return the best fragments.
        /// </summary>
        /// <param name="fieldQuery"><see cref="FieldQuery"/> object</param>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
        /// <param name="maxNumFragments">maximum number of fragments</param>
        /// <returns>
        /// created fragments or null when no fragments created.
        /// size of the array can be less than maxNumFragments
        /// </returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        public string[] GetBestFragments(FieldQuery fieldQuery, IndexReader reader, int docId,
            string fieldName, int fragCharSize, int maxNumFragments)
        {
            FieldFragList fieldFragList =
                GetFieldFragList(fragListBuilder, fieldQuery, reader, docId, fieldName, fragCharSize);
            return fragmentsBuilder.CreateFragments(reader, docId, fieldName, fieldFragList, maxNumFragments);
        }

        /// <summary>
        /// return the best fragment.
        /// </summary>
        /// <param name="fieldQuery"><see cref="FieldQuery"/> object</param>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
        /// <param name="fragListBuilder"><see cref="IFragListBuilder"/> object</param>
        /// <param name="fragmentsBuilder"><see cref="IFragmentsBuilder"/> object</param>
        /// <param name="preTags">pre-tags to be used to highlight terms</param>
        /// <param name="postTags">post-tags to be used to highlight terms</param>
        /// <param name="encoder">an encoder that generates encoded text</param>
        /// <returns>the best fragment (snippet) string</returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        public string GetBestFragment(FieldQuery fieldQuery, IndexReader reader, int docId,
            string fieldName, int fragCharSize,
            IFragListBuilder fragListBuilder, IFragmentsBuilder fragmentsBuilder,
            string[] preTags, string[] postTags, IEncoder encoder)
        {
            FieldFragList fieldFragList = GetFieldFragList(fragListBuilder, fieldQuery, reader, docId, fieldName, fragCharSize);
            return fragmentsBuilder.CreateFragment(reader, docId, fieldName, fieldFragList, preTags, postTags, encoder);
        }

        /// <summary>
        /// return the best fragments.
        /// </summary>
        /// <param name="fieldQuery"><see cref="FieldQuery"/> object</param>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
        /// <param name="maxNumFragments">maximum number of fragments</param>
        /// <param name="fragListBuilder"><see cref="IFragListBuilder"/> object</param>
        /// <param name="fragmentsBuilder"><see cref="IFragmentsBuilder"/> object</param>
        /// <param name="preTags">pre-tags to be used to highlight terms</param>
        /// <param name="postTags">post-tags to be used to highlight terms</param>
        /// <param name="encoder">an encoder that generates encoded text</param>
        /// <returns>
        /// created fragments or null when no fragments created.
        /// size of the array can be less than maxNumFragments
        /// </returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
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

        /// <summary>
        /// Return the best fragments.  Matches are scanned from <paramref name="matchedFields"/> and turned into fragments against
        /// <paramref name="storedField"/>.  The highlighting may not make sense if <paramref name="matchedFields"/> has matches with offsets that don't
        /// correspond features in <paramref name="storedField"/>.  It will outright throw a <see cref="IndexOutOfRangeException"/>
        /// if <paramref name="matchedFields"/> produces offsets outside of <paramref name="storedField"/>.  As such it is advisable that all
        /// <paramref name="matchedFields"/> share the same source as <paramref name="storedField"/> or are at least a prefix of it.
        /// </summary>
        /// <param name="fieldQuery"><see cref="FieldQuery"/> object</param>
        /// <param name="reader"><see cref="IndexReader"/> of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="storedField">field of the document that stores the text</param>
        /// <param name="matchedFields">fields of the document to scan for matches</param>
        /// <param name="fragCharSize">the length (number of chars) of a fragment</param>
        /// <param name="maxNumFragments">maximum number of fragments</param>
        /// <param name="fragListBuilder"><see cref="IFragListBuilder"/> object</param>
        /// <param name="fragmentsBuilder"><see cref="IFragmentsBuilder"/> object</param>
        /// <param name="preTags">pre-tags to be used to highlight terms</param>
        /// <param name="postTags">post-tags to be used to highlight terms</param>
        /// <param name="encoder">an encoder that generates encoded text</param>
        /// <returns>
        /// created fragments or null when no fragments created.
        /// size of the array can be less than <paramref name="maxNumFragments"/>
        /// </returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
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

        /// <summary>
        /// Build a <see cref="FieldFragList"/> for one field.
        /// </summary>
        private FieldFragList GetFieldFragList(IFragListBuilder fragListBuilder,
            FieldQuery fieldQuery, IndexReader reader, int docId,
            string matchedField, int fragCharSize)
        {
            FieldTermStack fieldTermStack = new FieldTermStack(reader, docId, matchedField, fieldQuery);
            FieldPhraseList fieldPhraseList = new FieldPhraseList(fieldTermStack, fieldQuery, phraseLimit);
            return fragListBuilder.CreateFieldFragList(fieldPhraseList, fragCharSize);
        }

        /// <summary>
        /// Build a <see cref="FieldFragList"/> for more than one field.
        /// </summary>
        private FieldFragList GetFieldFragList(IFragListBuilder fragListBuilder,
            FieldQuery fieldQuery, IndexReader reader, int docId,
            ISet<string> matchedFields, int fragCharSize)
        {
            if (matchedFields.Count == 0)
            {
                throw new ArgumentException("matchedFields must contain at least on field name.");
            }
            FieldPhraseList[]
            toMerge = new FieldPhraseList[matchedFields.Count];
            int i = 0;
            foreach (var matchedField in matchedFields)
            {
                FieldTermStack stack = new FieldTermStack(reader, docId, matchedField, fieldQuery);
                toMerge[i++] = new FieldPhraseList(stack, fieldQuery, phraseLimit);
            }
            return fragListBuilder.CreateFieldFragList(new FieldPhraseList(toMerge), fragCharSize);
        }

        /// <summary>
        /// return whether phraseHighlight or not.
        /// </summary>
        public virtual bool IsPhraseHighlight => phraseHighlight;

        /// <summary>
        /// return whether fieldMatch or not.
        /// </summary>
        public virtual bool IsFieldMatch => fieldMatch;

        /// <summary>
        /// Gets or Sets the maximum number of phrases to analyze when searching for the highest-scoring phrase.
        /// The default is unlimited (int.MaxValue).
        /// </summary>
        public virtual int PhraseLimit
        {
            get => phraseLimit;
            set => phraseLimit = value;
        }
    }
}
