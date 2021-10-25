using J2N;
using Lucene.Net.Analysis.Ja.Util;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Ja.Tools
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

    public class TestBuildDictionary : LuceneTestCase
    {
        /// <summary>
        /// Since there were no tests provided for the BuildDictionary tool, this is a smoke test
        /// just to ensure the I/O has basic functionality.
        /// <para/>
        /// The data was sourced from:
        /// http://mentaldetritus.blogspot.com/2013/03/compiling-custom-dictionary-for.html
        /// https://sourceforge.net/projects/mecab/files/mecab-ipadic/2.7.0-20070801/
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public void TestBuildDictionaryEndToEnd()
        {
            var inputDir = CreateTempDir("build-dictionary-input");
            var outputDir = CreateTempDir("build-dictionary-output");
            using (var zipFileStream = this.GetType().FindAndGetManifestResourceStream("custom-dictionary-input.zip"))
            {
                TestUtil.Unzip(zipFileStream, inputDir);
            }

            var args = new JCG.List<string>();
            args.Add("ipadic"); // dictionary format
            args.Add(inputDir.FullName); // input dir
            args.Add(outputDir.FullName); // output dir
            args.Add("euc-jp"); // encoding
            args.Add("true"); // normalize?

            DictionaryBuilder.Main(args.ToArray());
        }
    }
}
