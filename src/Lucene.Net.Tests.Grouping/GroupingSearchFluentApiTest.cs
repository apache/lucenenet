using Lucene.Net.Attributes;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using NUnit.Framework;
using System.Collections;

namespace Lucene.Net.Search.Grouping
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
    /// Tests that the fluent API on <see cref="GroupingSearch"/> subclasses
    /// preserves the concrete return type through method chaining.
    /// </summary>
    [LuceneNetSpecific]
    public class GroupingSearchFluentApiTest : LuceneTestCase
    {
        [Test]
        public void TestFieldGroupingSearch_SetGroupSort_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetGroupSort(Sort.RELEVANCE);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetSortWithinGroup_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetSortWithinGroup(Sort.RELEVANCE);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetGroupDocsOffset_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetGroupDocsOffset(0);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetGroupDocsLimit_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetGroupDocsLimit(10);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetFillSortFields_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetFillSortFields(true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetIncludeScores_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetIncludeScores(true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetIncludeMaxScore_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetIncludeMaxScore(true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetCachingInMB_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetCachingInMB(4.0, true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetCaching_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetCaching(1000, true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_DisableCaching_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .DisableCaching();
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetAllGroups_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetAllGroups(true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetAllGroupHeads_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetAllGroupHeads(true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_SetInitialSize_ReturnsConcrete()
        {
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetInitialSize(256);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFieldGroupingSearch_FullChaining()
        {
            // Verify that all setters can be chained without losing the concrete type,
            // including SetInitialSize which is only on FieldGroupingSearch.
            FieldGroupingSearch result = GroupingSearch.ByField("field")
                .SetGroupSort(Sort.RELEVANCE)
                .SetSortWithinGroup(Sort.RELEVANCE)
                .SetGroupDocsOffset(0)
                .SetGroupDocsLimit(10)
                .SetFillSortFields(true)
                .SetIncludeScores(true)
                .SetIncludeMaxScore(true)
                .SetAllGroups(true)
                .SetAllGroupHeads(true)
                .SetCachingInMB(4.0, true)
                .SetInitialSize(256);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFunctionGroupingSearch_SetGroupSort_ReturnsConcrete()
        {
            FunctionGroupingSearch<MutableValueStr> result = GroupingSearch
                .ByFunction<MutableValueStr>(new BytesRefFieldSource("field"), new Hashtable())
                .SetGroupSort(Sort.RELEVANCE);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFunctionGroupingSearch_SetAllGroups_ReturnsConcrete()
        {
            FunctionGroupingSearch<MutableValueStr> result = GroupingSearch
                .ByFunction<MutableValueStr>(new BytesRefFieldSource("field"), new Hashtable())
                .SetAllGroups(true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFunctionGroupingSearch_SetCachingInMB_ReturnsConcrete()
        {
            FunctionGroupingSearch<MutableValueStr> result = GroupingSearch
                .ByFunction<MutableValueStr>(new BytesRefFieldSource("field"), new Hashtable())
                .SetCachingInMB(4.0, true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestFunctionGroupingSearch_FullChaining()
        {
            FunctionGroupingSearch<MutableValueStr> result = GroupingSearch
                .ByFunction<MutableValueStr>(new BytesRefFieldSource("field"), new Hashtable())
                .SetGroupSort(Sort.RELEVANCE)
                .SetSortWithinGroup(Sort.RELEVANCE)
                .SetGroupDocsOffset(0)
                .SetGroupDocsLimit(10)
                .SetFillSortFields(true)
                .SetIncludeScores(true)
                .SetIncludeMaxScore(true)
                .SetAllGroups(true)
                .SetAllGroupHeads(true)
                .SetCachingInMB(4.0, true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestDocBlockGroupingSearch_SetGroupSort_ReturnsConcrete()
        {
            DocBlockGroupingSearch<object> result = GroupingSearch
                .ByDocBlock<object>(null)
                .SetGroupSort(Sort.RELEVANCE);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestDocBlockGroupingSearch_SetIncludeScores_ReturnsConcrete()
        {
            DocBlockGroupingSearch<object> result = GroupingSearch
                .ByDocBlock<object>(null)
                .SetIncludeScores(true);
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestDocBlockGroupingSearch_FullChaining()
        {
            DocBlockGroupingSearch<object> result = GroupingSearch
                .ByDocBlock<object>(null)
                .SetGroupSort(Sort.RELEVANCE)
                .SetSortWithinGroup(Sort.RELEVANCE)
                .SetGroupDocsOffset(0)
                .SetGroupDocsLimit(10)
                .SetFillSortFields(true)
                .SetIncludeScores(true);
            Assert.IsNotNull(result);
        }
    }
}
