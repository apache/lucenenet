// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Pattern
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
    public class TestPatternTokenizer : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestSplitting()
        {
            string qpattern = "\\'([^\\']+)\\'"; // get stuff between "'"
            string[][] tests = new string[][]
            {
            new string[] {"-1", "--", "aaa--bbb--ccc", "aaa bbb ccc"},
            new string[] {"-1", ":", "aaa:bbb:ccc", "aaa bbb ccc"},
            //new string[] {"-1", "\\p{Space}", "aaa   bbb \t\tccc  ", "aaa bbb ccc"}, // LUCENENET: Java-specific Regex syntax. See: http://stackoverflow.com/a/4731164/181087
            new string[] {"-1", "\\s", "aaa   bbb \t\tccc  ", "aaa bbb ccc"}, // LUCENENET: This is the .NET equivalent
            new string[] {"-1", ":", "boo:and:foo", "boo and foo"},
            new string[] {"-1", "o", "boo:and:foo", "b :and:f"},
            new string[] {"0", ":", "boo:and:foo", ": :"},
            new string[] {"0", qpattern, "aaa 'bbb' 'ccc'", "'bbb' 'ccc'"},
            new string[] {"1", qpattern, "aaa 'bbb' 'ccc'", "bbb ccc"}
            };

            foreach (string[] test in tests)
            {
                TokenStream stream = new PatternTokenizer(new StringReader(test[2]), new Regex(test[1], RegexOptions.Compiled), int.Parse(test[0], CultureInfo.InvariantCulture));
                string @out = tsToString(stream);
                // System.out.println( test[2] + " ==> " + out );

                assertEquals("pattern: " + test[1] + " with input: " + test[2], test[3], @out);

                // Make sure it is the same as if we called 'split'
                // test disabled, as we remove empty tokens
                /*if( "-1".equals( test[0] ) ) {
                  String[] split = test[2].split( test[1] );
                  stream = tokenizer.create( new StringReader( test[2] ) );
                  int i=0;
                  for( Token t = stream.next(); null != t; t = stream.next() ) 
                  {
                    assertEquals( "split: "+test[1] + " "+i, split[i++], new String(t.termBuffer(), 0, t.termLength()) );
                  }
                }*/
            }
        }

        [Test]
        public virtual void TestOffsetCorrection()
        {
            const string INPUT = "G&uuml;nther G&uuml;nther is here";

            // create MappingCharFilter
            IList<string> mappingRules = new JCG.List<string>();
            mappingRules.Add("\"&uuml;\" => \"ü\"");
            NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
            builder.Add("&uuml;", "ü");
            NormalizeCharMap normMap = builder.Build();
            CharFilter charStream = new MappingCharFilter(normMap, new StringReader(INPUT));

            // create PatternTokenizer
            TokenStream stream = new PatternTokenizer(charStream, new Regex("[,;/\\s]+", RegexOptions.Compiled), -1);
            AssertTokenStreamContents(stream, new string[] { "Günther", "Günther", "is", "here" }, new int[] { 0, 13, 26, 29 }, new int[] { 12, 25, 28, 33 }, INPUT.Length);

            charStream = new MappingCharFilter(normMap, new StringReader(INPUT));
            stream = new PatternTokenizer(charStream, new Regex("Günther", RegexOptions.Compiled), 0);
            AssertTokenStreamContents(stream, new string[] { "Günther", "Günther" }, new int[] { 0, 13 }, new int[] { 12, 25 }, INPUT.Length);
        }

        /// <summary>
        /// TODO: rewrite tests not to use string comparison.
        /// </summary>
        private static string tsToString(TokenStream @in)
        {
            StringBuilder @out = new StringBuilder();
            ICharTermAttribute termAtt = @in.AddAttribute<ICharTermAttribute>();
            // extra safety to enforce, that the state is not preserved and also
            // assign bogus values
            @in.ClearAttributes();
            termAtt.SetEmpty().Append("bogusTerm");
            @in.Reset();
            while (@in.IncrementToken())
            {
                if (@out.Length > 0)
                {
                    @out.Append(' ');
                }
                @out.Append(termAtt.ToString());
                @in.ClearAttributes();
                termAtt.SetEmpty().Append("bogusTerm");
            }

            @in.Dispose();
            return @out.ToString();
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new PatternTokenizer(reader, new Regex("a", RegexOptions.Compiled), -1);
                return new TokenStreamComponents(tokenizer);
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);

            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new PatternTokenizer(reader, new Regex("a", RegexOptions.Compiled), 0);
                return new TokenStreamComponents(tokenizer);
            });
            CheckRandomData(Random, b, 1000 * RandomMultiplier);
        }
    }
}