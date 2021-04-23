// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Test for <see cref="MorfologikFilterFactory"/>
    /// </summary>
    public class TestMorfologikFilterFactory : BaseTokenStreamTestCase
    {
        private class ForbidResourcesLoader : IResourceLoader
        {
            public Type FindType(string cname)
            {
                throw UnsupportedOperationException.Create();
            }

            public T NewInstance<T>(string cname)
            {
                throw UnsupportedOperationException.Create();
            }

            public Stream OpenResource(string resource)
            {
                throw UnsupportedOperationException.Create();
            }
        }

        [Test]
        public void TestDefaultDictionary()
        {
            StringReader reader = new StringReader("rowery bilety");
            MorfologikFilterFactory factory = new MorfologikFilterFactory(new Dictionary<string, string>());
            factory.Inform(new ForbidResourcesLoader());
            TokenStream stream = new MockTokenizer(reader); //whitespaceMockTokenizer(reader);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream, new String[] { "rower", "bilet" });
        }

        [Test]
        public void TestExplicitDictionary()
        {
            IResourceLoader loader = new ClasspathResourceLoader(typeof(TestMorfologikFilterFactory));

            StringReader reader = new StringReader("inflected1 inflected2");
            IDictionary<String, String> @params = new JCG.Dictionary<string, string>();
            @params[MorfologikFilterFactory.DICTIONARY_ATTRIBUTE] = "custom-dictionary.dict";
            MorfologikFilterFactory factory = new MorfologikFilterFactory(@params);
            factory.Inform(loader);
            TokenStream stream = new MockTokenizer(reader); // whitespaceMockTokenizer(reader);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream, new String[] { "lemma1", "lemma2" });
        }

        [Test]
        public void TestMissingDictionary()
        {
            IResourceLoader loader = new ClasspathResourceLoader(typeof(TestMorfologikFilterFactory));

            IOException expected = NUnit.Framework.Assert.Throws<IOException>(() =>
            {
                IDictionary<String, String> @params = new JCG.Dictionary<String, String>();
                @params[MorfologikFilterFactory.DICTIONARY_ATTRIBUTE] = "missing-dictionary-resource.dict";
                MorfologikFilterFactory factory = new MorfologikFilterFactory(@params);
                factory.Inform(loader);
            });

            assertTrue(expected.Message.Contains("Resource not found"));
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            ArgumentException expected = NUnit.Framework.Assert.Throws<ArgumentException>(() =>
            {
                JCG.Dictionary<String, String> @params = new JCG.Dictionary<String, String>();
                @params["bogusArg"] = "bogusValue";
                new MorfologikFilterFactory(@params);
            });

            assertTrue(expected.Message.Contains("Unknown parameters"));
        }
    }
}
