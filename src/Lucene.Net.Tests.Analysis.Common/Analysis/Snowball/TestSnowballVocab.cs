// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using NUnit.Framework;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Analysis.Snowball
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
    /// Test the snowball filters against the snowball data tests
    /// </summary>
    public class TestSnowballVocab : LuceneTestCase
    {
        /// <summary>
        /// Run all languages against their snowball vocabulary tests.
        /// </summary>
        [Test]
        [Slow]
        public virtual void TestStemmers()
        {
            AssertCorrectOutput("Danish", "danish");
            AssertCorrectOutput("Dutch", "dutch");
            AssertCorrectOutput("English", "english");
            // disabled due to snowball java code generation bug: 
            // see http://article.gmane.org/gmane.comp.search.snowball/1139
            // assertCorrectOutput("Finnish", "finnish");
            AssertCorrectOutput("French", "french");
            AssertCorrectOutput("German", "german");
            AssertCorrectOutput("German2", "german2");
            AssertCorrectOutput("Hungarian", "hungarian");
            AssertCorrectOutput("Italian", "italian");
            AssertCorrectOutput("Kp", "kraaij_pohlmann");
            // disabled due to snowball java code generation bug: 
            // see http://article.gmane.org/gmane.comp.search.snowball/1139
            // assertCorrectOutput("Lovins", "lovins");
            AssertCorrectOutput("Norwegian", "norwegian");
            AssertCorrectOutput("Porter", "porter");
            AssertCorrectOutput("Portuguese", "portuguese");
            AssertCorrectOutput("Romanian", "romanian");
            AssertCorrectOutput("Russian", "russian");
            AssertCorrectOutput("Spanish", "spanish");
            AssertCorrectOutput("Swedish", "swedish");
            AssertCorrectOutput("Turkish", "turkish");
        }

        /// <summary>
        /// For the supplied language, run the stemmer against all strings in voc.txt
        /// The output should be the same as the string in output.txt
        /// </summary>
        private void AssertCorrectOutput(string snowballLanguage, string dataDirectory)
        {
            if (Verbose)
            {
                Console.WriteLine("checking snowball language: " + snowballLanguage);
            }

            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new KeywordTokenizer(reader);
                return new TokenStreamComponents(t, new SnowballFilter(t, snowballLanguage));
            });

            VocabularyAssert.AssertVocabulary(a, GetDataFile("TestSnowballVocabData.zip"), dataDirectory + "/voc.txt", dataDirectory + "/output.txt");
        }
    }
}