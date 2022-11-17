using Lucene.Net.Analysis.Phonetic.Language;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Phonetic
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

    public class TestPhoneticFilterFactory : BaseTokenStreamTestCase
    {
        /**
   * Case: default
   */
        [Test]
        public void TestFactoryDefaults()
        {
            IDictionary<String, String> args = new Dictionary<String, String>();
            args[PhoneticFilterFactory.ENCODER] = "Metaphone";
            PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
            factory.Inform(new ClasspathResourceLoader(factory.GetType()));
            assertTrue(factory.GetEncoder() is Metaphone);
            assertTrue(factory.inject); // default
        }

        [Test]
        public void TestInjectFalse()
        {
            IDictionary<String, String> args = new Dictionary<String, String>();
            args[PhoneticFilterFactory.ENCODER] = "Metaphone";
            args[PhoneticFilterFactory.INJECT] = "false";
            PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
            factory.Inform(new ClasspathResourceLoader(factory.GetType()));
            assertFalse(factory.inject);
        }

        [Test]
        public void TestMaxCodeLength()
        {
            IDictionary<String, String> args = new Dictionary<String, String>();
            args[PhoneticFilterFactory.ENCODER] = "Metaphone";
            args[PhoneticFilterFactory.MAX_CODE_LENGTH] = "2";
            PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
            factory.Inform(new ClasspathResourceLoader(factory.GetType()));
            assertEquals(2, ((Metaphone)factory.GetEncoder()).MaxCodeLen);
        }

        /**
         * Case: Failures and Exceptions
         */
        [Test]
        public void TestMissingEncoder()
        {
            try
            {
                new PhoneticFilterFactory(new Dictionary<String, String>());
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Configuration Error: missing parameter 'encoder'"));
            }
        }
        [Test]
        public void TestUnknownEncoder()
        {
            try
            {
                IDictionary<String, String> args = new Dictionary<String, String>();
                args["encoder"] = "XXX";
                PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
                factory.Inform(new ClasspathResourceLoader(factory.GetType()));
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Error loading encoder"));
            }
        }

        [Test]
        public void TestUnknownEncoderReflection()
        {
            try
            {
                IDictionary<String, String> args = new Dictionary<String, String>();
                args["encoder"] = "org.apache.commons.codec.language.NonExistence";
                PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
                factory.Inform(new ClasspathResourceLoader(factory.GetType()));
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Error loading encoder"));
            }
        }

        /**
         * Case: Reflection
         */
        [Test]
        public void TestFactoryReflection()
        {
            IDictionary<String, String> args = new Dictionary<String, String>();
            args[PhoneticFilterFactory.ENCODER] = "Metaphone";
            PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
            factory.Inform(new ClasspathResourceLoader(factory.GetType()));
            assertTrue(factory.GetEncoder() is Metaphone);
            assertTrue(factory.inject); // default
        }

        /** 
         * we use "Caverphone2" as it is registered in the REGISTRY as Caverphone,
         * so this effectively tests reflection without package name
         */
        [Test]
        public void TestFactoryReflectionCaverphone2()
        {
            IDictionary<String, String> args = new Dictionary<String, String>();
            args[PhoneticFilterFactory.ENCODER] = "Caverphone2";
            PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
            factory.Inform(new ClasspathResourceLoader(factory.GetType()));
            assertTrue(factory.GetEncoder() is Caverphone2);
            assertTrue(factory.inject); // default
        }

        [Test]
        public void TestFactoryReflectionCaverphone()
        {
            IDictionary<String, String> args = new Dictionary<String, String>();
            args[PhoneticFilterFactory.ENCODER] = "Caverphone";
            PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
            factory.Inform(new ClasspathResourceLoader(factory.GetType()));
            assertTrue(factory.GetEncoder() is Caverphone2);
            assertTrue(factory.inject); // default
        }

        [Test]
        public void TestAlgorithms()
        {
            assertAlgorithm("Metaphone", "true", "aaa bbb ccc easgasg",
                new String[] { "A", "aaa", "B", "bbb", "KKK", "ccc", "ESKS", "easgasg" });
            assertAlgorithm("Metaphone", "false", "aaa bbb ccc easgasg",
                new String[] { "A", "B", "KKK", "ESKS" });


            assertAlgorithm("DoubleMetaphone", "true", "aaa bbb ccc easgasg",
                new String[] { "A", "aaa", "PP", "bbb", "KK", "ccc", "ASKS", "easgasg" });
            assertAlgorithm("DoubleMetaphone", "false", "aaa bbb ccc easgasg",
                new String[] { "A", "PP", "KK", "ASKS" });


            assertAlgorithm("Soundex", "true", "aaa bbb ccc easgasg",
                new String[] { "A000", "aaa", "B000", "bbb", "C000", "ccc", "E220", "easgasg" });
            assertAlgorithm("Soundex", "false", "aaa bbb ccc easgasg",
                new String[] { "A000", "B000", "C000", "E220" });


            assertAlgorithm("RefinedSoundex", "true", "aaa bbb ccc easgasg",
                new String[] { "A0", "aaa", "B1", "bbb", "C3", "ccc", "E034034", "easgasg" });
            assertAlgorithm("RefinedSoundex", "false", "aaa bbb ccc easgasg",
                new String[] { "A0", "B1", "C3", "E034034" });


            assertAlgorithm("Caverphone", "true", "Darda Karleen Datha Carlene",
                new String[] { "TTA1111111", "Darda", "KLN1111111", "Karleen",
                "TTA1111111", "Datha", "KLN1111111", "Carlene" });
            assertAlgorithm("Caverphone", "false", "Darda Karleen Datha Carlene",
                new String[] { "TTA1111111", "KLN1111111", "TTA1111111", "KLN1111111" });


            assertAlgorithm("ColognePhonetic", "true", "Meier Schmitt Meir Schmidt",
                new String[] { "67", "Meier", "862", "Schmitt",
                    "67", "Meir", "862", "Schmidt" });
            assertAlgorithm("ColognePhonetic", "false", "Meier Schmitt Meir Schmidt",
                new String[] { "67", "862", "67", "862" });
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new PhoneticFilterFactory(new Dictionary<String, String>() {
                    { "encoder", "Metaphone" },
                    { "bogusArg", "bogusValue" }
                });
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }

        internal static void assertAlgorithm(String algName, String inject, String input,
            String[] expected)
        {
            Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            IDictionary<String, String> args = new Dictionary<String, String>();
            args["encoder"] = algName;
            args["inject"] = inject;
            PhoneticFilterFactory factory = new PhoneticFilterFactory(args);
            factory.Inform(new ClasspathResourceLoader(factory.GetType()));
            TokenStream stream = factory.Create(tokenizer);
            AssertTokenStreamContents(stream, expected);
        }
    }
}
