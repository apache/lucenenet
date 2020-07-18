using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Phonetic.Language.Bm
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
    /// Tests guessLanguages API.
    /// <para/>
    /// since 1.6
    /// </summary>
    public class LanguageGuessingTest
    {
        private const string EXACT = "exact";
        private const string ONE_OF = "one of";

        public static IList<object[]> Values = new object[][] {
                new object[] { "Renault", "french", EXACT },
                new object[] { "Mickiewicz", "polish", EXACT },
                new object[] { "Thompson", "english", ONE_OF }, // this also hits german and greeklatin
                new object[] { "Nu\u00f1ez", "spanish", EXACT }, // Nuñez
                new object[] { "Carvalho", "portuguese", EXACT },
                new object[] { "\u010capek", "czech", EXACT }, // Čapek
                new object[] { "Sjneijder", "dutch", EXACT },
                new object[] { "Klausewitz", "german", EXACT },
                new object[] { "K\u00fc\u00e7\u00fck", "turkish", EXACT }, // Küçük
                new object[] { "Giacometti", "italian", EXACT },
                new object[] { "Nagy", "hungarian", EXACT },
                new object[] { "Ceau\u015fescu", "romanian", EXACT }, // Ceauşescu
                new object[] { "Angelopoulos", "greeklatin", EXACT },
                new object[] { "\u0391\u03b3\u03b3\u03b5\u03bb\u03cc\u03c0\u03bf\u03c5\u03bb\u03bf\u03c2", "greek", EXACT }, // Αγγελόπουλος
                new object[] { "\u041f\u0443\u0448\u043a\u0438\u043d", "cyrillic", EXACT }, // Пушкин
                new object[] { "\u05db\u05d4\u05df", "hebrew", EXACT }, // כהן
                new object[] { "\u00e1cz", "any", EXACT }, // ácz
                new object[] { "\u00e1tz", "any", EXACT } // átz
        };
            
       

        //private readonly String exactness;

        private readonly Lang lang = Lang.GetInstance(NameType.GENERIC);
        //private readonly String language;
        //private readonly String name;

        //[TestCaseSource("Values")]
        //public LanguageGuessingTest(String name, String language, String exactness)
        //{
        //    this.name = name;
        //    this.language = language;
        //    this.exactness = exactness;
        //}

        [Test]
        [TestCaseSource("Values")]
        public void TestLanguageGuessing(String name, String language, String exactness)
        {
            LanguageSet guesses = this.lang.GuessLanguages(name);

            Assert.True(guesses.Contains(language),
                "language predicted for name '" + name + "' is wrong: " + guesses + " should contain '" + language + "'"
                    );

        }
    }
}
