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

using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Classification
{
    /**
     * Testcase for <see cref="KNearestNeighborClassifier"/>
     */
    public class KNearestNeighborClassifierTest : ClassificationTestBase<BytesRef>
    {
        [Test]
        public void TestBasicUsage()
        {
            // usage with default MLT min docs / term freq
            CheckCorrectClassification(new KNearestNeighborClassifier(3), POLITICS_INPUT, POLITICS_RESULT, new MockAnalyzer(Random), textFieldName, categoryFieldName);
            // usage without custom min docs / term freq for MLT
            CheckCorrectClassification(new KNearestNeighborClassifier(3, 2, 1), TECHNOLOGY_INPUT, TECHNOLOGY_RESULT, new MockAnalyzer(Random), textFieldName, categoryFieldName);
        }

        [Test]
        public void TestBasicUsageWithQuery()
        {
            CheckCorrectClassification(new KNearestNeighborClassifier(1), TECHNOLOGY_INPUT, TECHNOLOGY_RESULT, new MockAnalyzer(Random), textFieldName, categoryFieldName, new TermQuery(new Term(textFieldName, "it")));
        }

        [Test]
        public void TestPerformance()
        {
            CheckPerformance(new KNearestNeighborClassifier(100), new MockAnalyzer(Random), categoryFieldName);
        }

    }
}