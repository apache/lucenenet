// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

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

    public class TestTypeTokenFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestTypeFilter()
        {
            StringReader reader = new StringReader("121 is palindrome, while 123 is not");
            ISet<string> stopTypes = AsSet("<NUM>");
            TokenStream stream =
#pragma warning disable 612, 618
                new TypeTokenFilter(
#pragma warning restore 612, 618
                    TEST_VERSION_CURRENT, true, new StandardTokenizer(TEST_VERSION_CURRENT, reader), stopTypes);
            AssertTokenStreamContents(stream, new string[] { "is", "palindrome", "while", "is", "not" });
        }

        /// <summary>
        /// Test Position increments applied by TypeTokenFilter with and without enabling this option.
        /// </summary>
        [Test]
        public virtual void TestStopPositons()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 10; i < 20; i++)
            {
                if (i % 3 != 0)
                {
                    sb.Append(i).Append(' ');
                }
                else
                {
                    string w = English.Int32ToEnglish(i).Trim();
                    sb.Append(w).Append(' ');
                }
            }
            log(sb.ToString());
            string[] stopTypes = new string[] { "<NUM>" };
            ISet<string> stopSet = AsSet(stopTypes);

            // with increments
            StringReader reader = new StringReader(sb.ToString());
            TypeTokenFilter typeTokenFilter = new TypeTokenFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, reader), stopSet);
            TestPositons(typeTokenFilter);

            // without increments
            reader = new StringReader(sb.ToString());
            typeTokenFilter =
#pragma warning disable 612, 618
                new TypeTokenFilter(LuceneVersion.LUCENE_43, 
#pragma warning restore 612, 618
                    false, new StandardTokenizer(TEST_VERSION_CURRENT, reader), stopSet);
            TestPositons(typeTokenFilter);

        }

        private void TestPositons(TypeTokenFilter stpf)
        {
            ITypeAttribute typeAtt = stpf.GetAttribute<ITypeAttribute>();
            ICharTermAttribute termAttribute = stpf.GetAttribute<ICharTermAttribute>();
            IPositionIncrementAttribute posIncrAtt = stpf.GetAttribute<IPositionIncrementAttribute>();
            stpf.Reset();
            bool enablePositionIncrements = stpf.EnablePositionIncrements;
            while (stpf.IncrementToken())
            {
                log("Token: " + termAttribute.ToString() + ": " + typeAtt.Type + " - " + posIncrAtt.PositionIncrement);
                assertEquals("if position increment is enabled the positionIncrementAttribute value should be 3, otherwise 1", posIncrAtt.PositionIncrement, enablePositionIncrements ? 3 : 1);
            }
            stpf.End();
            stpf.Dispose();
        }

        [Test]
        public virtual void TestTypeFilterWhitelist()
        {
            StringReader reader = new StringReader("121 is palindrome, while 123 is not");
            ISet<string> stopTypes = new JCG.HashSet<string> { "<NUM>" };
            TokenStream stream = new TypeTokenFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, reader), stopTypes, true);
            AssertTokenStreamContents(stream, new string[] { "121", "123" });
        }

        // print debug info depending on VERBOSE
        private static void log(string s)
        {
            if (Verbose)
            {
                Console.WriteLine(s);
            }
        }
    }
}