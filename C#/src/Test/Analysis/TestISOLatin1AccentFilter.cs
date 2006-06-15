/*
 * Copyright 2005 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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

namespace Lucene.Net.Analysis
{
	[TestFixture]
	public class TestISOLatin1AccentFilter
	{
        [Test]
		public virtual void  TestU()
		{
			TokenStream stream = new WhitespaceTokenizer(new System.IO.StringReader("Des mot clés À LA CHAÎNE À Á Â Ã Ä Å Æ Ç È É Ê Ë Ì Í Î Ï Ð Ñ Ò Ó Ô Õ Ö Ø Œ Þ Ù Ú Û Ü Ý Ÿ à á â ã ä å æ ç è é ê ë ì í î ï ð ñ ò ó ô õ ö ø œ ß þ ù ú û ü ý ÿ"));
			ISOLatin1AccentFilter filter = new ISOLatin1AccentFilter(stream);
			Assert.AreEqual("Des", filter.Next().TermText());
			Assert.AreEqual("mot", filter.Next().TermText());
			Assert.AreEqual("cles", filter.Next().TermText());
			Assert.AreEqual("A", filter.Next().TermText());
			Assert.AreEqual("LA", filter.Next().TermText());
			Assert.AreEqual("CHAINE", filter.Next().TermText());
			Assert.AreEqual("A", filter.Next().TermText());
			Assert.AreEqual("A", filter.Next().TermText());
			Assert.AreEqual("A", filter.Next().TermText());
			Assert.AreEqual("A", filter.Next().TermText());
			Assert.AreEqual("A", filter.Next().TermText());
			Assert.AreEqual("A", filter.Next().TermText());
			Assert.AreEqual("AE", filter.Next().TermText());
			Assert.AreEqual("C", filter.Next().TermText());
			Assert.AreEqual("E", filter.Next().TermText());
			Assert.AreEqual("E", filter.Next().TermText());
			Assert.AreEqual("E", filter.Next().TermText());
			Assert.AreEqual("E", filter.Next().TermText());
			Assert.AreEqual("I", filter.Next().TermText());
			Assert.AreEqual("I", filter.Next().TermText());
			Assert.AreEqual("I", filter.Next().TermText());
			Assert.AreEqual("I", filter.Next().TermText());
			Assert.AreEqual("D", filter.Next().TermText());
			Assert.AreEqual("N", filter.Next().TermText());
			Assert.AreEqual("O", filter.Next().TermText());
			Assert.AreEqual("O", filter.Next().TermText());
			Assert.AreEqual("O", filter.Next().TermText());
			Assert.AreEqual("O", filter.Next().TermText());
			Assert.AreEqual("O", filter.Next().TermText());
			Assert.AreEqual("O", filter.Next().TermText());
			Assert.AreEqual("OE", filter.Next().TermText());
			Assert.AreEqual("TH", filter.Next().TermText());
			Assert.AreEqual("U", filter.Next().TermText());
			Assert.AreEqual("U", filter.Next().TermText());
			Assert.AreEqual("U", filter.Next().TermText());
			Assert.AreEqual("U", filter.Next().TermText());
			Assert.AreEqual("Y", filter.Next().TermText());
			Assert.AreEqual("Y", filter.Next().TermText());
			Assert.AreEqual("a", filter.Next().TermText());
			Assert.AreEqual("a", filter.Next().TermText());
			Assert.AreEqual("a", filter.Next().TermText());
			Assert.AreEqual("a", filter.Next().TermText());
			Assert.AreEqual("a", filter.Next().TermText());
			Assert.AreEqual("a", filter.Next().TermText());
			Assert.AreEqual("ae", filter.Next().TermText());
			Assert.AreEqual("c", filter.Next().TermText());
			Assert.AreEqual("e", filter.Next().TermText());
			Assert.AreEqual("e", filter.Next().TermText());
			Assert.AreEqual("e", filter.Next().TermText());
			Assert.AreEqual("e", filter.Next().TermText());
			Assert.AreEqual("i", filter.Next().TermText());
			Assert.AreEqual("i", filter.Next().TermText());
			Assert.AreEqual("i", filter.Next().TermText());
			Assert.AreEqual("i", filter.Next().TermText());
			Assert.AreEqual("d", filter.Next().TermText());
			Assert.AreEqual("n", filter.Next().TermText());
			Assert.AreEqual("o", filter.Next().TermText());
			Assert.AreEqual("o", filter.Next().TermText());
			Assert.AreEqual("o", filter.Next().TermText());
			Assert.AreEqual("o", filter.Next().TermText());
			Assert.AreEqual("o", filter.Next().TermText());
			Assert.AreEqual("o", filter.Next().TermText());
			Assert.AreEqual("oe", filter.Next().TermText());
			Assert.AreEqual("ss", filter.Next().TermText());
			Assert.AreEqual("th", filter.Next().TermText());
			Assert.AreEqual("u", filter.Next().TermText());
			Assert.AreEqual("u", filter.Next().TermText());
			Assert.AreEqual("u", filter.Next().TermText());
			Assert.AreEqual("u", filter.Next().TermText());
			Assert.AreEqual("y", filter.Next().TermText());
			Assert.AreEqual("y", filter.Next().TermText());
			Assert.IsNull(filter.Next());
		}
	}
}