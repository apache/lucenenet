// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Core
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
    // jdocs


    /// <summary>
    /// Factory for <see cref="StopFilter"/>.
    /// 
    /// <code>
    /// &lt;fieldType name="text_stop" class="solr.TextField" positionIncrementGap="100" autoGeneratePhraseQueries="true"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.StopFilterFactory" ignoreCase="true"
    ///             words="stopwords.txt" format="wordset" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// <para>
    /// All attributes are optional:
    /// </para>
    /// <list type="bullet">
    ///     <item><description><c>ignoreCase</c> defaults to <c>false</c></description></item>
    ///     <item><description><c>words</c> should be the name of a stopwords file to parse, if not 
    ///      specified the factory will use <see cref="StopAnalyzer.ENGLISH_STOP_WORDS_SET"/>
    ///     </description></item>
    ///     <item><description><c>format</c> defines how the <c>words</c> file will be parsed, 
    ///      and defaults to <c>wordset</c>.  If <c>words</c> is not specified, 
    ///      then <c>format</c> must not be specified.
    ///     </description></item>
    /// </list>
    /// <para>
    /// The valid values for the <c>format</c> option are:
    /// </para>
    /// <list type="bullet">
    ///  <item><description><c>wordset</c> - This is the default format, which supports one word per 
    ///      line (including any intra-word whitespace) and allows whole line comments 
    ///      begining with the "#" character.  Blank lines are ignored.  See 
    ///      <see cref="WordlistLoader.GetLines"/> for details.
    ///  </description></item>
    ///  <item><description><c>snowball</c> - This format allows for multiple words specified on each 
    ///      line, and trailing comments may be specified using the vertical line ("&#124;"). 
    ///      Blank lines are ignored.  See 
    ///      <see cref="WordlistLoader.GetSnowballWordSet(TextReader, Net.Util.LuceneVersion)"/> 
    ///      for details.
    ///  </description></item>
    /// </list>
    /// </summary>
    public class StopFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public const string FORMAT_WORDSET = "wordset";
        public const string FORMAT_SNOWBALL = "snowball";

        private CharArraySet stopWords;
        private readonly string stopWordFiles;
        private readonly string format;
        private readonly bool ignoreCase;
        private readonly bool enablePositionIncrements;

        /// <summary>
        /// Creates a new <see cref="StopFilterFactory"/> </summary>
        public StopFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            AssureMatchVersion();
            stopWordFiles = Get(args, "words");
            format = Get(args, "format", (null == stopWordFiles ? null : FORMAT_WORDSET));
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            enablePositionIncrements = GetBoolean(args, "enablePositionIncrements", true);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (stopWordFiles != null)
            {
                if (FORMAT_WORDSET.Equals(format, StringComparison.OrdinalIgnoreCase))
                {
                    stopWords = GetWordSet(loader, stopWordFiles, ignoreCase);
                }
                else if (FORMAT_SNOWBALL.Equals(format, StringComparison.OrdinalIgnoreCase))
                {
                    stopWords = GetSnowballWordSet(loader, stopWordFiles, ignoreCase);
                }
                else
                {
                    throw new ArgumentException("Unknown 'format' specified for 'words' file: " + format);
                }
            }
            else
            {
                if (null != format)
                {
                    throw new ArgumentException("'format' can not be specified w/o an explicit 'words' file: " + format);
                }
                stopWords = new CharArraySet(m_luceneMatchVersion, StopAnalyzer.ENGLISH_STOP_WORDS_SET, ignoreCase);
            }
        }

        public virtual bool EnablePositionIncrements => enablePositionIncrements;

        public virtual bool IgnoreCase => ignoreCase;

        public virtual CharArraySet StopWords => stopWords;

        public override TokenStream Create(TokenStream input)
        {
            StopFilter stopFilter = new StopFilter(m_luceneMatchVersion, input, stopWords);
#pragma warning disable 612, 618
            stopFilter.SetEnablePositionIncrements(enablePositionIncrements);
#pragma warning restore 612, 618
            return stopFilter;
        }
    }
}