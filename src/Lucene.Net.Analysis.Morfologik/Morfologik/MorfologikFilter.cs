// Lucene version compatibility level 8.2.0
using J2N;
using Lucene.Net.Analysis.Morfologik.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Morfologik.Stemming;
using Morfologik.Stemming.Polish;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Morfologik
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
    /// <see cref="TokenFilter"/> using Morfologik library to transform input tokens into lemma and
    /// morphosyntactic (POS) tokens. Applies to Polish only.
    /// <para/>
    /// MorfologikFilter contains a <see cref="MorphosyntacticTagsAttribute"/>, which provides morphosyntactic
    /// annotations for produced lemmas. See the Morfologik documentation for details.
    /// </summary>
    public class MorfologikFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly IMorphosyntacticTagsAttribute tagsAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IKeywordAttribute keywordAttr;

        private readonly CharsRef scratch = new CharsRef();

        private State current;
        private readonly TokenStream input;
        private readonly IStemmer stemmer;

        private IList<WordData> lemmaList;
        private readonly JCG.List<StringBuilder> tagsList = new JCG.List<StringBuilder>();

        private int lemmaListIndex;

        private static readonly CultureInfo culture = new CultureInfo("pl"); // LUCENENET specific - do lowercasing in Polish culture

        /// <summary>
        /// Creates a filter with the default (Polish) dictionary.
        /// </summary>
        /// <param name="input">Input token stream.</param>
        public MorfologikFilter(TokenStream input)
            : this(input, new PolishStemmer().Dictionary)
        {
        }

        /// <summary>
        /// Creates a filter with a given dictionary.
        /// </summary>
        /// <param name="input">Input token stream.</param>
        /// <param name="dict"><see cref="Dictionary"/> to use for stemming.</param>
        public MorfologikFilter(TokenStream input, Dictionary dict)
            : base(input)
        {
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.tagsAtt = AddAttribute<IMorphosyntacticTagsAttribute>();
            this.posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            this.keywordAttr = AddAttribute<IKeywordAttribute>();

            this.input = input;
            this.stemmer = new DictionaryLookup(dict);
            this.lemmaList = new JCG.List<WordData>();
        }

        /// <summary>
        /// A regex used to split lemma forms.
        /// </summary>
        private readonly static Regex lemmaSplitter = new Regex("\\+|\\|", RegexOptions.Compiled);

        private void PopNextLemma()
        {
            // One tag (concatenated) per lemma.
            WordData lemma = lemmaList[lemmaListIndex++];
            termAtt.SetEmpty().Append(lemma.GetStem().ToString());
            var tag = lemma.GetTag();
            if (tag != null)
            {
                string[] tags = lemmaSplitter.Split(tag.ToString());
                for (int i = 0; i < tags.Length; i++)
                {
                    if (tagsList.Count <= i)
                    {
                        tagsList.Add(new StringBuilder());
                    }
                    StringBuilder buffer = tagsList[i];
                    buffer.Length = 0;
                    buffer.Append(tags[i]);
                }
                tagsAtt.Tags = tagsList.GetView(0, tags.Length - 0); // LUCENENET: Converted end index to length
            }
            else
            {
                tagsAtt.Tags = Collections.EmptyList<StringBuilder>();
            }
        }

        /// <summary>
        /// Lookup a given surface form of a token and update
        /// <see cref="lemmaList"/> and <see cref="lemmaListIndex"/> accordingly.
        /// </summary>
        private bool LookupSurfaceForm(string token)
        {
            lemmaList = this.stemmer.Lookup(token);
            lemmaListIndex = 0;
            return lemmaList.Count > 0;
        }

        /// <summary>Retrieves the next token (possibly from the list of lemmas).</summary>
        public override sealed bool IncrementToken()
        {
            if (lemmaListIndex < lemmaList.Count)
            {
                RestoreState(current);
                posIncrAtt.PositionIncrement = 0;
                PopNextLemma();
                return true;
            }
            else if (this.input.IncrementToken())
            {
                if (!keywordAttr.IsKeyword &&
                    (LookupSurfaceForm(termAtt.ToString()) || LookupSurfaceForm(ToLowercase(termAtt.ToString()))))
                {
                    current = CaptureState();
                    PopNextLemma();
                }
                else
                {
                    tagsAtt.Clear();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>Convert to lowercase in-place.</summary>
        private string ToLowercase(string chs)
        {
            int length = chs.Length;
            scratch.Length = length;
            scratch.Grow(length);

            char[] buffer = scratch.Chars;
            for (int i = 0; i < length;)
            {
                i += Character.ToChars(
                    Character.ToLower(Character.CodePointAt(chs, i), culture), buffer, i); // LUCENENET specific - need to use explicit culture to override current thread
            }

            return scratch.ToString();
        }

        /// <summary>Resets stems accumulator and hands over to superclass.</summary>
        public override void Reset()
        {
            lemmaListIndex = 0;
            lemmaList = new JCG.List<WordData>();
            tagsList.Clear();
            base.Reset();
        }
    }
}
