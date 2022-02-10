// Lucene version compatibility level 4.10.4
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Hunspell
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
    /// base class for hunspell stemmer tests </summary>
    public abstract class StemmerTestBase : LuceneTestCase
    {
        private static Stemmer stemmer;

        internal static void Init(string affix, string dictionary)
        {
            Init(false, affix, dictionary);
        }

        internal static void Init(bool ignoreCase, string affix, params string[] dictionaries)
        {
            if (dictionaries.Length == 0)
            {
                throw new ArgumentException("there must be at least one dictionary");
            }

            System.IO.Stream affixStream = typeof(StemmerTestBase).getResourceAsStream(affix);
            if (affixStream is null)
            {
                throw new FileNotFoundException("file not found: " + affix);
            }

            System.IO.Stream[] dictStreams = new System.IO.Stream[dictionaries.Length];
            for (int i = 0; i < dictionaries.Length; i++)
            {
                dictStreams[i] = typeof(StemmerTestBase).getResourceAsStream(dictionaries[i]);
                if (dictStreams[i] is null)
                {
                    throw new FileNotFoundException("file not found: " + dictStreams[i]);
                }
            }

            try
            {
                Dictionary dictionary = new Dictionary(affixStream, dictStreams, ignoreCase);
                stemmer = new Stemmer(dictionary);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(affixStream);
                IOUtils.DisposeWhileHandlingException(null, dictStreams);
            }
        }

        internal static void AssertStemsTo(string s, params string[] expected)
        {
            assertNotNull(stemmer);
            Array.Sort(expected);

            IList<CharsRef> stems = stemmer.Stem(s);
            string[] actual = new string[stems.Count];
            for (int i = 0; i < actual.Length; i++)
            {
                actual[i] = stems[i].ToString();
            }
            Array.Sort(actual);

            // LUCENENET: Use delegate to build the string so we don't have the expensive operation unless there is a failure
            assertArrayEquals(() => "expected=" + Arrays.ToString(expected) + ",actual=" + Arrays.ToString(actual), expected, actual);
        }
    }
}