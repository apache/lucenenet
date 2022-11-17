using ICU4N.Globalization;
using ICU4N.Text;
using J2N;
using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

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

    [Obsolete]
    public class TestICUCollationKeyFilterFactory : BaseTokenStreamTestCase
    {
        /// <summary>
        /// Turkish has some funny casing.
        /// This test shows how you can solve this kind of thing easily with collation.
        /// Instead of using LowerCaseFilter, use a turkish collator with primary strength.
        /// Then things will sort and match correctly.
        /// </summary>
        [Test]
        public void TestBasicUsage()
        {
            String turkishUpperCase = "I WİLL USE TURKİSH CASING";
            String turkishLowerCase = "ı will use turkish casıng";
            TokenFilterFactory factory = tokenFilterFactory("ICUCollationKey",
                "locale", "tr",
                "strength", "primary");
            TokenStream tsUpper = factory.Create(
                new KeywordTokenizer(new StringReader(turkishUpperCase)));
            TokenStream tsLower = factory.Create(
                new KeywordTokenizer(new StringReader(turkishLowerCase)));
            assertCollatesToSame(tsUpper, tsLower);
        }

        /*
         * Test usage of the decomposition option for unicode normalization.
         */
        [Test]
        public void TestNormalization()
        {
            String turkishUpperCase = "I W\u0049\u0307LL USE TURKİSH CASING";
            String turkishLowerCase = "ı will use turkish casıng";
            TokenFilterFactory factory = tokenFilterFactory("ICUCollationKey",
            "locale", "tr",
            "strength", "primary",
            "decomposition", "canonical");
            TokenStream tsUpper = factory.Create(
            new KeywordTokenizer(new StringReader(turkishUpperCase)));
            TokenStream tsLower = factory.Create(
                new KeywordTokenizer(new StringReader(turkishLowerCase)));
            assertCollatesToSame(tsUpper, tsLower);
        }

        /*
         * Test secondary strength, for english case is not significant.
         */
        [Test]
        public void TestSecondaryStrength()
        {
            String upperCase = "TESTING";
            String lowerCase = "testing";
            TokenFilterFactory factory = tokenFilterFactory("ICUCollationKey",
                "locale", "en",
                "strength", "secondary",
                "decomposition", "no");
            TokenStream tsUpper = factory.Create(
                new KeywordTokenizer(new StringReader(upperCase)));
            TokenStream tsLower = factory.Create(
                new KeywordTokenizer(new StringReader(lowerCase)));
            assertCollatesToSame(tsUpper, tsLower);
        }

        /*
         * Setting alternate=shifted to shift whitespace, punctuation and symbols
         * to quaternary level 
         */
        [Test]
        public void TestIgnorePunctuation()
        {
            String withPunctuation = "foo-bar";
            String withoutPunctuation = "foo bar";
            TokenFilterFactory factory = tokenFilterFactory("ICUCollationKey",
                "locale", "en",
                "strength", "primary",
                "alternate", "shifted");
            TokenStream tsPunctuation = factory.Create(
                new KeywordTokenizer(new StringReader(withPunctuation)));
            TokenStream tsWithoutPunctuation = factory.Create(
                new KeywordTokenizer(new StringReader(withoutPunctuation)));
            assertCollatesToSame(tsPunctuation, tsWithoutPunctuation);
        }

        /*
         * Setting alternate=shifted and variableTop to shift whitespace, but not 
         * punctuation or symbols, to quaternary level 
         */
        [Test]
        public void TestIgnoreWhitespace()
        {
            String withSpace = "foo bar";
            String withoutSpace = "foobar";
            String withPunctuation = "foo-bar";
            TokenFilterFactory factory = tokenFilterFactory("ICUCollationKey",
                "locale", "en",
                "strength", "primary",
                "alternate", "shifted",
                "variableTop", " ");
            TokenStream tsWithSpace = factory.Create(
                new KeywordTokenizer(new StringReader(withSpace)));
            TokenStream tsWithoutSpace = factory.Create(
                new KeywordTokenizer(new StringReader(withoutSpace)));
            assertCollatesToSame(tsWithSpace, tsWithoutSpace);
            // now assert that punctuation still matters: foo-bar < foo bar
            tsWithSpace = factory.Create(
                    new KeywordTokenizer(new StringReader(withSpace)));
            TokenStream tsWithPunctuation = factory.Create(
                new KeywordTokenizer(new StringReader(withPunctuation)));
            assertCollation(tsWithPunctuation, tsWithSpace, -1);
        }

        /*
         * Setting numeric to encode digits with numeric value, so that
         * foobar-9 sorts before foobar-10
         */
        [Test]
        public void TestNumerics()
        {
            String nine = "foobar-9";
            String ten = "foobar-10";
            TokenFilterFactory factory = tokenFilterFactory("ICUCollationKey",
                "locale", "en",
                "numeric", "true");
            TokenStream tsNine = factory.Create(
                new KeywordTokenizer(new StringReader(nine)));
            TokenStream tsTen = factory.Create(
                new KeywordTokenizer(new StringReader(ten)));
            assertCollation(tsNine, tsTen, -1);
        }

        /*
         * Setting caseLevel=true to create an additional case level between
         * secondary and tertiary
         */
        [Test]
        public void TestIgnoreAccentsButNotCase()
        {
            String withAccents = "résumé";
            String withoutAccents = "resume";
            String withAccentsUpperCase = "Résumé";
            String withoutAccentsUpperCase = "Resume";
            TokenFilterFactory factory = tokenFilterFactory("ICUCollationKey",
                "locale", "en",
                "strength", "primary",
                "caseLevel", "true");
            TokenStream tsWithAccents = factory.Create(
                new KeywordTokenizer(new StringReader(withAccents)));
            TokenStream tsWithoutAccents = factory.Create(
                new KeywordTokenizer(new StringReader(withoutAccents)));
            assertCollatesToSame(tsWithAccents, tsWithoutAccents);

            TokenStream tsWithAccentsUpperCase = factory.Create(
                new KeywordTokenizer(new StringReader(withAccentsUpperCase)));
            TokenStream tsWithoutAccentsUpperCase = factory.Create(
                new KeywordTokenizer(new StringReader(withoutAccentsUpperCase)));
            assertCollatesToSame(tsWithAccentsUpperCase, tsWithoutAccentsUpperCase);

            // now assert that case still matters: resume < Resume
            TokenStream tsLower = factory.Create(
                new KeywordTokenizer(new StringReader(withoutAccents)));
            TokenStream tsUpper = factory.Create(
                new KeywordTokenizer(new StringReader(withoutAccentsUpperCase)));
            assertCollation(tsLower, tsUpper, -1);
        }

        /*
         * Setting caseFirst=upper to cause uppercase strings to sort
         * before lowercase ones.
         */
        [Test]
        public void TestUpperCaseFirst()
        {
            String lower = "resume";
            String upper = "Resume";
            TokenFilterFactory factory = tokenFilterFactory("ICUCollationKey",
                "locale", "en",
                "strength", "tertiary",
                "caseFirst", "upper");
            TokenStream tsLower = factory.Create(
                new KeywordTokenizer(new StringReader(lower)));
            TokenStream tsUpper = factory.Create(
                new KeywordTokenizer(new StringReader(upper)));
            assertCollation(tsUpper, tsLower, -1);
        }

        /*
         * For german, you might want oe to sort and match with o umlaut.
         * This is not the default, but you can make a customized ruleset to do this.
         *
         * The default is DIN 5007-1, this shows how to tailor a collator to get DIN 5007-2 behavior.
         *  http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4423383
         */
        [Test]
        public void TestCustomRules()
        {
            RuleBasedCollator baseCollator = (RuleBasedCollator)Collator.GetInstance(new UCultureInfo("de_DE"));

            String DIN5007_2_tailorings =
              "& ae , a\u0308 & AE , A\u0308" +
              "& oe , o\u0308 & OE , O\u0308" +
              "& ue , u\u0308 & UE , u\u0308";

            RuleBasedCollator tailoredCollator = new RuleBasedCollator(baseCollator.GetRules() + DIN5007_2_tailorings);
            string tailoredRules = tailoredCollator.GetRules();
            //
            // at this point, you would save these tailoredRules to a file, 
            // and use the custom parameter.
            //
            String germanUmlaut = "Töne";
            String germanOE = "Toene";
            IDictionary<String, String> args = new Dictionary<String, String>();
            args["custom"] = "rules.txt";
            args["strength"] = "primary";
            ICUCollationKeyFilterFactory factory = new ICUCollationKeyFilterFactory(args);
            factory.Inform(new StringMockResourceLoader(tailoredRules));
            TokenStream tsUmlaut = factory.Create(
                new KeywordTokenizer(new StringReader(germanUmlaut)));
            TokenStream tsOE = factory.Create(
                new KeywordTokenizer(new StringReader(germanOE)));

            assertCollatesToSame(tsUmlaut, tsOE);
        }

        private void assertCollatesToSame(TokenStream stream1, TokenStream stream2)
        {
            assertCollation(stream1, stream2, 0);
        }

        private void assertCollation(TokenStream stream1, TokenStream stream2, int comparison)
        {
            ICharTermAttribute term1 = stream1
                .AddAttribute<ICharTermAttribute>();
            ICharTermAttribute term2 = stream2
                .AddAttribute<ICharTermAttribute>();
            stream1.Reset();
            stream2.Reset();
            assertTrue(stream1.IncrementToken());
            assertTrue(stream2.IncrementToken());
            assertEquals((comparison).Signum(), (term1.toString().CompareToOrdinal(term2.toString())).Signum());
            assertFalse(stream1.IncrementToken());
            assertFalse(stream2.IncrementToken());
            stream1.End();
            stream2.End();
            stream1.Dispose();
            stream2.Dispose();
        }

        private class StringMockResourceLoader : IResourceLoader
        {
            String text;

            internal StringMockResourceLoader(String text)
            {
                this.text = text;
            }

            public T NewInstance<T>(String cname)
            {
                return default;
            }

            public Type FindType(String cname)
            {
                return null;
            }

            public Stream OpenResource(String resource)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(text));
            }
        }

        private TokenFilterFactory tokenFilterFactory(String name, params String[] keysAndValues)
        {
            Type clazz = TokenFilterFactory.LookupClass(name);
            if (keysAndValues.Length % 2 == 1)
            {
                throw new ArgumentException("invalid keysAndValues map");
            }
            IDictionary<String, String> args = new Dictionary<String, String>();
            for (int i = 0; i < keysAndValues.Length; i += 2)
            {
                String prev = args.Put(keysAndValues[i], keysAndValues[i + 1]);
                assertNull("duplicate values for key: " + keysAndValues[i], prev);
            }
            String previous = args.Put("luceneMatchVersion", TEST_VERSION_CURRENT.ToString());
            assertNull("duplicate values for key: luceneMatchVersion", previous);
            TokenFilterFactory factory = null;
            try
            {
                //factory = clazz.getConstructor(Map.class).newInstance(args);
                factory = (TokenFilterFactory)Activator.CreateInstance(clazz, args);
            }
            catch (Exception e) when (e.IsInvocationTargetException())
            {
                // to simplify tests that check for illegal parameters
                if (e.InnerException is ArgumentException argumentException)
                {
                    ExceptionDispatchInfo.Capture(argumentException).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                }
                else
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }
            if (factory is IResourceLoaderAware)
            {
                ((IResourceLoaderAware)factory).Inform(new ClasspathResourceLoader(GetType()));
            }
            return factory;
        }
    }
}
