using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
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
    /// Loader for text files that represent a list of stopwords.
    /// </summary>
    /// <seealso cref= IOUtils to obtain <seealso cref="Reader"/> instances
    /// @lucene.internal </seealso>
    public class WordlistLoader
    {

        private const int INITIAL_CAPACITY = 16;

        /// <summary>
        /// no instance </summary>
        private WordlistLoader()
        {
        }

        // LUCENENET TODO: Add .NET overloads that accept a file name? Or at least a FileInfo object as was done in 3.0.3?

        /// <summary>
        /// Reads lines from a TextReader and adds every line as an entry to a CharArraySet (omitting
        /// leading and trailing whitespace). Every line of the TextReader should contain only
        /// one word. The words need to be in lowercase if you make use of an
        /// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
        /// </summary>
        /// <param name="reader"> TextReader containing the wordlist </param>
        /// <param name="result"> the <seealso cref="CharArraySet"/> to fill with the readers words </param>
        /// <returns> the given <seealso cref="CharArraySet"/> with the reader's words </returns>
        public static CharArraySet GetWordSet(TextReader reader, CharArraySet result)
        {
            try
            {
                string word = null;
                while ((word = reader.ReadLine()) != null)
                {
                    result.Add(word.Trim());
                }
                
            }
            finally
            {
                IOUtils.Close(reader);
            }
            return result;
        }

        /// <summary>
        /// Reads lines from a TextReader and adds every line as an entry to a CharArraySet (omitting
        /// leading and trailing whitespace). Every line of the TextReader should contain only
        /// one word. The words need to be in lowercase if you make use of an
        /// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
        /// </summary>
        /// <param name="reader"> TextReader containing the wordlist </param>
        /// <param name="matchVersion"> the <seealso cref="LuceneVersion"/> </param>
        /// <returns> A <seealso cref="CharArraySet"/> with the reader's words </returns>
        public static CharArraySet GetWordSet(TextReader reader, LuceneVersion matchVersion)
        {
            return GetWordSet(reader, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
        }

        /// <summary>
        /// Reads lines from a TextReader and adds every non-comment line as an entry to a CharArraySet (omitting
        /// leading and trailing whitespace). Every line of the TextReader should contain only
        /// one word. The words need to be in lowercase if you make use of an
        /// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
        /// </summary>
        /// <param name="reader"> TextReader containing the wordlist </param>
        /// <param name="comment"> The string representing a comment. </param>
        /// <param name="matchVersion"> the <seealso cref="LuceneVersion"/> </param>
        /// <returns> A CharArraySet with the reader's words </returns>
        public static CharArraySet GetWordSet(TextReader reader, string comment, LuceneVersion matchVersion)
        {
            return GetWordSet(reader, comment, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
        }

        /// <summary>
        /// Reads lines from a TextReader and adds every non-comment line as an entry to a CharArraySet (omitting
        /// leading and trailing whitespace). Every line of the TextReader should contain only
        /// one word. The words need to be in lowercase if you make use of an
        /// Analyzer which uses LowerCaseFilter (like StandardAnalyzer).
        /// </summary>
        /// <param name="reader"> TextReader containing the wordlist </param>
        /// <param name="comment"> The string representing a comment. </param>
        /// <param name="result"> the <seealso cref="CharArraySet"/> to fill with the readers words </param>
        /// <returns> the given <seealso cref="CharArraySet"/> with the reader's words </returns>
        public static CharArraySet GetWordSet(TextReader reader, string comment, CharArraySet result)
        {
            try
            {
                string word = null;
                while ((word = reader.ReadLine()) != null)
                {
                    if (word.StartsWith(comment, StringComparison.Ordinal) == false)
                    {
                        result.Add(word.Trim());
                    }
                }
            }
            finally
            {
                IOUtils.Close(reader);
            }
            return result;
        }


        /// <summary>
        /// Reads stopwords from a stopword list in Snowball format.
        /// <para>
        /// The snowball format is the following:
        /// <ul>
        /// <li>Lines may contain multiple words separated by whitespace.
        /// <li>The comment character is the vertical line (&#124;).
        /// <li>Lines may contain trailing comments.
        /// </ul>
        /// </para>
        /// </summary>
        /// <param name="reader"> TextReader containing a Snowball stopword list </param>
        /// <param name="result"> the <seealso cref="CharArraySet"/> to fill with the readers words </param>
        /// <returns> the given <seealso cref="CharArraySet"/> with the reader's words </returns>
        public static CharArraySet GetSnowballWordSet(TextReader reader, CharArraySet result)
        {
            try
            { 
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    int comment = line.IndexOf('|');
                    if (comment >= 0)
                    {
                        line = line.Substring(0, comment);
                    }
                    string[] words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(w => w.Trim()).ToArray();
                    foreach (var word in words)
                    {
                        if (word.Length > 0)
                        {
                            result.Add(word);
                        }
                    }
                }
            }
            finally
            {
                IOUtils.Close(reader);
            }
            return result;
        }

        /// <summary>
        /// Reads stopwords from a stopword list in Snowball format.
        /// <para>
        /// The snowball format is the following:
        /// <ul>
        /// <li>Lines may contain multiple words separated by whitespace.
        /// <li>The comment character is the vertical line (&#124;).
        /// <li>Lines may contain trailing comments.
        /// </ul>
        /// </para>
        /// </summary>
        /// <param name="reader"> TextReader containing a Snowball stopword list </param>
        /// <param name="matchVersion"> the Lucene <seealso cref="Version"/> </param>
        /// <returns> A <seealso cref="CharArraySet"/> with the reader's words </returns>
        public static CharArraySet GetSnowballWordSet(TextReader reader, LuceneVersion matchVersion)
        {
            return GetSnowballWordSet(reader, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
        }


        /// <summary>
        /// Reads a stem dictionary. Each line contains:
        /// <pre>word<b>\t</b>stem</pre>
        /// (i.e. two tab separated words)
        /// </summary>
        /// <returns> stem dictionary that overrules the stemming algorithm </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public static CharArrayMap<string> GetStemDict(TextReader reader, CharArrayMap<string> result)
        {
            try
            { 
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] wordstem = line.Split(new char[] { '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    result.Put(wordstem[0], wordstem[1]);
                }
            }
            finally
            {
                IOUtils.Close(reader);
            }
            return result;
        }

        /// <summary>
        /// Accesses a resource by name and returns the (non comment) lines containing
        /// data using the given character encoding.
        /// 
        /// <para>
        /// A comment line is any line that starts with the character "#"
        /// </para>
        /// </summary>
        /// <returns> a list of non-blank non-comment lines with whitespace trimmed </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public static IEnumerable<string> GetLines(Stream stream, Encoding encoding)
        {
            List<string> lines = new List<string>();

            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                string word;
                while ((word = reader.ReadLine()) != null)
                {
                    // skip initial bom marker
                    if (lines.Count == 0 && word.Length > 0 && word[0] == '\uFEFF')
                    {
                        word = word.Substring(1);
                    }
                    // skip comments
                    if (word.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    word = word.Trim();
                    // skip blank lines
                    if (word.Length == 0)
                    {
                        continue;
                    }
                    lines.Add(word);
                }
            }
            return lines;
        }
    }
}