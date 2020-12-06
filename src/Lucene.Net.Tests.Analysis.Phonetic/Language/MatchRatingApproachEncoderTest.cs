using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Phonetic.Language
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
    /// Series of tests for the Match Rating Approach algorithm.
    /// <para/>
    /// General naming nomenclature for the test is of the form:
    /// GeneralMetadataOnTheTestArea_ActualTestValues_ExpectedResult
    /// <para/>
    /// An unusual value is indicated by the term "corner case"
    /// </summary>
    public class MatchRatingApproachEncoderTest : StringEncoderAbstractTest<MatchRatingApproachEncoder>
    {
        // ********** BEGIN REGION - TEST SUPPORT METHODS

        [Test]
        public void TestAccentRemoval_AllLower_SuccessfullyRemoved()
        {
            Assert.AreEqual("aeiou", MatchRatingApproachEncoder.RemoveAccents("áéíóú")); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestAccentRemoval_WithSpaces_SuccessfullyRemovedAndSpacesInvariant()
        {
            Assert.AreEqual("ae io  u", MatchRatingApproachEncoder.RemoveAccents("áé íó  ú")); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestAccentRemoval_UpperandLower_SuccessfullyRemovedAndCaseInvariant()
        {
            Assert.AreEqual("AeiOuu", MatchRatingApproachEncoder.RemoveAccents("ÁeíÓuu")); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestAccentRemoval_MixedWithUnusualChars_SuccessfullyRemovedAndUnusualcharactersInvariant()
        {
            Assert.AreEqual("A-e'i.,o&u", MatchRatingApproachEncoder.RemoveAccents("Á-e'í.,ó&ú")); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestAccentRemoval_GerSpanFrenMix_SuccessfullyRemoved()
        {
            Assert.AreEqual("aeoußAEOUnNa", MatchRatingApproachEncoder.RemoveAccents("äëöüßÄËÖÜñÑà")); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestAccentRemoval_ComprehensiveAccentMix_AllSuccessfullyRemoved()
        {
            Assert.AreEqual("E,E,E,E,U,U,I,I,A,A,O,e,e,e,e,u,u,i,i,a,a,o,c",
                    MatchRatingApproachEncoder.RemoveAccents("È,É,Ê,Ë,Û,Ù,Ï,Î,À,Â,Ô,è,é,ê,ë,û,ù,ï,î,à,â,ô,ç")); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestAccentRemovalNormalString_NoChange()
        {
            Assert.AreEqual("Colorless green ideas sleep furiously", MatchRatingApproachEncoder.RemoveAccents("Colorless green ideas sleep furiously")); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestAccentRemoval_NINO_NoChange()
        {
            Assert.AreEqual("", MatchRatingApproachEncoder.RemoveAccents("")); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestAccentRemoval_NullValue_ReturnNullSuccessfully()
        {
            Assert.AreEqual(null, MatchRatingApproachEncoder.RemoveAccents(null)); // LUCENENET: Made RemoveAccents() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestRemoveSingleDoubleConsonants_BUBLE_RemovedSuccessfully()
        {
            Assert.AreEqual("BUBLE", this.StringEncoder.RemoveDoubleConsonants("BUBBLE"));
        }

        [Test]
        public void TestRemoveDoubleConsonants_MISSISSIPPI_RemovedSuccessfully()
        {
            Assert.AreEqual("MISISIPI", this.StringEncoder.RemoveDoubleConsonants("MISSISSIPPI"));
        }

        [Test]
        public void TestRemoveDoubleDoubleVowel_BEETLE_NotRemoved()
        {
            Assert.AreEqual("BEETLE", this.StringEncoder.RemoveDoubleConsonants("BEETLE"));
        }

        [Test]
        public void TestIsVowel_CapitalA_ReturnsTrue()
        {
            Assert.True(MatchRatingApproachEncoder.IsVowel("A")); // LUCENENET: Made IsVowel() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestIsVowel_SmallD_ReturnsFalse()
        {
            Assert.False(MatchRatingApproachEncoder.IsVowel("d")); // LUCENENET: Made IsVowel() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestRemoveVowel_ALESSANDRA_Returns_ALSSNDR()
        {
            Assert.AreEqual("ALSSNDR", this.StringEncoder.RemoveVowels("ALESSANDRA"));
        }

        [Test]
        public void TestRemoveVowel__AIDAN_Returns_ADN()
        {
            Assert.AreEqual("ADN", this.StringEncoder.RemoveVowels("AIDAN"));
        }

        [Test]
        public void TestRemoveVowel__DECLAN_Returns_DCLN()
        {
            Assert.AreEqual("DCLN", this.StringEncoder.RemoveVowels("DECLAN"));
        }

        [Test]
        public void TestGetFirstLast3__ALEXANDER_Returns_Aleder()
        {
            Assert.AreEqual("Aleder", this.StringEncoder.GetFirst3Last3("Alexzander"));
        }

        [Test]
        public void TestGetFirstLast3_PETE_Returns_PETE()
        {
            Assert.AreEqual("PETE", this.StringEncoder.GetFirst3Last3("PETE"));
        }

        [Test]
        public void TestleftTorightThenRightToLeft_ALEXANDER_ALEXANDRA_Returns4()
        {
            Assert.AreEqual(4, this.StringEncoder.LeftToRightThenRightToLeftProcessing("ALEXANDER", "ALEXANDRA"));
        }

        [Test]
        public void TestleftTorightThenRightToLeft_EINSTEIN_MICHAELA_Returns0()
        {
            Assert.AreEqual(0, this.StringEncoder.LeftToRightThenRightToLeftProcessing("EINSTEIN", "MICHAELA"));
        }

        [Test]
        public void TestGetMinRating_7_Return4_Successfully()
        {
            Assert.AreEqual(4, this.StringEncoder.GetMinRating(7));
        }

        [Test]
        public void TestGetMinRating_1_Returns5_Successfully()
        {
            Assert.AreEqual(5, this.StringEncoder.GetMinRating(1));
        }

        [Test]
        public void TestGetMinRating_2_Returns5_Successfully()
        {
            Assert.AreEqual(5, this.StringEncoder.GetMinRating(2));
        }

        [Test]
        public void TestGetMinRating_5_Returns4_Successfully()
        {
            Assert.AreEqual(4, this.StringEncoder.GetMinRating(5));
        }

        [Test]
        public void TestGetMinRating_5_Returns4_Successfully2()
        {
            Assert.AreEqual(4, this.StringEncoder.GetMinRating(5));
        }

        [Test]
        public void TestGetMinRating_6_Returns4_Successfully()
        {
            Assert.AreEqual(4, this.StringEncoder.GetMinRating(6));
        }

        [Test]
        public void TestGetMinRating_7_Returns4_Successfully()
        {
            Assert.AreEqual(4, this.StringEncoder.GetMinRating(7));
        }

        [Test]
        public void TestGetMinRating_8_Returns3_Successfully()
        {
            Assert.AreEqual(3, this.StringEncoder.GetMinRating(8));
        }

        [Test]
        public void TestGetMinRating_10_Returns3_Successfully()
        {
            Assert.AreEqual(3, this.StringEncoder.GetMinRating(10));
        }

        [Test]
        public void TestGetMinRating_11_Returns_3_Successfully()
        {
            Assert.AreEqual(3, this.StringEncoder.GetMinRating(11));
        }

        [Test]
        public void TestGetMinRating_13_Returns_1_Successfully()
        {
            Assert.AreEqual(1, this.StringEncoder.GetMinRating(13));
        }

        [Test]
        public void TestCleanName_SuccessfullyClean()
        {
            Assert.AreEqual("THISISATEST", this.StringEncoder.CleanName("This-ís   a t.,es &t"));
        }

        [Test]
        public void TestIsVowel_SingleVowel_ReturnsTrue()
        {
            Assert.True(MatchRatingApproachEncoder.IsVowel(("I"))); // LUCENENET: Made IsVowel() static per CA1822 - it is internal anyway
        }

        [Test]
        public void TestIsEncodeEquals_CornerCase_SecondNameNothing_ReturnsFalse()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("test", ""));
        }

        [Test]
        public void TestIsEncodeEquals_CornerCase_FirstNameNothing_ReturnsFalse()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("", "test"));
        }

        [Test]
        public void TestIsEncodeEquals_CornerCase_SecondNameJustSpace_ReturnsFalse()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("test", " "));
        }

        [Test]
        public void TestIsEncodeEquals_CornerCase_FirstNameJustSpace_ReturnsFalse()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals(" ", "test"));
        }

        [Test]
        public void TestIsEncodeEquals_CornerCase_SecondNameNull_ReturnsFalse()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("test", null));
        }

        [Test]
        public void TestIsEncodeEquals_CornerCase_FirstNameNull_ReturnsFalse()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals(null, "test"));
        }

        [Test]
        public void TestIsEncodeEquals_CornerCase_FirstNameJust1Letter_ReturnsFalse()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("t", "test"));
        }

        [Test]
        public void TestIsEncodeEqualsSecondNameJust1Letter_ReturnsFalse()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("test", "t"));
        }

        // ***** END REGION - TEST SUPPORT METHODS

        // ***** BEGIN REGION - TEST GET MRA ENCODING

        [Test]
        public void TestGetEncoding_HARPER_HRPR()
        {
            Assert.AreEqual("HRPR", this.StringEncoder.Encode("HARPER"));
        }

        [Test]
        public void TestGetEncoding_SMITH_to_SMTH()
        {
            Assert.AreEqual("SMTH", this.StringEncoder.Encode("Smith"));
        }

        [Test]
        public void TestGetEncoding_SMYTH_to_SMYTH()
        {
            Assert.AreEqual("SMYTH", this.StringEncoder.Encode("Smyth"));
        }

        [Test]
        public void TestGetEncoding_Space_to_Nothing()
        {
            Assert.AreEqual("", this.StringEncoder.Encode(" "));
        }

        [Test]
        public void TestGetEncoding_NoSpace_to_Nothing()
        {
            Assert.AreEqual("", this.StringEncoder.Encode(""));
        }

        [Test]
        public void TestGetEncoding_Null_to_Nothing()
        {
            Assert.AreEqual("", this.StringEncoder.Encode(null));
        }

        [Test]
        public void TestGetEncoding_One_Letter_to_Nothing()
        {
            Assert.AreEqual("", this.StringEncoder.Encode("E"));
        }

        [Test]
        public void TestCompareNameNullSpace_ReturnsFalseSuccessfully()
        {
            Assert.False(StringEncoder.IsEncodeEquals(null, " "));
        }

        [Test]
        public void TestCompareNameSameNames_ReturnsFalseSuccessfully()
        {
            Assert.True(StringEncoder.IsEncodeEquals("John", "John"));
        }

        // ***** END REGION - TEST GET MRA ENCODING

        // ***** BEGIN REGION - TEST GET MRA COMPARISONS

        [Test]
        public void TestCompare_SMITH_SMYTH_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("smith", "smyth"));
        }

        [Test]
        public void TestCompare_BURNS_BOURNE_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Burns", "Bourne"));
        }

        [Test]
        public void TestCompare_ShortNames_AL_ED_WorksButNoMatch()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Al", "Ed"));
        }

        [Test]
        public void TestCompare_CATHERINE_KATHRYN_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Catherine", "Kathryn"));
        }

        [Test]
        public void TestCompare_BRIAN_BRYAN_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Brian", "Bryan"));
        }

        [Test]
        public void TestCompare_SEAN_SHAUN_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Séan", "Shaun"));
        }

        [Test]
        public void TestCompare_COLM_COLIN_WithAccentsAndSymbolsAndSpaces_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Cólm.   ", "C-olín"));
        }

        [Test]
        public void TestCompare_STEPHEN_STEVEN_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Stephen", "Steven"));
        }

        [Test]
        public void TestCompare_STEVEN_STEFAN_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Steven", "Stefan"));
        }

        [Test]
        public void TestCompare_STEPHEN_STEFAN_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Stephen", "Stefan"));
        }

        [Test]
        public void TestCompare_SAM_SAMUEL_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Sam", "Samuel"));
        }

        [Test]
        public void TestCompare_MICKY_MICHAEL_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Micky", "Michael"));
        }

        [Test]
        public void TestCompare_OONA_OONAGH_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Oona", "Oonagh"));
        }

        [Test]
        public void TestCompare_SOPHIE_SOFIA_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Sophie", "Sofia"));
        }

        [Test]
        public void TestCompare_FRANCISZEK_FRANCES_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Franciszek", "Frances"));
        }

        [Test]
        public void TestCompare_TOMASZ_TOM_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Tomasz", "tom"));
        }

        [Test]
        public void TestCompare_SmallInput_CARK_Kl_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Kl", "Karl"));
        }

        [Test]
        public void TestCompareNameToSingleLetter_KARL_C_DoesNotMatch()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Karl", "C"));
        }

        [Test]
        public void TestCompare_ZACH_ZAKARIA_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Zach", "Zacharia"));
        }

        [Test]
        public void TestCompare_KARL_ALESSANDRO_DoesNotMatch()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Karl", "Alessandro"));
        }

        [Test]
        public void TestCompare_Forenames_UNA_OONAGH_ShouldSuccessfullyMatchButDoesNot()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Úna", "Oonagh")); // Disappointing
        }

        // ***** Begin Region - Test Get Encoding - Surnames

        [Test]
        public void TestCompare_Surname_OSULLIVAN_OSUILLEABHAIN_SuccessfulMatch()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("O'Sullivan", "Ó ' Súilleabháin"));
        }

        [Test]
        public void TestCompare_LongSurnames_MORIARTY_OMUIRCHEARTAIGH_DoesNotSuccessfulMatch()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Moriarty", "OMuircheartaigh"));
        }

        [Test]
        public void TestCompare_LongSurnames_OMUIRCHEARTAIGH_OMIREADHAIGH_SuccessfulMatch()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("o'muireadhaigh", "Ó 'Muircheartaigh "));
        }

        [Test]
        public void TestCompare_Surname_COOPERFLYNN_SUPERLYN_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Cooper-Flynn", "Super-Lyn"));
        }

        [Test]
        public void TestCompare_Surname_HAILEY_HALLEY_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Hailey", "Halley"));
        }

        // **** BEGIN YIDDISH/SLAVIC SECTION ****

        [Test]
        public void TestCompare_Surname_AUERBACH_UHRBACH_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Auerbach", "Uhrbach"));
        }

        [Test]
        public void TestCompare_Surname_MOSKOWITZ_MOSKOVITZ_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Moskowitz", "Moskovitz"));
        }

        [Test]
        public void TestCompare_Surname_LIPSHITZ_LIPPSZYC_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("LIPSHITZ", "LIPPSZYC"));
        }

        [Test]
        public void TestCompare_Surname_LEWINSKY_LEVINSKI_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("LEWINSKY", "LEVINSKI"));
        }

        [Test]
        public void TestCompare_Surname_SZLAMAWICZ_SHLAMOVITZ_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("SZLAMAWICZ", "SHLAMOVITZ"));
        }

        [Test]
        public void TestCompare_Surname_ROSOCHOWACIEC_ROSOKHOVATSETS_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("R o s o ch o w a c ie c", " R o s o k ho v a ts e ts"));
        }

        [Test]
        public void TestCompare_Surname_PRZEMYSL_PSHEMESHIL_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals(" P rz e m y s l", " P sh e m e sh i l"));
        }

        // **** END YIDDISH/SLAVIC SECTION ****

        [Test]
        public void TestCompare_PETERSON_PETERS_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Peterson", "Peters"));
        }

        [Test]
        public void TestCompare_MCGOWAN_MCGEOGHEGAN_SuccessfullyMatched()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("McGowan", "Mc Geoghegan"));
        }

        [Test]
        public void TestCompare_SurnamesCornerCase_MURPHY_Space_NoMatch()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Murphy", " "));
        }

        [Test]
        public void TestCompare_SurnamesCornerCase_MURPHY_NoSpace_NoMatch()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Murphy", ""));
        }

        [Test]
        public void TestCompare_SurnameCornerCase_Nulls_NoMatch()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals(null, null));
        }

        [Test]
        public void TestCompare_Surnames_MURPHY_LYNCH_NoMatchExpected()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Murphy", "Lynch"));
        }

        [Test]
        public void TestCompare_Forenames_SEAN_JOHN_MatchExpected()
        {
            Assert.True(this.StringEncoder.IsEncodeEquals("Sean", "John"));
        }

        [Test]
        public void TestCompare_Forenames_SEAN_PETE_NoMatchExpected()
        {
            Assert.False(this.StringEncoder.IsEncodeEquals("Sean", "Pete"));
        }

        protected override MatchRatingApproachEncoder CreateStringEncoder()
        {
            return new MatchRatingApproachEncoder();
        }

        // ***** END REGION - TEST GET MRA COMPARISONS

    }
}
