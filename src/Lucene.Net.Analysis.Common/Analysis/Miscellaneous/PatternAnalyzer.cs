// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Efficient Lucene analyzer/tokenizer that preferably operates on a <see cref="string"/> rather than a
    /// <see cref="TextReader"/>, that can flexibly separate text into terms via a regular expression <see cref="Regex"/>
    /// (with behaviour similar to <see cref="Regex.Split(string)"/>),
    /// and that combines the functionality of
    /// <see cref="LetterTokenizer"/>,
    /// <see cref="LowerCaseTokenizer"/>,
    /// <see cref="WhitespaceTokenizer"/>,
    /// <see cref="StopFilter"/> into a single efficient
    /// multi-purpose class.
    /// <para>
    /// If you are unsure how exactly a regular expression should look like, consider 
    /// prototyping by simply trying various expressions on some test texts via
    /// <see cref="Regex.Split(string)"/>. Once you are satisfied, give that regex to 
    /// <see cref="PatternAnalyzer"/>. Also see <a target="_blank" 
    /// href="http://www.regular-expressions.info/">Regular Expression Tutorial</a>.
    /// </para>
    /// <para>
    /// This class can be considerably faster than the "normal" Lucene tokenizers. 
    /// It can also serve as a building block in a compound Lucene
    /// <see cref="TokenFilter"/> chain. For example as in this 
    /// stemming example:
    /// <code>
    /// PatternAnalyzer pat = ...
    /// TokenStream tokenStream = new SnowballFilter(
    ///     pat.GetTokenStream("content", "James is running round in the woods"), 
    ///     "English"));
    /// </code>
    /// </para>
    /// </summary>
    /// @deprecated (4.0) use the pattern-based analysis in the analysis/pattern package instead. 
    [Obsolete("(4.0) use the pattern-based analysis in the analysis/pattern package instead.")]
    public sealed class PatternAnalyzer : Analyzer
    {
        /// <summary>
        /// <c>"\\W+"</c>; Divides text at non-letters (NOT Character.isLetter(c)) </summary>
        public static readonly Regex NON_WORD_PATTERN = new Regex("\\W+", RegexOptions.Compiled);

        /// <summary>
        /// <c>"\\s+"</c>; Divides text at whitespaces (Character.isWhitespace(c)) </summary>
        public static readonly Regex WHITESPACE_PATTERN = new Regex("\\s+", RegexOptions.Compiled);

        private static readonly CharArraySet EXTENDED_ENGLISH_STOP_WORDS =
            new CharArraySet(LuceneVersion.LUCENE_CURRENT,
                new string[] {
                    "a", "about", "above", "across", "adj", "after", "afterwards",
                    "again", "against", "albeit", "all", "almost", "alone", "along",
                    "already", "also", "although", "always", "among", "amongst", "an",
                    "and", "another", "any", "anyhow", "anyone", "anything",
                    "anywhere", "are", "around", "as", "at", "be", "became", "because",
                    "become", "becomes", "becoming", "been", "before", "beforehand",
                    "behind", "being", "below", "beside", "besides", "between",
                    "beyond", "both", "but", "by", "can", "cannot", "co", "could",
                    "down", "during", "each", "eg", "either", "else", "elsewhere",
                    "enough", "etc", "even", "ever", "every", "everyone", "everything",
                    "everywhere", "except", "few", "first", "for", "former",
                    "formerly", "from", "further", "had", "has", "have", "he", "hence",
                    "her", "here", "hereafter", "hereby", "herein", "hereupon", "hers",
                    "herself", "him", "himself", "his", "how", "however", "i", "ie", "if",
                    "in", "inc", "indeed", "into", "is", "it", "its", "itself", "last",
                    "latter", "latterly", "least", "less", "ltd", "many", "may", "me",
                    "meanwhile", "might", "more", "moreover", "most", "mostly", "much",
                    "must", "my", "myself", "namely", "neither", "never",
                    "nevertheless", "next", "no", "nobody", "none", "noone", "nor",
                    "not", "nothing", "now", "nowhere", "of", "off", "often", "on",
                    "once one", "only", "onto", "or", "other", "others", "otherwise",
                    "our", "ours", "ourselves", "out", "over", "own", "per", "perhaps",
                    "rather", "s", "same", "seem", "seemed", "seeming", "seems",
                    "several", "she", "should", "since", "so", "some", "somehow",
                    "someone", "something", "sometime", "sometimes", "somewhere",
                    "still", "such", "t", "than", "that", "the", "their", "them",
                    "themselves", "then", "thence", "there", "thereafter", "thereby",
                    "therefor", "therein", "thereupon", "these", "they", "this",
                    "those", "though", "through", "throughout", "thru", "thus", "to",
                    "together", "too", "toward", "towards", "under", "until", "up",
                    "upon", "us", "very", "via", "was", "we", "well", "were", "what",
                    "whatever", "whatsoever", "when", "whence", "whenever",
                    "whensoever", "where", "whereafter", "whereas", "whereat",
                    "whereby", "wherefrom", "wherein", "whereinto", "whereof",
                    "whereon", "whereto", "whereunto", "whereupon", "wherever",
                    "wherewith", "whether", "which", "whichever", "whichsoever",
                    "while", "whilst", "whither", "who", "whoever", "whole", "whom",
                    "whomever", "whomsoever", "whose", "whosoever", "why", "will",
                    "with", "within", "without", "would", "xsubj", "xcal", "xauthor",
                    "xother ", "xnote", "yet", "you", "your", "yours", "yourself",
                    "yourselves"
                    }, true).AsReadOnly();

        /// <summary>
        /// A lower-casing word analyzer with English stop words (can be shared
        /// freely across threads without harm); global per class loader.
        /// </summary>
        public static readonly PatternAnalyzer DEFAULT_ANALYZER = new PatternAnalyzer(
            LuceneVersion.LUCENE_CURRENT, NON_WORD_PATTERN, true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);

        /// <summary>
        /// A lower-casing word analyzer with <b>extended</b> English stop words
        /// (can be shared freely across threads without harm); global per class
        /// loader. The stop words are borrowed from
        /// http://thomas.loc.gov/home/stopwords.html, see
        /// http://thomas.loc.gov/home/all.about.inquery.html
        /// </summary>
        public static readonly PatternAnalyzer EXTENDED_ANALYZER = new PatternAnalyzer(
            LuceneVersion.LUCENE_CURRENT, NON_WORD_PATTERN, true, EXTENDED_ENGLISH_STOP_WORDS);

        private readonly Regex pattern;
        private readonly bool toLowerCase;
        private readonly CharArraySet stopWords;

        private readonly LuceneVersion matchVersion;

        /// <summary>
        /// Constructs a new instance with the given parameters.
        /// </summary>
        /// <param name="matchVersion"> currently does nothing </param>
        /// <param name="pattern">
        ///            a regular expression delimiting tokens </param>
        /// <param name="toLowerCase">
        ///            if <code>true</code> returns tokens after applying
        ///            String.toLowerCase() </param>
        /// <param name="stopWords">
        ///            if non-null, ignores all tokens that are contained in the
        ///            given stop set (after previously having applied toLowerCase()
        ///            if applicable). For example, created via
        ///            <see cref="StopFilter.MakeStopSet(LuceneVersion, string[])"/>and/or
        ///            <see cref="WordlistLoader"/>as in
        ///            <code>WordlistLoader.getWordSet(new File("samples/fulltext/stopwords.txt")</code>
        ///            or <a href="http://www.unine.ch/info/clef/">other stop words
        ///            lists </a>. </param>
        public PatternAnalyzer(LuceneVersion matchVersion, Regex pattern, bool toLowerCase, CharArraySet stopWords)
        {
            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern), "pattern must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            if (EqPattern(NON_WORD_PATTERN, pattern))
            {
                pattern = NON_WORD_PATTERN;
            }
            else if (EqPattern(WHITESPACE_PATTERN, pattern))
            {
                pattern = WHITESPACE_PATTERN;
            }

            if (stopWords != null && stopWords.Count == 0)
            {
                stopWords = null;
            }

            this.pattern = pattern;
            this.toLowerCase = toLowerCase;
            this.stopWords = stopWords;
            this.matchVersion = matchVersion;
        }

        /// <summary>
        /// Creates a token stream that tokenizes the given string into token terms
        /// (aka words).
        /// </summary>
        /// <param name="fieldName">
        ///            the name of the field to tokenize (currently ignored). </param>
        /// <param name="reader">
        ///            reader (e.g. charfilter) of the original text. can be null. </param>
        /// <param name="text">
        ///            the string to tokenize </param>
        /// <returns> a new token stream </returns>
        public TokenStreamComponents CreateComponents(string fieldName, TextReader reader, string text)
        {
            // Ideally the Analyzer superclass should have a method with the same signature, 
            // with a default impl that simply delegates to the StringReader flavour. 
            if (reader is null)
            {
                reader = new FastStringReader(text);
            }

            if (pattern == NON_WORD_PATTERN) // fast path
            {
                return new TokenStreamComponents(new FastStringTokenizer(reader, true, toLowerCase, stopWords));
            } // fast path
            else if (pattern == WHITESPACE_PATTERN)
            {
                return new TokenStreamComponents(new FastStringTokenizer(reader, false, toLowerCase, stopWords));
            }

            Tokenizer tokenizer = new PatternTokenizer(reader, pattern, toLowerCase);
            TokenStream result = (stopWords != null) ? (TokenStream)new StopFilter(matchVersion, tokenizer, stopWords) : tokenizer;
            return new TokenStreamComponents(tokenizer, result);
        }

        /// <summary>
        /// Creates a token stream that tokenizes all the text in the given SetReader;
        /// This implementation forwards to <see cref="Analyzer.GetTokenStream(string, TextReader)"/> and is
        /// less efficient than <see cref="Analyzer.GetTokenStream(string, TextReader)"/>.
        /// </summary>
        /// <param name="fieldName">
        ///            the name of the field to tokenize (currently ignored). </param>
        /// <param name="reader">
        ///            the reader delivering the text </param>
        /// <returns> a new token stream </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return CreateComponents(fieldName, reader, null);
        }

        /// <summary>
        /// Indicates whether some other object is "equal to" this one.
        /// </summary>
        /// <param name="other">
        ///            the reference object with which to compare. </param>
        /// <returns> true if equal, false otherwise </returns>
        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }
            if (this == DEFAULT_ANALYZER && other == EXTENDED_ANALYZER)
            {
                return false;
            }
            if (other == DEFAULT_ANALYZER && this == EXTENDED_ANALYZER)
            {
                return false;
            }

            var p2 = other as PatternAnalyzer;
            if (p2 != null)
            {
                return toLowerCase == p2.toLowerCase && EqPattern(pattern, p2.pattern) && Eq(stopWords, p2.stopWords);
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code value for the object.
        /// </summary>
        /// <returns> the hash code. </returns>
        public override int GetHashCode()
        {
            if (this == DEFAULT_ANALYZER) // fast path
            {
                return -1218418418;
            }
            if (this == EXTENDED_ANALYZER) // fast path
            {
                return 1303507063;
            }

            int h = 1;
            h = 31 * h + pattern.ToString().GetHashCode();
            h = 31 * h + (int)pattern.Options;
            h = 31 * h + (toLowerCase ? 1231 : 1237);
            h = 31 * h + (stopWords != null ? stopWords.GetHashCode() : 0);
            return h;
        }

        /// <summary>
        /// equality where o1 and/or o2 can be null </summary>
        private static bool Eq(object o1, object o2)
        {
            return (o1 == o2) || (o1 != null ? o1.Equals(o2) : false);
        }

        /// <summary>
        /// assumes p1 and p2 are not null </summary>
        private static bool EqPattern(Regex p1, Regex p2)
        {
            return p1 == p2 || (p1.Options == p2.Options && p1.ToString().Equals(p2.ToString(), StringComparison.Ordinal));
        }

        /// <summary>
        /// Reads until end-of-stream and returns all read chars, finally closes the stream.
        /// </summary>
        /// <param name="input"> the input stream </param>
        /// <exception cref="IOException"> if an I/O error occurs while reading the stream </exception>
        private static string ToString(TextReader input)
        {
            var reader = input as FastStringReader;
            if (reader != null) // fast path
            {
                return reader.String;
            }

            try
            {
                int len = 256;
                char[] buffer = new char[len];
                char[] output = new char[len];

                len = 0;
                int n;
                while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (len + n > output.Length) // grow capacity
                    {
                        char[] tmp = new char[Math.Max(output.Length << 1, len + n)];
                        Arrays.Copy(output, 0, tmp, 0, len);
                        Arrays.Copy(buffer, 0, tmp, len, n);
                        buffer = output; // use larger buffer for future larger bulk reads
                        output = tmp;
                    }
                    else
                    {
                        Arrays.Copy(buffer, 0, output, len, n);
                    }
                    len += n;
                }

                return new string(output, 0, len);
            }
            finally
            {
                input.Dispose();
            }
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Nested classes:
        ///////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// The work horse; performance isn't fantastic, but it's not nearly as bad
        /// as one might think - kudos to the Sun regex developers.
        /// </summary>
        private sealed class PatternTokenizer : Tokenizer
        {
            private readonly Regex pattern;
            private string str;
            private readonly bool toLowerCase;
            private Match matcher;
            private int pos = 0;
            private bool initialized = false;
            private bool isReset = false; // Flag to keep track of the first match vs subsequent matches
            private readonly ICharTermAttribute termAtt;
            private readonly IOffsetAttribute offsetAtt;

            public PatternTokenizer(TextReader input, Regex pattern, bool toLowerCase)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
                this.pattern = pattern;
                this.matcher = pattern.Match("");
                this.toLowerCase = toLowerCase;
            }

            public override bool IncrementToken()
            {
                if (!initialized)
                {
                    throw IllegalStateException.Create("Consumer did not call Reset().");
                }
                if (matcher is null)
                {
                    return false;
                }
                ClearAttributes();
                while (true) // loop takes care of leading and trailing boundary cases
                {
                    int start = pos;
                    int end;
                    if (!isReset)
                    {
                        matcher = matcher.NextMatch();
                    }
                    isReset = false;
                    bool isMatch = matcher.Success;
                    if (isMatch)
                    {
                        end = matcher.Index;
                        pos = matcher.Index + matcher.Length;
                    }
                    else
                    {
                        end = str.Length;
                        matcher = null; // we're finished
                    }

                    if (start != end) // non-empty match (header/trailer)
                    {
                        string text = str.Substring(start, end - start);
                        if (toLowerCase)
                        {
                            text = text.ToLower(); // LUCENENET: Since this class is obsolete, we aren't going to bother with passing culture in the constructor.
                        }
                        termAtt.SetEmpty().Append(text);
                        offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(end));
                        return true;
                    }
                    if (!isMatch)
                    {
                        return false;
                    }
                }
            }

            public override void End()
            {
                base.End();
                // set final offset
                int finalOffset = CorrectOffset(str.Length);
                this.offsetAtt.SetOffset(finalOffset, finalOffset);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    this.initialized = false;
                }
            }

            public override void Reset()
            {
                base.Reset();
                this.str = PatternAnalyzer.ToString(m_input);

                // LUCENENET: Since we need to "reset" the Match
                // object, we also need an "isReset" flag to indicate
                // whether we are at the head of the match and to 
                // take the appropriate measures to ensure we don't 
                // overwrite our matcher variable with 
                // matcher = matcher.NextMatch();
                // before it is time. A string could potentially
                // match on index 0, so we need another variable to
                // manage this state.
                this.matcher = pattern.Match(this.str);
                this.isReset = true;
                this.pos = 0;
                this.initialized = true;
            }
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Nested classes:
        ///////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Special-case class for best performance in common cases; this class is
        /// otherwise unnecessary.
        /// </summary>
        private sealed class FastStringTokenizer : Tokenizer
        {
            private string str;
            private int pos;
            private readonly bool isLetter;
            private readonly bool toLowerCase;
            private readonly CharArraySet stopWords;
            private readonly ICharTermAttribute termAtt;
            private readonly IOffsetAttribute offsetAtt;

            public FastStringTokenizer(TextReader input, bool isLetter, bool toLowerCase, CharArraySet stopWords)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();

                this.isLetter = isLetter;
                this.toLowerCase = toLowerCase;
                this.stopWords = stopWords;
            }

            public override bool IncrementToken()
            {
                if (str is null)
                {
                    throw IllegalStateException.Create("Consumer did not call Reset().");
                }
                ClearAttributes();
                // cache loop instance vars (performance)
                string s = str;
                int len = s.Length;
                int i = pos;
                bool letter = isLetter;

                int start = 0;
                string text;
                do
                {
                    // find beginning of token
                    text = null;
                    while (i < len && !IsTokenChar(s[i], letter))
                    {
                        i++;
                    }

                    if (i < len) // found beginning; now find end of token
                    {
                        start = i;
                        while (i < len && IsTokenChar(s[i], letter))
                        {
                            i++;
                        }

                        text = s.Substring(start, i - start);
                        if (toLowerCase)
                        {
                            text = text.ToLower(); // LUCENENET: Since this class is obsolete, we aren't going to bother with passing culture in the constructor.
                        }
                        //          if (toLowerCase) {            
                        ////            use next line once JDK 1.5 String.toLowerCase() performance regression is fixed
                        ////            see http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6265809
                        //            text = s.substring(start, i).toLowerCase(); 
                        ////            char[] chars = new char[i-start];
                        ////            for (int j=start; j < i; j++) chars[j-start] = Character.toLowerCase(s.charAt(j));
                        ////            text = new String(chars);
                        //          } else {
                        //            text = s.substring(start, i);
                        //          }
                    }
                } while (text != null && IsStopWord(text));

                pos = i;
                if (text is null)
                {
                    return false;
                }
                termAtt.SetEmpty().Append(text);
                offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(i));
                return true;
            }

            public override void End()
            {
                base.End();
                // set final offset
                int finalOffset = str.Length;
                this.offsetAtt.SetOffset(CorrectOffset(finalOffset), CorrectOffset(finalOffset));
            }

            private bool IsTokenChar(char c, bool isLetter)
            {
                return isLetter ? char.IsLetter(c) : !char.IsWhiteSpace(c);
            }

            private bool IsStopWord(string text)
            {
                return stopWords != null && stopWords.Contains(text);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    this.str = null;
                }
            }

            public override void Reset()
            {
                base.Reset();
                this.str = PatternAnalyzer.ToString(m_input);
                this.pos = 0;
            }
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Nested classes:
        ///////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// A <see cref="StringReader"/> that exposes it's contained string for fast direct access.
        /// Might make sense to generalize this to ICharSequence and make it public?
        /// </summary>
        internal sealed class FastStringReader : StringReader
        {
            private readonly string s;

            internal FastStringReader(string s)
                : base(s)
            {
                this.s = s;
            }

            internal string String => s;
        }
    }
}