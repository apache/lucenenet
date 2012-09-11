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
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analysis
{
	
    [TestFixture]
	public class TestLengthFilter:BaseTokenStreamTestCase
	{
		
        [Test]
		public virtual void  TestFilter()
		{
			TokenStream stream = new WhitespaceTokenizer(new System.IO.StringReader("short toolong evenmuchlongertext a ab toolong foo"));
			LengthFilter filter = new LengthFilter(stream, 2, 6);
            ITermAttribute termAtt = filter.GetAttribute<ITermAttribute>();
			
			Assert.IsTrue(filter.IncrementToken());
			Assert.AreEqual("short", termAtt.Term);
			Assert.IsTrue(filter.IncrementToken());
			Assert.AreEqual("ab", termAtt.Term);
			Assert.IsTrue(filter.IncrementToken());
			Assert.AreEqual("foo", termAtt.Term);
			Assert.IsFalse(filter.IncrementToken());
		}
	}
}