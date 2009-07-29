/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
{
	
	[TestFixture]
	public class TestISOLatin1AccentFilter : LuceneTestCase
	{
		[Test]
		public virtual void  TestU()
		{
            TokenStream stream = new WhitespaceTokenizer(new System.IO.StringReader("Des mot clés À LA CHAÎNE À Á Â Ã Ä Å Æ Ç È É Ê Ë Ì Í Î Ï Ĳ Ð Ñ Ò Ó Ô Õ Ö Ø Œ Þ Ù Ú Û Ü Ý Ÿ à á â ã ä å æ ç è é ê ë ì í î ï ĳ ð ñ ò ó ô õ ö ø œ ß þ ù ú û ü ý ÿ ﬁ ﬂ"));
			ISOLatin1AccentFilter filter = new ISOLatin1AccentFilter(stream);
            Token reusableToken = new Token();
            Assert.AreEqual("Des", filter.Next(reusableToken).Term());
			Assert.AreEqual("mot", filter.Next(reusableToken).Term());
			Assert.AreEqual("cles", filter.Next(reusableToken).Term());
			Assert.AreEqual("A", filter.Next(reusableToken).Term());
			Assert.AreEqual("LA", filter.Next(reusableToken).Term());
			Assert.AreEqual("CHAINE", filter.Next(reusableToken).Term());
			Assert.AreEqual("A", filter.Next(reusableToken).Term());
			Assert.AreEqual("A", filter.Next(reusableToken).Term());
			Assert.AreEqual("A", filter.Next(reusableToken).Term());
			Assert.AreEqual("A", filter.Next(reusableToken).Term());
			Assert.AreEqual("A", filter.Next(reusableToken).Term());
			Assert.AreEqual("A", filter.Next(reusableToken).Term());
			Assert.AreEqual("AE", filter.Next(reusableToken).Term());
			Assert.AreEqual("C", filter.Next(reusableToken).Term());
			Assert.AreEqual("E", filter.Next(reusableToken).Term());
			Assert.AreEqual("E", filter.Next(reusableToken).Term());
			Assert.AreEqual("E", filter.Next(reusableToken).Term());
			Assert.AreEqual("E", filter.Next(reusableToken).Term());
			Assert.AreEqual("I", filter.Next(reusableToken).Term());
			Assert.AreEqual("I", filter.Next(reusableToken).Term());
			Assert.AreEqual("I", filter.Next(reusableToken).Term());
            Assert.AreEqual("I", filter.Next(reusableToken).Term());
            Assert.AreEqual("IJ", filter.Next(reusableToken).Term());
			Assert.AreEqual("D", filter.Next(reusableToken).Term());
			Assert.AreEqual("N", filter.Next(reusableToken).Term());
			Assert.AreEqual("O", filter.Next(reusableToken).Term());
			Assert.AreEqual("O", filter.Next(reusableToken).Term());
			Assert.AreEqual("O", filter.Next(reusableToken).Term());
			Assert.AreEqual("O", filter.Next(reusableToken).Term());
			Assert.AreEqual("O", filter.Next(reusableToken).Term());
			Assert.AreEqual("O", filter.Next(reusableToken).Term());
			Assert.AreEqual("OE", filter.Next(reusableToken).Term());
			Assert.AreEqual("TH", filter.Next(reusableToken).Term());
			Assert.AreEqual("U", filter.Next(reusableToken).Term());
			Assert.AreEqual("U", filter.Next(reusableToken).Term());
			Assert.AreEqual("U", filter.Next(reusableToken).Term());
			Assert.AreEqual("U", filter.Next(reusableToken).Term());
			Assert.AreEqual("Y", filter.Next(reusableToken).Term());
			Assert.AreEqual("Y", filter.Next(reusableToken).Term());
			Assert.AreEqual("a", filter.Next(reusableToken).Term());
			Assert.AreEqual("a", filter.Next(reusableToken).Term());
			Assert.AreEqual("a", filter.Next(reusableToken).Term());
			Assert.AreEqual("a", filter.Next(reusableToken).Term());
			Assert.AreEqual("a", filter.Next(reusableToken).Term());
			Assert.AreEqual("a", filter.Next(reusableToken).Term());
			Assert.AreEqual("ae", filter.Next(reusableToken).Term());
			Assert.AreEqual("c", filter.Next(reusableToken).Term());
			Assert.AreEqual("e", filter.Next(reusableToken).Term());
			Assert.AreEqual("e", filter.Next(reusableToken).Term());
			Assert.AreEqual("e", filter.Next(reusableToken).Term());
			Assert.AreEqual("e", filter.Next(reusableToken).Term());
			Assert.AreEqual("i", filter.Next(reusableToken).Term());
			Assert.AreEqual("i", filter.Next(reusableToken).Term());
			Assert.AreEqual("i", filter.Next(reusableToken).Term());
            Assert.AreEqual("i", filter.Next(reusableToken).Term());
            Assert.AreEqual("ij", filter.Next(reusableToken).Term());
			Assert.AreEqual("d", filter.Next(reusableToken).Term());
			Assert.AreEqual("n", filter.Next(reusableToken).Term());
			Assert.AreEqual("o", filter.Next(reusableToken).Term());
			Assert.AreEqual("o", filter.Next(reusableToken).Term());
			Assert.AreEqual("o", filter.Next(reusableToken).Term());
			Assert.AreEqual("o", filter.Next(reusableToken).Term());
			Assert.AreEqual("o", filter.Next(reusableToken).Term());
			Assert.AreEqual("o", filter.Next(reusableToken).Term());
			Assert.AreEqual("oe", filter.Next(reusableToken).Term());
			Assert.AreEqual("ss", filter.Next(reusableToken).Term());
			Assert.AreEqual("th", filter.Next(reusableToken).Term());
			Assert.AreEqual("u", filter.Next(reusableToken).Term());
			Assert.AreEqual("u", filter.Next(reusableToken).Term());
			Assert.AreEqual("u", filter.Next(reusableToken).Term());
			Assert.AreEqual("u", filter.Next(reusableToken).Term());
			Assert.AreEqual("y", filter.Next(reusableToken).Term());
            Assert.AreEqual("y", filter.Next(reusableToken).Term());
            Assert.AreEqual("fi", filter.Next(reusableToken).Term());
            Assert.AreEqual("fl", filter.Next(reusableToken).Term());
			Assert.IsNull(filter.Next(reusableToken));
		}
	}
}