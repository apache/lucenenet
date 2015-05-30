using System;
using System.IO;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

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
    /// Efficient Lucene analyzer/tokenizer that preferably operates on a String rather than a
    /// <seealso cref="TextReader"/>, that can flexibly separate text into terms via a regular expression <seealso cref="Pattern"/>
    /// (with behaviour identical to <seealso cref="String#split(String)"/>),
    /// and that combines the functionality of
    /// <seealso cref="LetterTokenizer"/>,
    /// <seealso cref="LowerCaseTokenizer"/>,
    /// <seealso cref="WhitespaceTokenizer"/>,
    /// <seealso cref="StopFilter"/> into a single efficient
    /// multi-purpose class.
    /// <para>
    /// If you are unsure how exactly a regular expression should look like, consider 
    /// prototyping by simply trying various expressions on some test texts via
    /// <seealso cref="String#split(String)"/>. Once you are satisfied, give that regex to 
    /// PatternAnalyzer. Also see <a target="_blank" 
    /// href="http://java.sun.com/docs/books/tutorial/extra/regex/">Java Regular Expression Tutorial</a>.
    /// </para>
    /// <para>
    /// This class can be considerably faster than the "normal" Lucene tokenizers. 
    /// It can also serve as a building block in a compound Lucene
    /// <seealso cref="TokenFilter"/> chain. For example as in this 
    /// stemming example:
    /// <pre>
    /// PatternAnalyzer pat = ...
    /// TokenStream tokenStream = new SnowballFilter(
    ///     pat.tokenStream("content", "James is running round in the woods"), 
    ///     "English"));
    /// </pre>
    /// </para>
    /// </summary>
    /// @deprecated (4.0) use the pattern-based analysis in the analysis/pattern package instead. 
    [Obsolete("(4.0) use the pattern-based analysis in the analysis/pattern package instead.")]
    public sealed class PatternAnalyzer : Analyzer
    {

        /// <summary>
        /// <code>"\\W+"</code>; Divides text at non-letters (NOT Character.isLetter(c)) </summary>
        public static readonly Pattern NON_WORD_PATTERN = Pattern.compile("\\W+");

        /// <summary>
        /// <code>"\\s+"</code>; Divides text at whitespaces (Character.isWhitespace(c)) </summary>
        public static readonly Pattern WHITESPACE_PATTERN = Pattern.compile("\\s+");

        private static readonly CharArraySet EXTENDED_ENGLISH_STOP_WORDS = CharArraySet.UnmodifiableSet(new CharArraySet(LuceneVersion.LUCENE_CURRENT, Arrays.asList("a", "about", "above", "across", "adj", "after", "afterwards", "again", "against", "albeit", "all", "almost", "alone", "along", "already", "also", "although", "always", "among", "amongst", "an", "and", "another", "any", "anyhow", "anyone", "anything", "anywhere", "are", "around", "as", "at", "be", "became", "because", "become", "becomes", "becoming", "been", "before", "beforehand", "behind", "being", "below", "beside", "besides", "between", "beyond", "both", "but", "by", "can", "cannot", "co", "could", "down", "during", "each", "eg", "either", "else", "elsewhere", "enough", "etc", "even", "ever", "every", "everyone", "everything", "everywhere", "except", "few", "first", "for", "former", "formerly", "from", "further", "had", "has", "have", "he", "hence", "her", "here", "hereafter", "hereby", "herein", "hereupon", "hers", "herself", "him", "himself", "his", "how", "however", "i", "ie", "if", "in", "inc", "indeed", "into", "is", "it", "its", "itself", "last", "latter", "latterly", "least", "less", "ltd", "many", "may", "me", "meanwhile", "might", "more", "moreover", "most", "mostly", "much", "must", "my", "myself", "namely", "neither", "never", "nevertheless", "next", "no", "nobody", "none", "noone", "nor", "not", "nothing", "now", "nowhere", "of", "off", "often", "on", "once one", "only", "onto", "or", "other", "others", "otherwise", "our", "ours", "ourselves", "out", "over", "own", "per", "perhaps", "rather", "s", "same", "seem", "seemed", "seeming", "seems", "several", "she", "should", "since", "so", "some", "somehow", "someone", "something", "sometime", "sometimes", "somewhere", "still", "such", "t", "than", "that", "the", "their", "them", "themselves", "then", "thence", "there", "thereafter", "thereby", "therefor", "therein", "thereupon", "these", "they", "this", "those", "though", "through", "throughout", "thru", "thus", "to", "together", "too", "toward", "towards", "under", "until", "up", "upon", "us", "very", "via", "was", "we", "well", "were", "what", "whatever", "whatsoever", "when", "whence", "whenever", "whensoever", "where", "whereafter", "whereas", "whereat", "whereby", "wherefrom", "wherein", "whereinto", "whereof", "whereon", "whereto", "whereunto", "whereupon", "wherever", "wherewith", "whether", "which", "whichever", "whichsoever", "while", "whilst", "whither", "who", "whoever", "whole", "whom", "whomever", "whomsoever", "whose", "whosoever", "why", "will", "with", "within", "without", "would", "xsubj", "xcal", "xauthor", "xother ", "xnote", "yet", "you", "your", "yours", "yourself", "yourselves"), true));

        /// <summary>
        /// A lower-casing word analyzer with English stop words (can be shared
        /// freely across threads without harm); global per class loader.
        /// </summary>
        public static readonly PatternAnalyzer DEFAULT_ANALYZER = new PatternAnalyzer(LuceneVersion.LUCENE_CURRENT, NON_WORD_PATTERN, true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);

        /// <summary>
        /// A lower-casing word analyzer with <b>extended </b> English stop words
        /// (can be shared freely across threads without harm); global per class
        /// loader. The stop words are borrowed from
        /// http://thomas.loc.gov/home/stopwords.html, see
        /// http://thomas.loc.gov/home/all.about.inquery.html
        /// </summary>
        public static readonly PatternAnalyzer EXTENDED_ANALYZER = new PatternAnalyzer(LuceneVersion.LUCENE_CURRENT, NON_WORD_PATTERN, true, EXTENDED_ENGLISH_STOP_WORDS);

        private readonly Pattern pattern;
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
        ///            <seealso cref="StopFilter#makeStopSet(Version, String[])"/>and/or
        ///            <seealso cref="WordlistLoader"/>as in
        ///            <code>WordlistLoader.getWordSet(new File("samples/fulltext/stopwords.txt")</code>
        ///            or <a href="http://www.unine.ch/info/clef/">other stop words
        ///            lists </a>. </param>
        public PatternAnalyzer(LuceneVersion matchVersion, Pattern pattern, bool toLowerCase, CharArraySet stopWords)
        {
            if (pattern == null)
            {
                throw new System.ArgumentException("pattern must not be null");
            }

            if (eqPattern(NON_WORD_PATTERN, pattern))
            {
                pattern = NON_WORD_PATTERN;
            }
            else if (eqPattern(WHITESPACE_PATTERN, pattern))
            {
                pattern = WHITESPACE_PATTERN;
            }

            if (stopWords != null && stopWords.Size == 0)
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
        public TokenStreamComponents createComponents(string fieldName, TextReader reader, string text)
        {
            // Ideally the Analyzer superclass should have a method with the same signature, 
            // with a default impl that simply delegates to the StringReader flavour. 
            if (reader == null)
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
            TokenStream result = (stopWords != null) ? new StopFilter(matchVersion, tokenizer, stopWords) : tokenizer;
            return new TokenStreamComponents(tokenizer, result);
        }

        /// <summary>
        /// Creates a token stream that tokenizes all the text in the given Reader;
        /// This implementation forwards to <code>tokenStream(String, Reader, String)</code> and is
        /// less efficient than <code>tokenStream(String, Reader, String)</code>.
        /// </summary>
        /// <param name="fieldName">
        ///            the name of the field to tokenize (currently ignored). </param>
        /// <param name="reader">
        ///            the reader delivering the text </param>
        /// <returns> a new token stream </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return createComponents(fieldName, reader, null);
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
                return toLowerCase == p2.toLowerCase && eqPattern(pattern, p2.pattern) && eq(stopWords, p2.stopWords);
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
            h = 31 * h + pattern.pattern().GetHashCode();
            h = 31 * h + pattern.flags();
            h = 31 * h + (toLowerCase ? 1231 : 1237);
            h = 31 * h + (stopWords != null ? stopWords.GetHashCode() : 0);
            return h;
        }

        /// <summary>
        /// equality where o1 and/or o2 can be null </summary>
        private static bool eq(object o1, object o2)
        {
            return (o1 == o2) || (o1 != null ? o1.Equals(o2) : false);
        }

        /// <summary>
        /// assumes p1 and p2 are not null </summary>
        private static bool eqPattern(Pattern p1, Pattern p2)
        {
            return p1 == p2 || (p1.flags() == p2.flags() && p1.pattern().Equals(p2.pattern()));
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
                while ((n = input.Read(buffer)) >= 0)
                {
                    if (len + n > output.Length) // grow capacity
                    {
                        char[] tmp = new char[Math.Max(output.Length << 1, len + n)];
                        Array.Copy(output, 0, tmp, 0, len);
                        Array.Copy(buffer, 0, tmp, len, n);
                        buffer = output; // use larger buffer for future larger bulk reads
                        output = tmp;
                    }
                    else
                    {
                        Array.Copy(buffer, 0, output, len, n);
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
            private readonly Pattern pattern;
            private string str;
            private readonly bool toLowerCase;
            private Matcher matcher;
            private int pos = 0;
            private bool initialized = false;
            private static readonly Locale locale = Locale.Default;
            private readonly ICharTermAttribute termAtt;
            private readonly IOffsetAttribute offsetAtt;

            public PatternTokenizer(TextReader input, Pattern pattern, bool toLowerCase)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
                this.pattern = pattern;
                this.matcher = pattern.matcher("");
                this.toLowerCase = toLowerCase;
            }

            public override bool IncrementToken()
            {
                if (!initialized)
                {
                    throw new System.InvalidOperationException("Consumer did not call reset().");
                }
                if (matcher == null)
                {
                    return false;
                }
                ClearAttributes();
                while (true) // loop takes care of leading and trailing boundary cases
                {
                    int start = pos;
                    int end_Renamed;
                    bool isMatch = matcher.find();
                    if (isMatch)
                    {
                        end_Renamed = matcher.start();
                        pos = matcher.end();
                    }
                    else
                    {
                        end_Renamed = str.Length;
                        matcher = null; // we're finished
                    }

                    if (start != end_Renamed) // non-empty match (header/trailer)
                    {
                        string text = str.Substring(start, end_Renamed - start);
                        if (toLowerCase)
                        {
                            text = text.ToLower(locale);
                        }
                        termAtt.SetEmpty().Append(text);
                        offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(end_Renamed));
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

            public override void Dispose()
            {
                base.Dispose();
                this.initialized = false;
            }

            public override void Reset()
            {
                base.Reset();
                this.str = PatternAnalyzer.ToString(input);
                this.matcher = pattern.matcher(this.str);
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
            private static readonly Locale locale = Locale.Default;
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
                if (str == null)
                {
                    throw new System.InvalidOperationException("Consumer did not call reset().");
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
                    while (i < len && !isTokenChar(s[i], letter))
                    {
                        i++;
                    }

                    if (i < len) // found beginning; now find end of token
                    {
                        start = i;
                        while (i < len && isTokenChar(s[i], letter))
                        {
                            i++;
                        }

                        text = s.Substring(start, i - start);
                        if (toLowerCase)
                        {
                            text = text.ToLower(locale);
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
                } while (text != null && isStopWord(text));

                pos = i;
                if (text == null)
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

            private bool isTokenChar(char c, bool isLetter)
            {
                return isLetter ? char.IsLetter(c) : !char.IsWhiteSpace(c);
            }

            private bool isStopWord(string text)
            {
                return stopWords != null && stopWords.Contains(text);
            }

            public override void Dispose()
            {
                base.Dispose();
                this.str = null;
            }

            public override void Reset()
            {
                base.Reset();
                this.str = PatternAnalyzer.ToString(input);
                this.pos = 0;
            }
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Nested classes:
        ///////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// A StringReader that exposes it's contained string for fast direct access.
        /// Might make sense to generalize this to CharSequence and make it public?
        /// </summary>
        internal sealed class FastStringReader : StringReader
        {

            internal readonly string s;

            internal FastStringReader(string s)
                : base(s)
            {
                this.s = s;
            }

            internal string String
            {
                get
                {
                    return s;
                }
            }
        }

    }
}