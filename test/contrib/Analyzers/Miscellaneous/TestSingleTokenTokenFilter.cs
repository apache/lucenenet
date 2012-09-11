/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using NUnit.Framework;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analyzers.Miscellaneous
{
    [TestFixture]
    public class TestSingleTokenTokenFilter : LuceneTestCase
    {
        [Test]
        public void Test()
        {
            Token token = new Token();
            SingleTokenTokenStream ts = new SingleTokenTokenStream(token);
            Attribute tokenAtt = (Attribute)ts.AddAttribute<ITermAttribute>();
            Assert.True(tokenAtt is Token);
            ts.Reset();

            Assert.True(ts.IncrementToken());
            Assert.AreEqual(token, tokenAtt);
            Assert.False(ts.IncrementToken());

            token = new Token("hallo", 10, 20, "someType");
            ts.SetToken(token);
            ts.Reset();

            Assert.True(ts.IncrementToken());
            Assert.AreEqual(token, tokenAtt);
            Assert.False(ts.IncrementToken());
        }
    }
}
