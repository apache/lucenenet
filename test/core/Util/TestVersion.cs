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

namespace Lucene.Net.Util
{

	public class TestVersion : LuceneTestCase
	{

	  public virtual void Test()
	  {
		foreach (Version v in Version.values())
		{
		  Assert.IsTrue("LUCENE_CURRENT must be always onOrAfter(" + v + ")", Version.LUCENE_CURRENT.onOrAfter(v));
		}
		Assert.IsTrue(Version.LUCENE_40.onOrAfter(Version.LUCENE_31));
		Assert.IsTrue(Version.LUCENE_40.onOrAfter(Version.LUCENE_40));
		Assert.IsFalse(Version.LUCENE_30.onOrAfter(Version.LUCENE_31));
	  }

	  public virtual void TestParseLeniently()
	  {
		Assert.AreEqual(Version.LUCENE_40, Version.parseLeniently("4.0"));
		Assert.AreEqual(Version.LUCENE_40, Version.parseLeniently("LUCENE_40"));
		Assert.AreEqual(Version.LUCENE_CURRENT, Version.parseLeniently("LUCENE_CURRENT"));
	  }

	  public virtual void TestDeprecations()
	  {
		Version[] values = Version.values();
		// all but the latest version should be deprecated
		for (int i = 0; i < values.Length; i++)
		{
		  if (i + 1 == values.Length)
		  {
			assertSame("Last constant must be LUCENE_CURRENT", Version.LUCENE_CURRENT, values[i]);
		  }
		  bool dep = typeof(Version).getField(values[i].name()).isAnnotationPresent(typeof(Deprecated));
		  if (i + 2 != values.Length)
		  {
			Assert.IsTrue(values[i].name() + " should be deprecated", dep);
		  }
		  else
		  {
			Assert.IsFalse(values[i].name() + " should not be deprecated", dep);
		  }
		}
	  }

	  public virtual void TestAgainstMainVersionConstant()
	  {
		Version[] values = Version.values();
		Assert.IsTrue(values.Length >= 2);
		string mainVersionWithoutAlphaBeta = Constants.mainVersionWithoutAlphaBeta();
		Version mainVersionParsed = Version.parseLeniently(mainVersionWithoutAlphaBeta);
		assertSame("Constant one before last must be the same as the parsed LUCENE_MAIN_VERSION (without alpha/beta) constant: " + mainVersionWithoutAlphaBeta, mainVersionParsed, values[values.Length - 2]);
	  }
	}

}