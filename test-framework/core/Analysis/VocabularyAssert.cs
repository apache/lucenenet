namespace Lucene.Net.Analysis
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
	/// Utility class for doing vocabulary-based stemming tests </summary>
	public class VocabularyAssert
	{
	  /// <summary>
	  /// Run a vocabulary test against two data files. </summary>
	  public static void AssertVocabulary(Analyzer a, InputStream voc, InputStream @out)
	  {
		BufferedReader vocReader = new BufferedReader(new InputStreamReader(voc, StandardCharsets.UTF_8));
		BufferedReader outputReader = new BufferedReader(new InputStreamReader(@out, StandardCharsets.UTF_8));
		string inputWord = null;
		while ((inputWord = vocReader.readLine()) != null)
		{
		  string expectedWord = outputReader.readLine();
		  Assert.Assert.IsNotNull(expectedWord);
		  BaseTokenStreamTestCase.CheckOneTerm(a, inputWord, expectedWord);
		}
	  }

	  /// <summary>
	  /// Run a vocabulary test against one file: tab separated. </summary>
	  public static void AssertVocabulary(Analyzer a, InputStream vocOut)
	  {
		BufferedReader vocReader = new BufferedReader(new InputStreamReader(vocOut, StandardCharsets.UTF_8));
		string inputLine = null;
		while ((inputLine = vocReader.readLine()) != null)
		{
		  if (inputLine.StartsWith("#") || inputLine.Trim().Length == 0)
		  {
			continue; // comment
		  }
		  string[] words = inputLine.Split("\t", true);
		  BaseTokenStreamTestCase.CheckOneTerm(a, words[0], words[1]);
		}
	  }

	  /// <summary>
	  /// Run a vocabulary test against two data files inside a zip file </summary>
	  public static void AssertVocabulary(Analyzer a, File zipFile, string voc, string @out)
	  {
		ZipFile zip = new ZipFile(zipFile);
		InputStream v = zip.getInputStream(zip.getEntry(voc));
		InputStream o = zip.getInputStream(zip.getEntry(@out));
		AssertVocabulary(a, v, o);
		v.close();
		o.close();
		zip.close();
	  }

	  /// <summary>
	  /// Run a vocabulary test against a tab-separated data file inside a zip file </summary>
	  public static void AssertVocabulary(Analyzer a, File zipFile, string vocOut)
	  {
		ZipFile zip = new ZipFile(zipFile);
		InputStream vo = zip.getInputStream(zip.getEntry(vocOut));
		AssertVocabulary(a, vo);
		vo.close();
		zip.close();
	  }
	}

}