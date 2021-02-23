// Lucene version compatibility level 4.8.1
#if FEATURE_COLLATION
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Icu;
using Icu.Collation;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Collation
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

    [TestFixture]
    public class TestCollationKeyFilterFactory : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Turkish has some funny casing.
        /// This test shows how you can solve this kind of thing easily with collation.
        /// Instead of using LowerCaseFilter, use a turkish collator with primary strength.
        /// Then things will sort and match correctly.
        /// </summary>
        public virtual void TestBasicUsage()
        {
            var turkishUpperCase = "I WİLL USE TURKİSH CASING";
            var turkishLowerCase = "ı will use turkish casıng";
            var factory = this.TokenFilterFactory("CollationKey", "language", "tr", "strength", "primary");
            var tsUpper = factory.Create(new MockTokenizer(new StringReader(turkishUpperCase), MockTokenizer.KEYWORD, false));
            var tsLower = factory.Create(new MockTokenizer(new StringReader(turkishLowerCase), MockTokenizer.KEYWORD, false));
            AssertCollatesToSame(tsUpper, tsLower);
        }

        /// <summary>
        /// Test usage of the decomposition option for unicode normalization.
        /// </summary>
        [Test]
        public virtual void TestNormalization()
        {
            var turkishUpperCase = "I W\u0049\u0307LL USE TURKİSH CASING";
            var turkishLowerCase = "ı will use turkish casıng";
            var factory = this.TokenFilterFactory("CollationKey", "language", "tr", "strength", "primary", "decomposition", "canonical");
            var tsUpper = factory.Create(new MockTokenizer(new StringReader(turkishUpperCase), MockTokenizer.KEYWORD, false));
            var tsLower = factory.Create(new MockTokenizer(new StringReader(turkishLowerCase), MockTokenizer.KEYWORD, false));
            AssertCollatesToSame(tsUpper, tsLower);
        }

        /// <summary>
        /// Test usage of the K decomposition option for unicode normalization.
        /// This works even with identical strength.
        /// </summary>
        [Test]
        public virtual void TestFullDecomposition()
        {
            var fullWidth = "Ｔｅｓｔｉｎｇ";
            var halfWidth = "Testing";
            var factory = this.TokenFilterFactory("CollationKey", "language", "zh", "strength", "identical", "decomposition", "full");
            var tsFull = factory.Create(new MockTokenizer(new StringReader(fullWidth), MockTokenizer.KEYWORD, false));
            var tsHalf = factory.Create(new MockTokenizer(new StringReader(halfWidth), MockTokenizer.KEYWORD, false));
            AssertCollatesToSame(tsFull, tsHalf);
        }

        /// <summary>
        /// Test secondary strength, for english case is not significant.
        /// </summary>
        [Test]
        public virtual void TestSecondaryStrength()
        {
            var upperCase = "TESTING";
            var lowerCase = "testing";
            var factory = this.TokenFilterFactory("CollationKey", "language", "en", "strength", "secondary", "decomposition", "no");
            var tsUpper = factory.Create(new MockTokenizer(new StringReader(upperCase), MockTokenizer.KEYWORD, false));
            var tsLower = factory.Create(new MockTokenizer(new StringReader(lowerCase), MockTokenizer.KEYWORD, false));
            AssertCollatesToSame(tsUpper, tsLower);
        }

        /// <summary>
        /// For german, you might want oe to sort and match with o umlaut.
        /// This is not the default, but you can make a customized ruleset to do this.
        ///
        /// The default is DIN 5007-1, this shows how to tailor a collator to get DIN 5007-2 behavior.
        ///  http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4423383
        /// </summary>
        [Test]
        public virtual void TestCustomRules()
        {
            // It is possible not to have the collator rules for a specific
            // country, fallback to the language if that is the case.
            var possiblelocales = new[] { new Locale("de-DE"), new Locale("de") };
            var allRules = RuleBasedCollator.GetAvailableCollationLocales();
            var localeToUse = possiblelocales.FirstOrDefault(locl => allRules.Contains(locl.Id));

            Assert.True(localeToUse != default, "Should have found a matching collation locale given the two locales to use.");

            const string DIN5007_2_tailorings = "& ae , a\u0308 & AE , A\u0308" + "& oe , o\u0308 & OE , O\u0308" + "& ue , u\u0308 & UE , u\u0308";
            var collationRules = Collator.GetCollationRules(localeToUse.Id);

            Assert.IsNotNull(collationRules, $"Rules should have been fetched for {localeToUse.Id}");

            string tailoredRules = collationRules + DIN5007_2_tailorings;

            RuleBasedCollator tailoredCollator = new RuleBasedCollator(tailoredRules);

            // at this point, you would save these tailoredRules to a file, 
            // and use the custom parameter.
            string germanUmlaut = "Töne";
            string germanOE = "Toene";
            IDictionary<string, string> args = new Dictionary<string, string>();
            args["custom"] = "rules.txt";
            args["strength"] = "primary";
#pragma warning disable 612, 618
            CollationKeyFilterFactory factory = new CollationKeyFilterFactory(args);
#pragma warning restore 612, 618
            factory.Inform(new StringMockResourceLoader(tailoredRules));
            TokenStream tsUmlaut = factory.Create(new MockTokenizer(new StringReader(germanUmlaut), MockTokenizer.KEYWORD, false));
            TokenStream tsOE = factory.Create(new MockTokenizer(new StringReader(germanOE), MockTokenizer.KEYWORD, false));

            AssertCollatesToSame(tsUmlaut, tsOE);
        }
        
        private static void AssertCollatesToSame(TokenStream stream1, TokenStream stream2)
        {
            stream1.Reset();
            stream2.Reset();
            ICharTermAttribute term1 = stream1.AddAttribute<ICharTermAttribute>();
            ICharTermAttribute term2 = stream2.AddAttribute<ICharTermAttribute>();
            assertTrue(stream1.IncrementToken());
            assertTrue(stream2.IncrementToken());
            assertEquals(term1.ToString(), term2.ToString());
            assertFalse(stream1.IncrementToken());
            assertFalse(stream2.IncrementToken());
            stream1.End();
            stream2.End();
            stream1.Dispose();
            stream2.Dispose();
        }
    }
}
#endif