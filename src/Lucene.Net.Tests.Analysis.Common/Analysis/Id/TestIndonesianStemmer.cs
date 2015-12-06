namespace org.apache.lucene.analysis.id
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


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	/// <summary>
	/// Tests <seealso cref="IndonesianStemmer"/>
	/// </summary>
	public class TestIndonesianStemmer : BaseTokenStreamTestCase
	{
	  /* full stemming, no stopwords */
	  internal Analyzer a = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  public override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new IndonesianStemFilter(tokenizer));
		  }
	  }

	  /// <summary>
	  /// Some examples from the paper </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExamples() throws java.io.IOException
	  public virtual void testExamples()
	  {
		checkOneTerm(a, "bukukah", "buku");
		checkOneTerm(a, "adalah", "ada");
		checkOneTerm(a, "bukupun", "buku");
		checkOneTerm(a, "bukuku", "buku");
		checkOneTerm(a, "bukumu", "buku");
		checkOneTerm(a, "bukunya", "buku");
		checkOneTerm(a, "mengukur", "ukur");
		checkOneTerm(a, "menyapu", "sapu");
		checkOneTerm(a, "menduga", "duga");
		checkOneTerm(a, "menuduh", "uduh");
		checkOneTerm(a, "membaca", "baca");
		checkOneTerm(a, "merusak", "rusak");
		checkOneTerm(a, "pengukur", "ukur");
		checkOneTerm(a, "penyapu", "sapu");
		checkOneTerm(a, "penduga", "duga");
		checkOneTerm(a, "pembaca", "baca");
		checkOneTerm(a, "diukur", "ukur");
		checkOneTerm(a, "tersapu", "sapu");
		checkOneTerm(a, "kekasih", "kasih");
		checkOneTerm(a, "berlari", "lari");
		checkOneTerm(a, "belajar", "ajar");
		checkOneTerm(a, "bekerja", "kerja");
		checkOneTerm(a, "perjelas", "jelas");
		checkOneTerm(a, "pelajar", "ajar");
		checkOneTerm(a, "pekerja", "kerja");
		checkOneTerm(a, "tarikkan", "tarik");
		checkOneTerm(a, "ambilkan", "ambil");
		checkOneTerm(a, "mengambilkan", "ambil");
		checkOneTerm(a, "makanan", "makan");
		checkOneTerm(a, "janjian", "janji");
		checkOneTerm(a, "perjanjian", "janji");
		checkOneTerm(a, "tandai", "tanda");
		checkOneTerm(a, "dapati", "dapat");
		checkOneTerm(a, "mendapati", "dapat");
		checkOneTerm(a, "pantai", "panta");
	  }

	  /// <summary>
	  /// Some detailed analysis examples (that might not be the best) </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIRExamples() throws java.io.IOException
	  public virtual void testIRExamples()
	  {
		checkOneTerm(a, "penyalahgunaan", "salahguna");
		checkOneTerm(a, "menyalahgunakan", "salahguna");
		checkOneTerm(a, "disalahgunakan", "salahguna");

		checkOneTerm(a, "pertanggungjawaban", "tanggungjawab");
		checkOneTerm(a, "mempertanggungjawabkan", "tanggungjawab");
		checkOneTerm(a, "dipertanggungjawabkan", "tanggungjawab");

		checkOneTerm(a, "pelaksanaan", "laksana");
		checkOneTerm(a, "pelaksana", "laksana");
		checkOneTerm(a, "melaksanakan", "laksana");
		checkOneTerm(a, "dilaksanakan", "laksana");

		checkOneTerm(a, "melibatkan", "libat");
		checkOneTerm(a, "terlibat", "libat");

		checkOneTerm(a, "penculikan", "culik");
		checkOneTerm(a, "menculik", "culik");
		checkOneTerm(a, "diculik", "culik");
		checkOneTerm(a, "penculik", "culik");

		checkOneTerm(a, "perubahan", "ubah");
		checkOneTerm(a, "peledakan", "ledak");
		checkOneTerm(a, "penanganan", "tangan");
		checkOneTerm(a, "kepolisian", "polisi");
		checkOneTerm(a, "kenaikan", "naik");
		checkOneTerm(a, "bersenjata", "senjata");
		checkOneTerm(a, "penyelewengan", "seleweng");
		checkOneTerm(a, "kecelakaan", "celaka");
	  }

	  /* inflectional-only stemming */
	  internal Analyzer b = new AnalyzerAnonymousInnerClassHelper2();

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper2()
		  {
		  }

		  public override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new IndonesianStemFilter(tokenizer, false));
		  }
	  }

	  /// <summary>
	  /// Test stemming only inflectional suffixes </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInflectionalOnly() throws java.io.IOException
	  public virtual void testInflectionalOnly()
	  {
		checkOneTerm(b, "bukunya", "buku");
		checkOneTerm(b, "bukukah", "buku");
		checkOneTerm(b, "bukunyakah", "buku");
		checkOneTerm(b, "dibukukannya", "dibukukan");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testShouldntStem() throws java.io.IOException
	  public virtual void testShouldntStem()
	  {
		checkOneTerm(a, "bersenjata", "senjata");
		checkOneTerm(a, "bukukah", "buku");
		checkOneTerm(a, "gigi", "gigi");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestIndonesianStemmer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestIndonesianStemmer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new IndonesianStemFilter(tokenizer));
		  }
	  }
	}

}