using System;
using System.Text;

namespace org.apache.lucene.analysis.core
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

	using UAX29URLEmailAnalyzer = org.apache.lucene.analysis.standard.UAX29URLEmailAnalyzer;
	using Version = org.apache.lucene.util.Version;


	public class TestUAX29URLEmailAnalyzer : BaseTokenStreamTestCase
	{

	  private Analyzer a = new UAX29URLEmailAnalyzer(TEST_VERSION_CURRENT);

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
		BaseTokenStreamTestCase.assertAnalyzesTo(a, input, new string[]{"testing", "1234"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testArmenian() throws Exception
	  public virtual void testArmenian()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "Վիքիպեդիայի 13 միլիոն հոդվածները (4,600` հայերեն վիքիպեդիայում) գրվել են կամավորների կողմից ու համարյա բոլոր հոդվածները կարող է խմբագրել ցանկաց մարդ ով կարող է բացել Վիքիպեդիայի կայքը։", new string[] {"վիքիպեդիայի", "13", "միլիոն", "հոդվածները", "4,600", "հայերեն", "վիքիպեդիայում", "գրվել", "են", "կամավորների", "կողմից", "ու", "համարյա", "բոլոր", "հոդվածները", "կարող", "է", "խմբագրել", "ցանկաց", "մարդ", "ով", "կարող", "է", "բացել", "վիքիպեդիայի", "կայքը"});
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
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "الفيلم الوثائقي الأول عن ويكيبيديا يسمى \"الحقيقة بالأرقام: قصة ويكيبيديا\" (بالإنجليزية: Truth in Numbers: The Wikipedia Story)، سيتم إطلاقه في 2008.", new string[] {"الفيلم", "الوثائقي", "الأول", "عن", "ويكيبيديا", "يسمى", "الحقيقة", "بالأرقام", "قصة", "ويكيبيديا", "بالإنجليزية", "truth", "numbers", "wikipedia", "story", "سيتم", "إطلاقه", "في", "2008"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAramaic() throws Exception
	  public virtual void testAramaic()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "ܘܝܩܝܦܕܝܐ (ܐܢܓܠܝܐ: Wikipedia) ܗܘ ܐܝܢܣܩܠܘܦܕܝܐ ܚܐܪܬܐ ܕܐܢܛܪܢܛ ܒܠܫܢ̈ܐ ܣܓܝܐ̈ܐ܂ ܫܡܗ ܐܬܐ ܡܢ ܡ̈ܠܬܐ ܕ\"ܘܝܩܝ\" ܘ\"ܐܝܢܣܩܠܘܦܕܝܐ\"܀", new string[] {"ܘܝܩܝܦܕܝܐ", "ܐܢܓܠܝܐ", "wikipedia", "ܗܘ", "ܐܝܢܣܩܠܘܦܕܝܐ", "ܚܐܪܬܐ", "ܕܐܢܛܪܢܛ", "ܒܠܫܢ̈ܐ", "ܣܓܝܐ̈ܐ", "ܫܡܗ", "ܐܬܐ", "ܡܢ", "ܡ̈ܠܬܐ", "ܕ", "ܘܝܩܝ", "ܘ", "ܐܝܢܣܩܠܘܦܕܝܐ"});
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
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "Γράφεται σε συνεργασία από εθελοντές με το λογισμικό wiki, κάτι που σημαίνει ότι άρθρα μπορεί να προστεθούν ή να αλλάξουν από τον καθένα.", new string[] {"γράφεται", "σε", "συνεργασία", "από", "εθελοντές", "με", "το", "λογισμικό", "wiki", "κάτι", "που", "σημαίνει", "ότι", "άρθρα", "μπορεί", "να", "προστεθούν", "ή", "να", "αλλάξουν", "από", "τον", "καθένα"});
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
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "我是中国人。 １２３４ Ｔｅｓｔｓ ", new string[] {"我", "是", "中", "国", "人", "１２３４", "ｔｅｓｔｓ"});
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
		 * Standard analyzer does not correctly tokenize combining character U+0364 COMBINING LATIN SMALL LETTER E.
		 * The word "moͤchte" is incorrectly tokenized into "mo" "chte", the combining character is lost.
		 * Expected result is only one token "moͤchte".
		 */
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "moͤchte", new string[] {"moͤchte"});
	  }

	  /* Tests from StandardAnalyzer, just to show behavior is similar */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAlphanumericSA() throws Exception
	  public virtual void testAlphanumericSA()
	  {
		// alphanumeric tokens
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "B2B", new string[]{"b2b"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "2B", new string[]{"2b"});
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
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "O'Reilly", new string[]{"o'reilly"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "you're", new string[]{"you're"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "she's", new string[]{"she's"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "Jim's", new string[]{"jim's"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "don't", new string[]{"don't"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "O'Reilly's", new string[]{"o'reilly's"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNumericSA() throws Exception
	  public virtual void testNumericSA()
	  {
		// floating point, serial, model numbers, ip addresses, etc.
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "21.35", new string[]{"21.35"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "R2D2 C3PO", new string[]{"r2d2", "c3po"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "216.239.63.104", new string[]{"216.239.63.104"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "216.239.63.104", new string[]{"216.239.63.104"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTextWithNumbersSA() throws Exception
	  public virtual void testTextWithNumbersSA()
	  {
		// numbers
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "David has 5000 bones", new string[]{"david", "has", "5000", "bones"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVariousTextSA() throws Exception
	  public virtual void testVariousTextSA()
	  {
		// various
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "C embedded developers wanted", new string[]{"c", "embedded", "developers", "wanted"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "foo bar FOO BAR", new string[]{"foo", "bar", "foo", "bar"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[]{"foo", "bar", "foo", "bar"});
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "\"QUOTED\" word", new string[]{"quoted", "word"});
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
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "David has 5000 bones", new string[] {"david", "has", "5000", "bones"}, new int[] {0, 6, 10, 15}, new int[] {5, 9, 14, 20});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTypes() throws Exception
	  public virtual void testTypes()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "david has 5000 bones", new string[] {"david", "has", "5000", "bones"}, new string[] {"<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<ALPHANUM>"});
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

	  /// @deprecated remove this and sophisticated backwards layer in 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("remove this and sophisticated backwards layer in 5.0") public void testCombiningMarksBackwards() throws Exception
	  [Obsolete("remove this and sophisticated backwards layer in 5.0")]
	  public virtual void testCombiningMarksBackwards()
	  {
		Analyzer a = new UAX29URLEmailAnalyzer(Version.LUCENE_33);
		checkOneTerm(a, "ざ", "さ"); // hiragana Bug
		checkOneTerm(a, "ザ", "ザ"); // katakana Works
		checkOneTerm(a, "壹゙", "壹"); // ideographic Bug
		checkOneTerm(a, "아゙", "아゙"); // hangul Works
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasicEmails() throws Exception
	  public virtual void testBasicEmails()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "one test@example.com two three [A@example.CO.UK] \"ArakaBanassaMassanaBakarA\" <info@Info.info>", new string[] {"one", "test@example.com", "two", "three", "a@example.co.uk", "arakabanassamassanabakara", "info@info.info"}, new string[] {"<ALPHANUM>", "<EMAIL>", "<ALPHANUM>", "<ALPHANUM>", "<EMAIL>", "<ALPHANUM>", "<EMAIL>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMailtoSchemeEmails() throws Exception
	  public virtual void testMailtoSchemeEmails()
	  {
		// See LUCENE-3880
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "MAILTO:Test@Example.ORG", new string[] {"mailto", "test@example.org"}, new string[] {"<ALPHANUM>", "<EMAIL>"});

		// TODO: Support full mailto: scheme URIs. See RFC 6068: http://tools.ietf.org/html/rfc6068
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "mailto:personA@example.com,personB@example.com?cc=personC@example.com" + "&subject=Subjectivity&body=Corpusivity%20or%20something%20like%20that", new string[] {"mailto", "persona@example.com", ",personb@example.com", "?cc=personc@example.com", "subject", "subjectivity", "body", "corpusivity", "20or", "20something","20like", "20that"}, new string[] {"<ALPHANUM>", "<EMAIL>", "<EMAIL>", "<EMAIL>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>"}); // TODO: Hex decoding + re-tokenization -  TODO: split field keys/values
					// TODO: recognize ',' address delimiter. Also, see examples of ';' delimiter use at: http://www.mailto.co.uk/
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasicURLs() throws Exception
	  public virtual void testBasicURLs()
	  {
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "a <HTTPs://example.net/omg/isnt/that/NICE?no=its&n%30t#mntl-E>b-D ftp://www.example.com/ABC.txt file:///C:/path/to/a/FILE.txt C", new string[] {"https://example.net/omg/isnt/that/nice?no=its&n%30t#mntl-e", "b", "d", "ftp://www.example.com/abc.txt", "file:///c:/path/to/a/file.txt", "c"}, new string[] {"<URL>", "<ALPHANUM>", "<ALPHANUM>", "<URL>", "<URL>", "<ALPHANUM>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoSchemeURLs() throws Exception
	  public virtual void testNoSchemeURLs()
	  {
		// ".ph" is a Top Level Domain
		BaseTokenStreamTestCase.assertAnalyzesTo(a, "<index.ph>", new string[]{"index.ph"}, new string[]{"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "index.ph", new string[]{"index.ph"}, new string[]{"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "index.php", new string[]{"index.php"}, new string[]{"<ALPHANUM>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "index.phα", new string[]{"index.phα"}, new string[]{"<ALPHANUM>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "index-h.php", new string[] {"index", "h.php"}, new string[] {"<ALPHANUM>","<ALPHANUM>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "index2.php", new string[] {"index2", "php"}, new string[] {"<ALPHANUM>", "<ALPHANUM>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "index2.ph９,", new string[] {"index2", "ph９"}, new string[] {"<ALPHANUM>", "<ALPHANUM>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com,example.ph,index.php,index2.php,example2.ph", new string[] {"example.com", "example.ph", "index.php", "index2", "php", "example2.ph"}, new string[] {"<URL>", "<URL>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com:8080 example.com/path/here example.com?query=something example.com#fragment", new string[] {"example.com:8080", "example.com/path/here", "example.com?query=something", "example.com#fragment"}, new string[] {"<URL>", "<URL>", "<URL>", "<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com:8080/path/here?query=something#fragment", new string[] {"example.com:8080/path/here?query=something#fragment"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com:8080/path/here?query=something", new string[] {"example.com:8080/path/here?query=something"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com:8080/path/here#fragment", new string[] {"example.com:8080/path/here#fragment"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com:8080/path/here", new string[] {"example.com:8080/path/here"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com:8080?query=something#fragment", new string[] {"example.com:8080?query=something#fragment"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com:8080?query=something", new string[] {"example.com:8080?query=something"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com:8080#fragment", new string[] {"example.com:8080#fragment"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com/path/here?query=something#fragment", new string[] {"example.com/path/here?query=something#fragment"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com/path/here?query=something", new string[] {"example.com/path/here?query=something"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com/path/here#fragment", new string[] {"example.com/path/here#fragment"}, new string[] {"<URL>"});

		BaseTokenStreamTestCase.assertAnalyzesTo(a, "example.com?query=something#fragment", new string[] {"example.com?query=something#fragment"}, new string[] {"<URL>"});
	  }


	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new UAX29URLEmailAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}