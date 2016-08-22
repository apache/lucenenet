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

using System;
using System.Text;

namespace Lucene.Net.Tests.Analysis.Common.Analysis.Core
{




	using StandardAnalyzer = org.apache.lucene.analysis.standard.StandardAnalyzer;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using Version = org.apache.lucene.util.Version;

	public class TestStandardAnalyzer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHugeDoc() throws java.io.IOException
	  public virtual void testHugeDoc()
	  {
		StringBuilder sb = new StringBuilder();
		char[] whitespace = new char[4094];
		Arrays.fill(whitespace, ' ');
		sb.Append(whitespace);
		sb.Append("testing 1234");
		string input = sb.ToString();
		StandardTokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
		BaseTokenStreamTestCase.assertTokenStreamContents(tokenizer, new string[] {"testing", "1234"});
	  }

	  private Analyzer a = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {

			Tokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testArmenian() throws Exception
	  public virtual void testArmenian()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "Վիքիպեդիայի 13 միլիոն հոդվածները (4,600` հայերեն վիքիպեդիայում) գրվել են կամավորների կողմից ու համարյա բոլոր հոդվածները կարող է խմբագրել ցանկաց մարդ ով կարող է բացել Վիքիպեդիայի կայքը։", new string[] {"Վիքիպեդիայի", "13", "միլիոն", "հոդվածները", "4,600", "հայերեն", "վիքիպեդիայում", "գրվել", "են", "կամավորների", "կողմից", "ու", "համարյա", "բոլոր", "հոդվածները", "կարող", "է", "խմբագրել", "ցանկաց", "մարդ", "ով", "կարող", "է", "բացել", "Վիքիպեդիայի", "կայքը"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAmharic() throws Exception
	  public virtual void testAmharic()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "ዊኪፔድያ የባለ ብዙ ቋንቋ የተሟላ ትክክለኛና ነጻ መዝገበ ዕውቀት (ኢንሳይክሎፒዲያ) ነው። ማንኛውም", new string[] {"ዊኪፔድያ", "የባለ", "ብዙ", "ቋንቋ", "የተሟላ", "ትክክለኛና", "ነጻ", "መዝገበ", "ዕውቀት", "ኢንሳይክሎፒዲያ", "ነው", "ማንኛውም"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testArabic() throws Exception
	  public virtual void testArabic()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "الفيلم الوثائقي الأول عن ويكيبيديا يسمى \"الحقيقة بالأرقام: قصة ويكيبيديا\" (بالإنجليزية: Truth in Numbers: The Wikipedia Story)، سيتم إطلاقه في 2008.", new string[] {"الفيلم", "الوثائقي", "الأول", "عن", "ويكيبيديا", "يسمى", "الحقيقة", "بالأرقام", "قصة", "ويكيبيديا", "بالإنجليزية", "Truth", "in", "Numbers", "The", "Wikipedia", "Story", "سيتم", "إطلاقه", "في", "2008"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAramaic() throws Exception
	  public virtual void testAramaic()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "ܘܝܩܝܦܕܝܐ (ܐܢܓܠܝܐ: Wikipedia) ܗܘ ܐܝܢܣܩܠܘܦܕܝܐ ܚܐܪܬܐ ܕܐܢܛܪܢܛ ܒܠܫܢ̈ܐ ܣܓܝܐ̈ܐ܂ ܫܡܗ ܐܬܐ ܡܢ ܡ̈ܠܬܐ ܕ\"ܘܝܩܝ\" ܘ\"ܐܝܢܣܩܠܘܦܕܝܐ\"܀", new string[] {"ܘܝܩܝܦܕܝܐ", "ܐܢܓܠܝܐ", "Wikipedia", "ܗܘ", "ܐܝܢܣܩܠܘܦܕܝܐ", "ܚܐܪܬܐ", "ܕܐܢܛܪܢܛ", "ܒܠܫܢ̈ܐ", "ܣܓܝܐ̈ܐ", "ܫܡܗ", "ܐܬܐ", "ܡܢ", "ܡ̈ܠܬܐ", "ܕ", "ܘܝܩܝ", "ܘ", "ܐܝܢܣܩܠܘܦܕܝܐ"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBengali() throws Exception
	  public virtual void testBengali()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "এই বিশ্বকোষ পরিচালনা করে উইকিমিডিয়া ফাউন্ডেশন (একটি অলাভজনক সংস্থা)। উইকিপিডিয়ার শুরু ১৫ জানুয়ারি, ২০০১ সালে। এখন পর্যন্ত ২০০টিরও বেশী ভাষায় উইকিপিডিয়া রয়েছে।", new string[] {"এই", "বিশ্বকোষ", "পরিচালনা", "করে", "উইকিমিডিয়া", "ফাউন্ডেশন", "একটি", "অলাভজনক", "সংস্থা", "উইকিপিডিয়ার", "শুরু", "১৫", "জানুয়ারি", "২০০১", "সালে", "এখন", "পর্যন্ত", "২০০টিরও", "বেশী", "ভাষায়", "উইকিপিডিয়া", "রয়েছে"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFarsi() throws Exception
	  public virtual void testFarsi()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "ویکی پدیای انگلیسی در تاریخ ۲۵ دی ۱۳۷۹ به صورت مکملی برای دانشنامهٔ تخصصی نوپدیا نوشته شد.", new string[] {"ویکی", "پدیای", "انگلیسی", "در", "تاریخ", "۲۵", "دی", "۱۳۷۹", "به", "صورت", "مکملی", "برای", "دانشنامهٔ", "تخصصی", "نوپدیا", "نوشته", "شد"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testGreek() throws Exception
	  public virtual void testGreek()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "Γράφεται σε συνεργασία από εθελοντές με το λογισμικό wiki, κάτι που σημαίνει ότι άρθρα μπορεί να προστεθούν ή να αλλάξουν από τον καθένα.", new string[] {"Γράφεται", "σε", "συνεργασία", "από", "εθελοντές", "με", "το", "λογισμικό", "wiki", "κάτι", "που", "σημαίνει", "ότι", "άρθρα", "μπορεί", "να", "προστεθούν", "ή", "να", "αλλάξουν", "από", "τον", "καθένα"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testThai() throws Exception
	  public virtual void testThai()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "การที่ได้ต้องแสดงว่างานดี. แล้วเธอจะไปไหน? ๑๒๓๔", new string[] {"การที่ได้ต้องแสดงว่างานดี", "แล้วเธอจะไปไหน", "๑๒๓๔"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLao() throws Exception
	  public virtual void testLao()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "ສາທາລະນະລັດ ປະຊາທິປະໄຕ ປະຊາຊົນລາວ", new string[] {"ສາທາລະນະລັດ", "ປະຊາທິປະໄຕ", "ປະຊາຊົນລາວ"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTibetan() throws Exception
	  public virtual void testTibetan()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "སྣོན་མཛོད་དང་ལས་འདིས་བོད་ཡིག་མི་ཉམས་གོང་འཕེལ་དུ་གཏོང་བར་ཧ་ཅང་དགེ་མཚན་མཆིས་སོ། །", new string[] {"སྣོན", "མཛོད", "དང", "ལས", "འདིས", "བོད", "ཡིག", "མི", "ཉམས", "གོང", "འཕེལ", "དུ", "གཏོང", "བར", "ཧ", "ཅང", "དགེ", "མཚན", "མཆིས", "སོ"});
	  }

	  /*
	   * For chinese, tokenize as char (these can later form bigrams or whatever)
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testChinese() throws Exception
	  public virtual void testChinese()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "我是中国人。 １２３４ Ｔｅｓｔｓ ", new string[] {"我", "是", "中", "国", "人", "１２３４", "Ｔｅｓｔｓ"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmpty() throws Exception
	  public virtual void testEmpty()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "", new string[] {});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, ".", new string[] {});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, " ", new string[] {});
	  }

	  /* test various jira issues this analyzer is related to */

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLUCENE1545() throws Exception
	  public virtual void testLUCENE1545()
	  {
		/*
		 * Standard analyzer does not correctly tokenize combining character U+0364 COMBINING LATIN SMALL LETTRE E.
		 * The word "moͤchte" is incorrectly tokenized into "mo" "chte", the combining character is lost.
		 * Expected result is only on token "moͤchte".
		 */
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "moͤchte", new string[] {"moͤchte"});
	  }

	  /* Tests from StandardAnalyzer, just to show behavior is similar */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAlphanumericSA() throws Exception
	  public virtual void testAlphanumericSA()
	  {
		// alphanumeric tokens
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "B2B", new string[]{"B2B"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "2B", new string[]{"2B"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDelimitersSA() throws Exception
	  public virtual void testDelimitersSA()
	  {
		// other delimiters: "-", "/", ","
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "some-dashed-phrase", new string[]{"some", "dashed", "phrase"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "dogs,chase,cats", new string[]{"dogs", "chase", "cats"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "ac/dc", new string[]{"ac", "dc"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testApostrophesSA() throws Exception
	  public virtual void testApostrophesSA()
	  {
		// internal apostrophes: O'Reilly, you're, O'Reilly's
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "O'Reilly", new string[]{"O'Reilly"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "you're", new string[]{"you're"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "she's", new string[]{"she's"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "Jim's", new string[]{"Jim's"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "don't", new string[]{"don't"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "O'Reilly's", new string[]{"O'Reilly's"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNumericSA() throws Exception
	  public virtual void testNumericSA()
	  {
		// floating point, serial, model numbers, ip addresses, etc.
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "21.35", new string[]{"21.35"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "R2D2 C3PO", new string[]{"R2D2", "C3PO"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "216.239.63.104", new string[]{"216.239.63.104"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "216.239.63.104", new string[]{"216.239.63.104"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTextWithNumbersSA() throws Exception
	  public virtual void testTextWithNumbersSA()
	  {
		// numbers
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "David has 5000 bones", new string[]{"David", "has", "5000", "bones"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVariousTextSA() throws Exception
	  public virtual void testVariousTextSA()
	  {
		// various
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "C embedded developers wanted", new string[]{"C", "embedded", "developers", "wanted"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "foo bar FOO BAR", new string[]{"foo", "bar", "FOO", "BAR"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[]{"foo", "bar", "FOO", "BAR"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "\"QUOTED\" word", new string[]{"QUOTED", "word"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKoreanSA() throws Exception
	  public virtual void testKoreanSA()
	  {
		// Korean words
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "안녕하세요 한글입니다", new string[]{"안녕하세요", "한글입니다"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOffsets() throws Exception
	  public virtual void testOffsets()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "David has 5000 bones", new string[] {"David", "has", "5000", "bones"}, new int[] {0, 6, 10, 15}, new int[] {5, 9, 14, 20});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTypes() throws Exception
	  public virtual void testTypes()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "David has 5000 bones", new string[] {"David", "has", "5000", "bones"}, new string[] {"<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<ALPHANUM>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUnicodeWordBreaks() throws Exception
	  public virtual void testUnicodeWordBreaks()
	  {
		WordBreakTestUnicode_6_3_0 wordBreakTest = new WordBreakTestUnicode_6_3_0();
		wordBreakTest.test(a);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSupplementary() throws Exception
	  public virtual void testSupplementary()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "𩬅艱鍟䇹愯瀛", new string[] {"𩬅", "艱", "鍟", "䇹", "愯", "瀛"}, new string[] {"<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKorean() throws Exception
	  public virtual void testKorean()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "훈민정음", new string[] {"훈민정음"}, new string[] {"<HANGUL>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testJapanese() throws Exception
	  public virtual void testJapanese()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "仮名遣い カタカナ", new string[] {"仮", "名", "遣", "い", "カタカナ"}, new string[] {"<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<HIRAGANA>", "<KATAKANA>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCombiningMarks() throws Exception
	  public virtual void testCombiningMarks()
	  {
		checkOneTerm(a, "ざ", "ざ"); // hiragana
		checkOneTerm(a, "ザ", "ザ"); // katakana
		checkOneTerm(a, "壹゙", "壹゙"); // ideographic
		checkOneTerm(a, "아゙", "아゙"); // hangul
	  }

	  /// <summary>
	  /// Multiple consecutive chars in \p{WB:MidLetter}, \p{WB:MidNumLet},
	  /// and/or \p{MidNum} should trigger a token split.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMid() throws Exception
	  public virtual void testMid()
	  {
		// ':' is in \p{WB:MidLetter}, which should trigger a split unless there is a Letter char on both sides
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A:B", new string[] {"A:B"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A::B", new string[] {"A", "B"});

		// '.' is in \p{WB:MidNumLet}, which should trigger a split unless there is a Letter or Numeric char on both sides
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1.2", new string[] {"1.2"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A.B", new string[] {"A.B"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1..2", new string[] {"1", "2"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A..B", new string[] {"A", "B"});

		// ',' is in \p{WB:MidNum}, which should trigger a split unless there is a Numeric char on both sides
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1,2", new string[] {"1,2"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1,,2", new string[] {"1", "2"});

		// Mixed consecutive \p{WB:MidLetter} and \p{WB:MidNumLet} should trigger a split
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A.:B", new string[] {"A", "B"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A:.B", new string[] {"A", "B"});

		// Mixed consecutive \p{WB:MidNum} and \p{WB:MidNumLet} should trigger a split
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1,.2", new string[] {"1", "2"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1.,2", new string[] {"1", "2"});

		// '_' is in \p{WB:ExtendNumLet}

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A:B_A:B", new string[] {"A:B_A:B"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A:B_A::B", new string[] {"A:B_A", "B"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1.2_1.2", new string[] {"1.2_1.2"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A.B_A.B", new string[] {"A.B_A.B"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1.2_1..2", new string[] {"1.2_1", "2"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "A.B_A..B", new string[] {"A.B_A", "B"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1,2_1,2", new string[] {"1,2_1,2"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "1,2_1,,2", new string[] {"1,2_1", "2"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "C_A.:B", new string[] {"C_A", "B"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "C_A:.B", new string[] {"C_A", "B"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "3_1,.2", new string[] {"3_1", "2"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "3_1.,2", new string[] {"3_1", "2"});
	  }


	  /// @deprecated remove this and sophisticated backwards layer in 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("remove this and sophisticated backwards layer in 5.0") public void testCombiningMarksBackwards() throws Exception
	  [Obsolete("remove this and sophisticated backwards layer in 5.0")]
	  public virtual void testCombiningMarksBackwards()
	  {
		Analyzer a = new StandardAnalyzer(Version.LUCENE_33);
		checkOneTerm(a, "ざ", "さ"); // hiragana Bug
		checkOneTerm(a, "ザ", "ザ"); // katakana Works
		checkOneTerm(a, "壹゙", "壹"); // ideographic Bug
		checkOneTerm(a, "아゙", "아゙"); // hangul Works
	  }

	  /// @deprecated uses older unicode (6.0). simple test to make sure its basically working 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("uses older unicode (6.0). simple test to make sure its basically working") public void testVersion36() throws Exception
	  [Obsolete("uses older unicode (6.0). simple test to make sure its basically working")]
	  public virtual void testVersion36()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		assertAnalyzesTo(a, "this is just a t\u08E6st lucene@apache.org", new string[] {"this", "is", "just", "a", "t", "st", "lucene", "apache.org"}); // new combining mark in 6.1
	  };

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestStandardAnalyzer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestStandardAnalyzer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new StandardTokenizer(Version.LUCENE_36, reader);
			return new TokenStreamComponents(tokenizer);
		  }
	  }

	  /// @deprecated uses older unicode (6.1). simple test to make sure its basically working 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("uses older unicode (6.1). simple test to make sure its basically working") public void testVersion40() throws Exception
	  [Obsolete("uses older unicode (6.1). simple test to make sure its basically working")]
	  public virtual void testVersion40()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		// U+061C is a new combining mark in 6.3, found using "[[\p{WB:Format}\p{WB:Extend}]&[^\p{Age:6.2}]]"
		// on the online UnicodeSet utility: <http://unicode.org/cldr/utility/list-unicodeset.jsp>
		assertAnalyzesTo(a, "this is just a t\u061Cst lucene@apache.org", new string[] {"this", "is", "just", "a", "t", "st", "lucene", "apache.org"});
	  };

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestStandardAnalyzer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestStandardAnalyzer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new StandardTokenizer(Version.LUCENE_40, reader);
			return new TokenStreamComponents(tokenizer);
		  }
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new StandardAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// blast some random large strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStrings() throws Exception
	  public virtual void testRandomHugeStrings()
	  {
		Random random = random();
		checkRandomData(random, new StandardAnalyzer(TEST_VERSION_CURRENT), 100 * RANDOM_MULTIPLIER, 8192);
	  }

	  // Adds random graph after:
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStringsGraphAfter() throws Exception
	  public virtual void testRandomHugeStringsGraphAfter()
	  {
		Random random = random();
		checkRandomData(random, new AnalyzerAnonymousInnerClassHelper4(this), 100 * RANDOM_MULTIPLIER, 8192);
	  }

	  private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
	  {
		  private readonly TestStandardAnalyzer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper4(TestStandardAnalyzer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			TokenStream tokenStream = new MockGraphTokenFilter(random(), tokenizer);
			return new TokenStreamComponents(tokenizer, tokenStream);
		  }
	  }
	}

}