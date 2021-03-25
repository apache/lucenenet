// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Wikipedia
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
    /// Basic Tests for <seealso cref="WikipediaTokenizer"/>
    /// 
    /// </summary>
    public class WikipediaTokenizerTest : BaseTokenStreamTestCase
    {
        protected internal const string LINK_PHRASES = "click [[link here again]] click [http://lucene.apache.org here again] [[Category:a b c d]]";

        [Test]
        public virtual void TestSimple()
        {
            string text = "This is a [[Category:foo]]";
            WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(text));
            AssertTokenStreamContents(tf, new string[] { "This", "is", "a", "foo" }, new int[] { 0, 5, 8, 21 }, new int[] { 4, 7, 9, 24 }, new string[] { "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.CATEGORY }, new int[] { 1, 1, 1, 1 }, text.Length);
        }

        [Test]
        public virtual void TestHandwritten()
        {
            // make sure all tokens are in only one type
            string test = "[[link]] This is a [[Category:foo]] Category  This is a linked [[:Category:bar none withstanding]] " + "Category This is (parens) This is a [[link]]  This is an external URL [http://lucene.apache.org] " + "Here is ''italics'' and ''more italics'', '''bold''' and '''''five quotes''''' " + " This is a [[link|display info]]  This is a period.  Here is $3.25 and here is 3.50.  Here's Johnny.  " + "==heading== ===sub head=== followed by some text  [[Category:blah| ]] " + "''[[Category:ital_cat]]''  here is some that is ''italics [[Category:foo]] but is never closed." + "'''same [[Category:foo]] goes for this '''''and2 [[Category:foo]] and this" + " [http://foo.boo.com/test/test/ Test Test] [http://foo.boo.com/test/test/test.html Test Test]" + " [http://foo.boo.com/test/test/test.html?g=b&c=d Test Test] <ref>Citation</ref> <sup>martian</sup> <span class=\"glue\">code</span>";

            WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(test));
            AssertTokenStreamContents(tf, new string[] { "link", "This", "is", "a", "foo", "Category", "This", "is", "a", "linked", "bar", "none", "withstanding", "Category", "This", "is", "parens", "This", "is", "a", "link", "This", "is", "an", "external", "URL", "http://lucene.apache.org", "Here", "is", "italics", "and", "more", "italics", "bold", "and", "five", "quotes", "This", "is", "a", "link", "display", "info", "This", "is", "a", "period", "Here", "is", "3.25", "and", "here", "is", "3.50", "Here's", "Johnny", "heading", "sub", "head", "followed", "by", "some", "text", "blah", "ital", "cat", "here", "is", "some", "that", "is", "italics", "foo", "but", "is", "never", "closed", "same", "foo", "goes", "for", "this", "and2", "foo", "and", "this", "http://foo.boo.com/test/test/", "Test", "Test", "http://foo.boo.com/test/test/test.html", "Test", "Test", "http://foo.boo.com/test/test/test.html?g=b&c=d", "Test", "Test", "Citation", "martian", "code" }, new string[] { WikipediaTokenizer.INTERNAL_LINK, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.CATEGORY, WikipediaTokenizer.CATEGORY, WikipediaTokenizer.CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.INTERNAL_LINK, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.EXTERNAL_LINK_URL, "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.ITALICS, "<ALPHANUM>", WikipediaTokenizer.ITALICS, WikipediaTokenizer.ITALICS, WikipediaTokenizer.BOLD, "<ALPHANUM>", WikipediaTokenizer.BOLD_ITALICS, WikipediaTokenizer.BOLD_ITALICS, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.INTERNAL_LINK, WikipediaTokenizer.INTERNAL_LINK, WikipediaTokenizer.INTERNAL_LINK, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<APOSTROPHE>", "<ALPHANUM>", WikipediaTokenizer.HEADING, WikipediaTokenizer.SUB_HEADING, WikipediaTokenizer.SUB_HEADING, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.CATEGORY, WikipediaTokenizer.CATEGORY, WikipediaTokenizer.CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.ITALICS, WikipediaTokenizer.CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.BOLD, WikipediaTokenizer.CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.BOLD_ITALICS, WikipediaTokenizer.CATEGORY, "<ALPHANUM>", "<ALPHANUM>", WikipediaTokenizer.EXTERNAL_LINK_URL, WikipediaTokenizer.EXTERNAL_LINK, WikipediaTokenizer.EXTERNAL_LINK, WikipediaTokenizer.EXTERNAL_LINK_URL, WikipediaTokenizer.EXTERNAL_LINK, WikipediaTokenizer.EXTERNAL_LINK, WikipediaTokenizer.EXTERNAL_LINK_URL, WikipediaTokenizer.EXTERNAL_LINK, WikipediaTokenizer.EXTERNAL_LINK, WikipediaTokenizer.CITATION, "<ALPHANUM>", "<ALPHANUM>" });
        }

        [Test]
        public virtual void TestLinkPhrases()
        {
            WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(LINK_PHRASES));
            CheckLinkPhrases(tf);
        }

        private void CheckLinkPhrases(WikipediaTokenizer tf)
        {
            AssertTokenStreamContents(tf, new string[] { "click", "link", "here", "again", "click", "http://lucene.apache.org", "here", "again", "a", "b", "c", "d" }, new int[] { 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1 });
        }

        [Test]
        public virtual void TestLinks()
        {
            string test = "[http://lucene.apache.org/java/docs/index.html#news here] [http://lucene.apache.org/java/docs/index.html?b=c here] [https://lucene.apache.org/java/docs/index.html?b=c here]";
            WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(test));
            AssertTokenStreamContents(tf, new string[] { "http://lucene.apache.org/java/docs/index.html#news", "here", "http://lucene.apache.org/java/docs/index.html?b=c", "here", "https://lucene.apache.org/java/docs/index.html?b=c", "here" }, new string[] { WikipediaTokenizer.EXTERNAL_LINK_URL, WikipediaTokenizer.EXTERNAL_LINK, WikipediaTokenizer.EXTERNAL_LINK_URL, WikipediaTokenizer.EXTERNAL_LINK, WikipediaTokenizer.EXTERNAL_LINK_URL, WikipediaTokenizer.EXTERNAL_LINK });
        }

        [Test]
        public virtual void TestLucene1133()
        {
            ISet<string> untoks = new JCG.HashSet<string>();
            untoks.Add(WikipediaTokenizer.CATEGORY);
            untoks.Add(WikipediaTokenizer.ITALICS);
            //should be exactly the same, regardless of untoks
            WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(LINK_PHRASES), WikipediaTokenizer.TOKENS_ONLY, untoks);
            CheckLinkPhrases(tf);
            string test = "[[Category:a b c d]] [[Category:e f g]] [[link here]] [[link there]] ''italics here'' something ''more italics'' [[Category:h   i   j]]";
            tf = new WikipediaTokenizer(new StringReader(test), WikipediaTokenizer.UNTOKENIZED_ONLY, untoks);
            AssertTokenStreamContents(tf, new string[] { "a b c d", "e f g", "link", "here", "link", "there", "italics here", "something", "more italics", "h   i   j" }, new int[] { 11, 32, 42, 47, 56, 61, 71, 86, 98, 124 }, new int[] { 18, 37, 46, 51, 60, 66, 83, 95, 110, 133 }, new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
        }

        [Test]
        public virtual void TestBoth()
        {
            ISet<string> untoks = new JCG.HashSet<string>();
            untoks.Add(WikipediaTokenizer.CATEGORY);
            untoks.Add(WikipediaTokenizer.ITALICS);
            string test = "[[Category:a b c d]] [[Category:e f g]] [[link here]] [[link there]] ''italics here'' something ''more italics'' [[Category:h   i   j]]";
            //should output all the indivual tokens plus the untokenized tokens as well.  Untokenized tokens
            WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(test), WikipediaTokenizer.BOTH, untoks);
            AssertTokenStreamContents(tf, new string[] { "a b c d", "a", "b", "c", "d", "e f g", "e", "f", "g", "link", "here", "link", "there", "italics here", "italics", "here", "something", "more italics", "more", "italics", "h   i   j", "h", "i", "j" }, new int[] { 11, 11, 13, 15, 17, 32, 32, 34, 36, 42, 47, 56, 61, 71, 71, 79, 86, 98, 98, 103, 124, 124, 128, 132 }, new int[] { 18, 12, 14, 16, 18, 37, 33, 35, 37, 46, 51, 60, 66, 83, 78, 83, 95, 110, 102, 110, 133, 125, 129, 133 }, new int[] { 1, 0, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0, 1, 1 });

            // now check the flags, TODO: add way to check flags from BaseTokenStreamTestCase?
            tf = new WikipediaTokenizer(new StringReader(test), WikipediaTokenizer.BOTH, untoks);
            int[] expectedFlags = new int[] { WikipediaTokenizer.UNTOKENIZED_TOKEN_FLAG, 0, 0, 0, 0, WikipediaTokenizer.UNTOKENIZED_TOKEN_FLAG, 0, 0, 0, 0, 0, 0, 0, WikipediaTokenizer.UNTOKENIZED_TOKEN_FLAG, 0, 0, 0, WikipediaTokenizer.UNTOKENIZED_TOKEN_FLAG, 0, 0, WikipediaTokenizer.UNTOKENIZED_TOKEN_FLAG, 0, 0, 0 };
            IFlagsAttribute flagsAtt = tf.AddAttribute<IFlagsAttribute>();
            tf.Reset();
            for (int i = 0; i < expectedFlags.Length; i++)
            {
                assertTrue(tf.IncrementToken());
                assertEquals("flags " + i, expectedFlags[i], flagsAtt.Flags);
            }
            assertFalse(tf.IncrementToken());
            tf.Dispose();
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new WikipediaTokenizer(reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        /// <summary>
        /// blast some random large strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomHugeStrings()
        {
            Random random = Random;
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new WikipediaTokenizer(reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            CheckRandomData(random, a, 100 * RandomMultiplier, 8192);
        }
    }
}