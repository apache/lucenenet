using Egothor.Stemmer;
using J2N.IO;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Stempel
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
    /// Stemmer class is a convenient facade for other stemmer-related classes. The
    /// core stemming algorithm and its implementation is taken verbatim from the
    /// Egothor project ( <a href="http://www.egothor.org">www.egothor.org </a>).
    /// <para>
    /// Even though the stemmer tables supplied in the distribution package are built
    /// for Polish language, there is nothing language-specific here.
    /// </para>
    /// </summary>
    public class StempelStemmer
    {
        private readonly Trie stemmer = null; // LUCENENET: marked readonly
        private readonly StringBuilder buffer = new StringBuilder(); // LUCENENET: marked readonly

        /// <summary>
        /// Create a Stemmer using selected stemmer table
        /// </summary>
        /// <param name="stemmerTable">stemmer table.</param>
        public StempelStemmer(Stream stemmerTable)
            : this(Load(stemmerTable))
        {
        }

        /// <summary>
        /// Create a Stemmer using pre-loaded stemmer table
        /// </summary>
        /// <param name="stemmer">pre-loaded stemmer table</param>
        public StempelStemmer(Trie stemmer)
        {
            this.stemmer = stemmer;
        }

        /// <summary>
        /// Load a stemmer table from an inputstream.
        /// </summary>
        public static Trie Load(Stream stemmerTable)
        {
            DataInputStream @in = null;
            try
            {
                @in = new DataInputStream(stemmerTable);
                string method = @in.ReadUTF().ToUpperInvariant();
                if (method.IndexOf('M') < 0)
                {
                    return new Trie(@in);
                }
                else
                {
                    return new MultiTrie2(@in);
                }
            }
            finally
            {
                @in.Dispose();
            }
        }

        /// <summary>
        /// Stem a word.
        /// </summary>
        /// <param name="word">input word to be stemmed.</param>
        /// <returns>stemmed word, or null if the stem could not be generated.</returns>
        public StringBuilder Stem(string word)
        {
            string cmd = stemmer.GetLastOnPath(word);

            if (cmd is null)
                return null;

            buffer.Length = 0;
            buffer.Append(word);

            Diff.Apply(buffer, cmd);

            if (buffer.Length > 0)
                return buffer;
            else
                return null;
        }
    }
}
