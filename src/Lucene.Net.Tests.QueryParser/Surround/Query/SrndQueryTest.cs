using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.QueryParsers.Surround.Query
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

    [TestFixture]
    public class SrndQueryTest : LuceneTestCase
    {
        private void CheckEqualParsings(string s1, string s2)
        {
            string fieldName = "foo";
            BasicQueryFactory qf = new BasicQueryFactory(16);
            Search.Query lq1, lq2;
            lq1 = Parser.QueryParser.Parse(s1).MakeLuceneQueryField(fieldName, qf);
            lq2 = Parser.QueryParser.Parse(s2).MakeLuceneQueryField(fieldName, qf);
            QueryUtils.CheckEqual(lq1, lq2);
        }

        [Test]
        public void TestHashEquals()
        {
            //grab some sample queries from Test02Boolean and Test03Distance and
            //check there hashes and equals
            CheckEqualParsings("word1 w word2", " word1  w  word2 ");
            CheckEqualParsings("2N(w1,w2,w3)", " 2N(w1, w2 , w3)");
            CheckEqualParsings("abc?", " abc? ");
            CheckEqualParsings("w*rd?", " w*rd?");
        }
    }
}
