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

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Grouping
{

    /// <summary>
    /// LUCENENET: File not included in java Lucene. This file contains extra
    /// tests to test a few specific ways of using grouping.
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    public class TestGroupingExtra : LuceneTestCase
    {


        /// <summary>
        /// LUCENENET: Additional Unit Test.  Tests grouping by a StringField via the
        /// 2 pass by field name approach and using new LUCENENET specific method
        /// Uses FieldCache, not DocValues.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public void GroupingSearchByField_StringSorted_UsingDocValues_Top3Groups_Top4DocsEach()
        {
            string[,] carData = GetCarData();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            int carCount = carData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < carCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new StringField("carMake", carData[i, 0], Field.Store.YES));
                doc.Add(new SortedDocValuesField("carMake_dv", new BytesRef(carData[i, 0])));
                doc.Add(new StringField("carModel", carData[i, 1], Field.Store.YES));
                doc.Add(new SortedDocValuesField("carModel_dv", new BytesRef(carData[i, 1])));
                doc.Add(new StringField("carColor", carData[i, 2], Field.Store.YES));
                writer.AddDocument(doc);
            }
            writer.Commit();

            GroupingSearch groupingSearch = new GroupingSearch("carMake");
            groupingSearch.SetAllGroups(true);                    //true = compute all groups matching the query
            groupingSearch.SetGroupDocsLimit(4);                 //max docs returned in a group
            groupingSearch.SetGroupSort(new Sort(new SortField("carMake_dv", SortFieldType.STRING)));
            groupingSearch.SetSortWithinGroup(new Sort(new SortField("carModel_dv", SortFieldType.STRING)));
            groupingSearch.SetFillSortFields(true);
            groupingSearch.SetCachingInMB(10, cacheScores: true);

            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query matchAllQuery = new MatchAllDocsQuery();
            ITopGroups<BytesRef> topGroups = groupingSearch.SearchByField(searcher, matchAllQuery, groupOffset: 0, groupLimit: 3);


            int? totalGroupCount = topGroups.TotalGroupCount;               //null if not computed
            int totalGroupedHitCount = topGroups.TotalGroupedHitCount;

            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<BytesRef> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    sb.AppendLine($"Group: {groupDocs.GroupValue.Utf8ToString()}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("carMake").GetStringValue()} {doc.GetField("carModel").GetStringValue()} {doc.GetField("carColor").GetStringValue()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: Audi\r\nAudi A3 Orange\r\nAudi A3 Green\r\nAudi A3 Blue\r\nAudi S4 Yellow\r\n\r\nGroup: Bently\r\nBently Arnage Grey\r\nBently Arnage Blue\r\nBently Azure Green\r\nBently Azure Blue\r\n\r\nGroup: Ford\r\nFord Aspire Yellow\r\nFord Aspire Blue\r\nFord Bronco Green\r\nFord Bronco Orange\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             
                Group: Audi
                Audi A3 Orange
                Audi A3 Green
                Audi A3 Blue
                Audi S4 Yellow

                Group: Bently
                Bently Arnage Grey
                Bently Arnage Blue
                Bently Azure Green
                Bently Azure Blue

                Group: Ford
                Ford Aspire Yellow
                Ford Aspire Blue
                Ford Bronco Green
                Ford Bronco Orange

            */
        }

        /// <summary>
        /// LUCENENET: Additional Unit Test.  Tests grouping by an Int32 via the
        /// 2 pass by function/ValueSource/MutableValue approach. Uses FieldCache, not DocValues.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void GroupingSearchByFunction_Int32Sorted_UsingFieldCache_Top10Groups_Top10DocsEach()
        {
            int[,] numericData = GetNumbers();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);


            //Normally we can not group on a Int32Field when using fieldCache because the Int32Field is stored
            //as a 8 term trie structure by default.  But by specifying int.MaxValue as the NumericPrecisionStep
            //we force the inverted index to store the value as a single term. This allows us to use it for
            //grouping via FieldCache (although it's no longer good for range queries as they will be slow if
            //the range is large). 

            var int32OneTerm = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = true,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = Documents.NumericType.INT32,
                NumericPrecisionStep = int.MaxValue,             //Ensures a single term is generated not a trie
                IsStored = true
            };
            int32OneTerm.Freeze();

            int rowCount = numericData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < rowCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new Int32Field("major", numericData[i, 0], int32OneTerm));
                doc.Add(new Int32Field("minor", numericData[i, 1], int32OneTerm));
                doc.Add(new StoredField("rev", numericData[i, 2]));
                writer.AddDocument(doc);
            }
            writer.Commit();

            ValueSource vs = new BytesRefFieldSource("major");
            GroupingSearch groupingSearch = new GroupingSearch(vs, new Hashtable());
            groupingSearch.SetAllGroups(true);                      //true = compute all groups matching the query
            groupingSearch.SetGroupDocsLimit(10);                   //max docs returned in a group
            groupingSearch.SetGroupSort(new Sort(new SortField("major", SortFieldType.INT32)));
            groupingSearch.SetSortWithinGroup(new Sort(new SortField("minor", SortFieldType.INT32)));

            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query matchAllQuery = new MatchAllDocsQuery();
            ITopGroups<MutableValueStr> topGroups = groupingSearch.SearchByFunction<MutableValueStr>(searcher, matchAllQuery, groupOffset: 0, groupLimit: 10);

            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<MutableValueStr> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    MutableValueStr mutableValueStr = groupDocs.GroupValue;
                    int major = NumericUtils.PrefixCodedToInt32(mutableValueStr.Value);
                    sb.AppendLine($"Group: {major}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("major").GetInt32Value()} {doc.GetField("minor").GetInt32Value()} {doc.GetField("rev").GetInt32Value()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: 1000\r\n1000 1102 21\r\n1000 1123 45\r\n\r\nGroup: 2000\r\n2000 2222 7\r\n2000 2888 88\r\n\r\nGroup: 3000\r\n3000 3123 11\r\n3000 3222 37\r\n3000 3993 9\r\n\r\nGroup: 4000\r\n4000 4001 88\r\n4000 4011 10\r\n\r\nGroup: 8000\r\n8000 8123 28\r\n8000 8888 8\r\n8000 8998 92\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             *  
                Group: 1000
                1000 1102 21
                1000 1123 45

                Group: 2000
                2000 2222 7
                2000 2888 88

                Group: 3000
                3000 3123 11
                3000 3222 37
                3000 3993 9

                Group: 4000
                4000 4001 88
                4000 4011 10

                Group: 8000
                8000 8123 28
                8000 8888 8
                8000 8998 92
            */
        }


        /// <summary>
        /// LUCENENET: Additional Unit Test.  Tests grouping by a StringField via the
        /// 2 pass by field name approach. Uses FieldCache, not DocValues.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public void GroupingSearch_ViaField_StringSorted_UsingFieldCache_Top3Groups_Top4DocsEach()
        {
            string[,] carData = GetCarData();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            int carCount = carData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < carCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new StringField("carMake", carData[i, 0], Field.Store.YES));
                doc.Add(new StringField("carModel", carData[i, 1], Field.Store.YES));
                doc.Add(new StringField("carColor", carData[i, 2], Field.Store.YES));
                writer.AddDocument(doc);
            }
            writer.Commit();

            GroupingSearch groupingSearch = new GroupingSearch("carMake");
            groupingSearch.SetAllGroups(true);                      //true = compute all groups matching the query
            groupingSearch.SetGroupDocsLimit(4);                   //max docs returned in a group
            groupingSearch.SetGroupSort(new Sort(new SortField("carMake", SortFieldType.STRING)));
            groupingSearch.SetSortWithinGroup(new Sort(new SortField("carModel", SortFieldType.STRING)));
            groupingSearch.SetFillSortFields(true);
            groupingSearch.SetCachingInMB(10, cacheScores: true);

            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query matchAllQuery = new MatchAllDocsQuery();
            ITopGroups<object> topGroups = groupingSearch.Search(searcher, matchAllQuery, groupOffset: 0, groupLimit: 3);


            int? totalGroupCount = topGroups.TotalGroupCount;               //null if not computed
            int totalGroupedHitCount = topGroups.TotalGroupedHitCount;

            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<BytesRef> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    sb.AppendLine($"Group: {groupDocs.GroupValue.Utf8ToString()}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("carMake").GetStringValue()} {doc.GetField("carModel").GetStringValue()} {doc.GetField("carColor").GetStringValue()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: Audi\r\nAudi A3 Orange\r\nAudi A3 Green\r\nAudi A3 Blue\r\nAudi S4 Yellow\r\n\r\nGroup: Bently\r\nBently Arnage Grey\r\nBently Arnage Blue\r\nBently Azure Green\r\nBently Azure Blue\r\n\r\nGroup: Ford\r\nFord Aspire Yellow\r\nFord Aspire Blue\r\nFord Bronco Green\r\nFord Bronco Orange\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             
                Group: Audi
                Audi A3 Orange
                Audi A3 Green
                Audi A3 Blue
                Audi S4 Yellow

                Group: Bently
                Bently Arnage Grey
                Bently Arnage Blue
                Bently Azure Green
                Bently Azure Blue

                Group: Ford
                Ford Aspire Yellow
                Ford Aspire Blue
                Ford Bronco Green
                Ford Bronco Orange
            */


        }

        /// <summary>
        /// LUCENENET: Additional Unit Test.  Tests grouping by a StringField via the
        /// 2 pass by field name approach. Uses FieldCache, not DocValues.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public void GroupingSearch_ViaField_StringSorted_UsingDocValues_Top3Groups_Top4DocsEach()
        {
            string[,] carData = GetCarData();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            int carCount = carData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < carCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new StringField("carMake", carData[i, 0], Field.Store.YES));
                doc.Add(new SortedDocValuesField("carMake_dv", new BytesRef(carData[i, 0])));
                doc.Add(new StringField("carModel", carData[i, 1], Field.Store.YES));
                doc.Add(new SortedDocValuesField("carModel_dv", new BytesRef(carData[i, 1])));
                doc.Add(new StringField("carColor", carData[i, 2], Field.Store.YES));
                writer.AddDocument(doc);
            }
            writer.Commit();

            GroupingSearch groupingSearch = new GroupingSearch("carMake");
            groupingSearch.SetAllGroups(true);                    //true = compute all groups matching the query
            groupingSearch.SetGroupDocsLimit(4);                 //max docs returned in a group
            groupingSearch.SetGroupSort(new Sort(new SortField("carMake_dv", SortFieldType.STRING)));
            groupingSearch.SetSortWithinGroup(new Sort(new SortField("carModel_dv", SortFieldType.STRING)));
            groupingSearch.SetFillSortFields(true);
            groupingSearch.SetCachingInMB(10, cacheScores: true);

            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query matchAllQuery = new MatchAllDocsQuery();
            ITopGroups<object> topGroups = groupingSearch.Search(searcher, matchAllQuery, groupOffset: 0, groupLimit: 3);


            int? totalGroupCount = topGroups.TotalGroupCount;               //null if not computed
            int totalGroupedHitCount = topGroups.TotalGroupedHitCount;

            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<BytesRef> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    sb.AppendLine($"Group: {groupDocs.GroupValue.Utf8ToString()}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("carMake").GetStringValue()} {doc.GetField("carModel").GetStringValue()} {doc.GetField("carColor").GetStringValue()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: Audi\r\nAudi A3 Orange\r\nAudi A3 Green\r\nAudi A3 Blue\r\nAudi S4 Yellow\r\n\r\nGroup: Bently\r\nBently Arnage Grey\r\nBently Arnage Blue\r\nBently Azure Green\r\nBently Azure Blue\r\n\r\nGroup: Ford\r\nFord Aspire Yellow\r\nFord Aspire Blue\r\nFord Bronco Green\r\nFord Bronco Orange\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             
                Group: Audi
                Audi A3 Orange
                Audi A3 Green
                Audi A3 Blue
                Audi S4 Yellow

                Group: Bently
                Bently Arnage Grey
                Bently Arnage Blue
                Bently Azure Green
                Bently Azure Blue

                Group: Ford
                Ford Aspire Yellow
                Ford Aspire Blue
                Ford Bronco Green
                Ford Bronco Orange

            */
        }


        /// <summary>
        /// LUCENENET: Additional Unit Test.  Tests grouping by an Int32 via the
        /// 2 pass by field name approach. Uses FieldCache, not DocValues.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void GroupingSearch_ViaField_Int32Sorted_UsingFieldCache_Top10Groups_Top10DocsEach()
        {
            int[,] numericData = GetNumbers();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            //Normally we can not group on a Int32Field when using fieldCache because the Int32Field is stored
            //as a 8 term trie structure by default.  But by specifying int.MaxValue as the NumericPrecisionStep
            //we force the inverted index to store the value as a single term. This allows us to use it for
            //grouping via FieldCache (although it's no longer good for range queries as they will be slow if
            //the range is large). 

            var int32OneTerm = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = true,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = Documents.NumericType.INT32,
                NumericPrecisionStep = int.MaxValue,             //Ensures a single term is generated not a trie
                IsStored = true
            };
            int32OneTerm.Freeze();

            int rowCount = numericData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < rowCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new Int32Field("major", numericData[i, 0], int32OneTerm));
                doc.Add(new Int32Field("minor", numericData[i, 1], int32OneTerm));
                doc.Add(new StoredField("rev", numericData[i, 2]));
                writer.AddDocument(doc);
            }
            writer.Commit();

            GroupingSearch groupingSearch = new GroupingSearch("major");
            groupingSearch.SetAllGroups(true);                      //true = compute all groups matching the query
            groupingSearch.SetGroupDocsLimit(10);                   //max docs returned in a group
            groupingSearch.SetGroupSort(new Sort(new SortField("major", SortFieldType.INT32)));
            groupingSearch.SetSortWithinGroup(new Sort(new SortField("minor", SortFieldType.INT32)));

            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query matchAllQuery = new MatchAllDocsQuery();
            ITopGroups<BytesRef> topGroups = groupingSearch.Search<BytesRef>(searcher, matchAllQuery, groupOffset: 0, groupLimit: 10);

            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<BytesRef> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    int major = NumericUtils.PrefixCodedToInt32(groupDocs.GroupValue);
                    sb.AppendLine($"Group: {major}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("major").GetInt32Value()} {doc.GetField("minor").GetInt32Value()} {doc.GetField("rev").GetInt32Value()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: 1000\r\n1000 1102 21\r\n1000 1123 45\r\n\r\nGroup: 2000\r\n2000 2222 7\r\n2000 2888 88\r\n\r\nGroup: 3000\r\n3000 3123 11\r\n3000 3222 37\r\n3000 3993 9\r\n\r\nGroup: 4000\r\n4000 4001 88\r\n4000 4011 10\r\n\r\nGroup: 8000\r\n8000 8123 28\r\n8000 8888 8\r\n8000 8998 92\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:

                Group: 1000
                1000 1102 21
                1000 1123 45

                Group: 2000
                2000 2222 7
                2000 2888 88

                Group: 3000
                3000 3123 11
                3000 3222 37
                3000 3993 9

                Group: 4000
                4000 4001 88
                4000 4011 10

                Group: 8000
                8000 8123 28
                8000 8888 8
                8000 8998 92
            */
        }


        /// <summary>
        /// LUCENENET: Additional Unit Test.  Tests grouping by an Int32 via the
        /// 2 pass by function/ValueSource/MutableValue approach. Uses FieldCache, not DocValues.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void GroupingSearch_ViaFunction_Int32Sorted_UsingFieldCache_Top10Groups_Top10DocsEach()
        {
            int[,] numericData = GetNumbers();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);


            //Normally we can not group on a Int32Field when using fieldCache because the Int32Field is stored
            //as a 8 term trie structure by default.  But by specifying int.MaxValue as the NumericPrecisionStep
            //we force the inverted index to store the value as a single term. This allows us to use it for
            //grouping via FieldCache (although it's no longer good for range queries as they will be slow if
            //the range is large). 

            var int32OneTerm = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = true,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = Documents.NumericType.INT32,
                NumericPrecisionStep = int.MaxValue,             //Ensures a single term is generated not a trie
                IsStored = true
            };
            int32OneTerm.Freeze();

            int rowCount = numericData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < rowCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new Int32Field("major", numericData[i, 0], int32OneTerm));
                doc.Add(new Int32Field("minor", numericData[i, 1], int32OneTerm));
                doc.Add(new StoredField("rev", numericData[i, 2]));
                writer.AddDocument(doc);
            }
            writer.Commit();

            ValueSource vs = new BytesRefFieldSource("major");
            GroupingSearch groupingSearch = new GroupingSearch(vs, new Hashtable());
            groupingSearch.SetAllGroups(true);                      //true = compute all groups matching the query
            groupingSearch.SetGroupDocsLimit(10);                   //max docs returned in a group
            groupingSearch.SetGroupSort(new Sort(new SortField("major", SortFieldType.INT32)));
            groupingSearch.SetSortWithinGroup(new Sort(new SortField("minor", SortFieldType.INT32)));

            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query matchAllQuery = new MatchAllDocsQuery();
            ITopGroups<MutableValue> topGroups = groupingSearch.Search<MutableValue>(searcher, matchAllQuery, groupOffset: 0, groupLimit: 10);

            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<MutableValue> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    BytesRef bytesRef = ((MutableValueStr)groupDocs.GroupValue).Value;
                    int major = NumericUtils.PrefixCodedToInt32(bytesRef);
                    sb.AppendLine($"Group: {major}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("major").GetInt32Value()} {doc.GetField("minor").GetInt32Value()} {doc.GetField("rev").GetInt32Value()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: 1000\r\n1000 1102 21\r\n1000 1123 45\r\n\r\nGroup: 2000\r\n2000 2222 7\r\n2000 2888 88\r\n\r\nGroup: 3000\r\n3000 3123 11\r\n3000 3222 37\r\n3000 3993 9\r\n\r\nGroup: 4000\r\n4000 4001 88\r\n4000 4011 10\r\n\r\nGroup: 8000\r\n8000 8123 28\r\n8000 8888 8\r\n8000 8998 92\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             *  
                Group: 1000
                1000 1102 21
                1000 1123 45

                Group: 2000
                2000 2222 7
                2000 2888 88

                Group: 3000
                3000 3123 11
                3000 3222 37
                3000 3993 9

                Group: 4000
                4000 4001 88
                4000 4011 10

                Group: 8000
                8000 8123 28
                8000 8888 8
                8000 8998 92
            */
        }

        /// <summary>
        /// LUCENENET: Additional Unit Test.  Tests grouping by an Int32 via the
        /// 2 pass by function/ValueSource/MutableValue approach. Uses DocValues, not FieldCache.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void GroupingSearchByFunction_Int32Sorted_UsingDocValues_Top10Groups_Top10DocsEach()
        {
            int[,] numericData = GetNumbers();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);


            int rowCount = numericData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < rowCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new Int32Field("major", numericData[i, 0], Field.Store.YES));
                doc.Add(new NumericDocValuesField("major_dv", numericData[i, 0]));
                doc.Add(new Int32Field("minor", numericData[i, 1], Field.Store.YES));
                doc.Add(new NumericDocValuesField("minor_dv", numericData[i, 1]));
                doc.Add(new StoredField("rev", numericData[i, 2]));
                writer.AddDocument(doc);
            }
            writer.Commit();

            ValueSource vs = new Int32FieldSource("major");
            GroupingSearch groupingSearch = new GroupingSearch(vs, new Hashtable());
            groupingSearch.SetAllGroups(true);                      //true = compute all groups matching the query
            groupingSearch.SetGroupDocsLimit(10);                   //max docs returned in a group
            groupingSearch.SetGroupSort(new Sort(new SortField("major", SortFieldType.INT32)));
            groupingSearch.SetSortWithinGroup(new Sort(new SortField("minor", SortFieldType.INT32)));

            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query matchAllQuery = new MatchAllDocsQuery();

            ITopGroups<MutableValueInt32> topGroups = groupingSearch.SearchByFunction<MutableValueInt32>(searcher, matchAllQuery, groupOffset: 0, groupLimit: 10);

            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<MutableValueInt32> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    int major = groupDocs.GroupValue.Value;
                    sb.AppendLine($"Group: {major}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("major").GetInt32Value()} {doc.GetField("minor").GetInt32Value()} {doc.GetField("rev").GetInt32Value()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: 1000\r\n1000 1102 21\r\n1000 1123 45\r\n\r\nGroup: 2000\r\n2000 2222 7\r\n2000 2888 88\r\n\r\nGroup: 3000\r\n3000 3123 11\r\n3000 3222 37\r\n3000 3993 9\r\n\r\nGroup: 4000\r\n4000 4001 88\r\n4000 4011 10\r\n\r\nGroup: 8000\r\n8000 8123 28\r\n8000 8888 8\r\n8000 8998 92\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             *  
                Group: 1000
                1000 1102 21
                1000 1123 45

                Group: 2000
                2000 2222 7
                2000 2888 88

                Group: 3000
                3000 3123 11
                3000 3222 37
                3000 3993 9

                Group: 4000
                4000 4001 88
                4000 4011 10

                Group: 8000
                8000 8123 28
                8000 8888 8
                8000 8998 92
            */
        }

        /// <summary>
        /// LUCENENET: Additional Unit Test.  Tests grouping by an Int32 via the
        /// 2 pass by function/ValueSource/MutableValue approach. Uses DocValues, not FieldCache.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void GroupingSearch_ViaFunction_Int32Sorted_UsingDocValues_Top4Groups_Top2DocsEach()
        {
            int[,] numericData = GetNumbers();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            int rowCount = numericData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < rowCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new Int32Field("major", numericData[i, 0], Field.Store.YES));
                doc.Add(new NumericDocValuesField("major_dv", numericData[i, 0]));
                doc.Add(new Int32Field("minor", numericData[i, 1], Field.Store.YES));
                doc.Add(new NumericDocValuesField("minor_dv", numericData[i, 1]));
                doc.Add(new StoredField("rev", numericData[i, 2]));
                writer.AddDocument(doc);
            }
            writer.Commit();

            ValueSource vs = new Int32FieldSource("major");
            GroupingSearch groupingSearch = new GroupingSearch(vs, new Hashtable());
            groupingSearch.SetAllGroups(true);                      //true = compute all groups matching the query
            groupingSearch.SetGroupDocsLimit(2);                   //max docs returned in a group
            groupingSearch.SetGroupSort(new Sort(new SortField("major", SortFieldType.INT32)));
            groupingSearch.SetSortWithinGroup(new Sort(new SortField("minor", SortFieldType.INT32)));

            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query matchAllQuery = new MatchAllDocsQuery();

            ITopGroups<MutableValue> topGroups = groupingSearch.Search<MutableValue>(searcher, matchAllQuery, groupOffset: 0, groupLimit: 4);

            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<MutableValue> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    int major = ((MutableValueInt32)groupDocs.GroupValue).Value;
                    sb.AppendLine($"Group: {major}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("major").GetInt32Value()} {doc.GetField("minor").GetInt32Value()} {doc.GetField("rev").GetInt32Value()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: 1000\r\n1000 1102 21\r\n1000 1123 45\r\n\r\nGroup: 2000\r\n2000 2222 7\r\n2000 2888 88\r\n\r\nGroup: 3000\r\n3000 3123 11\r\n3000 3222 37\r\n\r\nGroup: 4000\r\n4000 4001 88\r\n4000 4011 10\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             *  
                Group: 1000
                1000 1102 21
                1000 1123 45

                Group: 2000
                2000 2222 7
                2000 2888 88

                Group: 3000
                3000 3123 11
                3000 3222 37

                Group: 4000
                4000 4001 88
                4000 4011 10
            */
        }


        /// <summary>
        /// LUCENENET: Additional Unit Test.  Low Level test that does not use GroupingSearch
        /// Class. Tests grouping by an StringField via the
        /// 2 pass by field name approach. Uses DocValues, not FieldCache.
        /// Demonstrates grouping returning ITopGroups<BytesRef> 
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void Group_LowLevelNoCaching_ViaField_StringStored_UsingDocValues_Top3Groups_Top4DocsEach()
        {
            string[,] carData = GetCarData();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            int carCount = carData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < carCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new StringField("carMake", carData[i, 0], Field.Store.YES));
                doc.Add(new SortedDocValuesField("carMake_dv", new BytesRef(carData[i, 0])));
                doc.Add(new StringField("carModel", carData[i, 1], Field.Store.YES));
                doc.Add(new SortedDocValuesField("carModel_dv", new BytesRef(carData[i, 1])));
                doc.Add(new StringField("carColor", carData[i, 2], Field.Store.YES));

                writer.AddDocument(doc);
            }
            writer.Commit();


            //Begin work to do grouping search manually without use of GroupingSearch class.
            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query query = new MatchAllDocsQuery();
            Filter filter = null;

            int groupOffset = 0;            //The number of groups to skip
            int groupLimit = 3;             //The number of groups to return from the specified group offset
            int groupDocsLimit = 4;         //max number of docs in each group
            int groupDocsOffset = 0;        //within group offset
            string groupField = "carMake_dv";
            ValueSource groupByValueSource = new Int32FieldSource(groupField);
            Hashtable valueSourceContext = new Hashtable();
            Sort groupSort = new Sort(new SortField(groupField, SortFieldType.STRING));
            Sort sortWithinGroup = new Sort(new SortField("carModel_dv", SortFieldType.STRING));
            bool fillSortFields = false;               //Whether to also fill the sort fields per returned group and groups docs.

            int topN = groupOffset + groupLimit;
            int topNInsideGroup = groupDocsOffset + groupDocsLimit;

            //First Pass
            TermFirstPassGroupingCollector firstPassCollector = new TermFirstPassGroupingCollector(groupField, groupSort, topN);
            searcher.Search(query, filter, firstPassCollector);
            IEnumerable<ISearchGroup<BytesRef>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);

            //Second Pass
            TermSecondPassGroupingCollector secondPassCollector = new TermSecondPassGroupingCollector(groupField, topSearchGroups,
                groupSort, sortWithinGroup, topNInsideGroup, getScores: true, getMaxScores: true, fillSortFields);
            searcher.Search(query, filter, secondPassCollector);
            ITopGroups<BytesRef> topGroups = secondPassCollector.GetTopGroups(groupDocsOffset);

            //Validate topGroups are as expected
            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<BytesRef> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    sb.AppendLine($"Group: {groupDocs.GroupValue.Utf8ToString()}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("carMake").GetStringValue()} {doc.GetField("carModel").GetStringValue()} {doc.GetField("carColor").GetStringValue()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: Audi\r\nAudi A3 Orange\r\nAudi A3 Green\r\nAudi A3 Blue\r\nAudi S4 Yellow\r\n\r\nGroup: Bently\r\nBently Arnage Grey\r\nBently Arnage Blue\r\nBently Azure Green\r\nBently Azure Blue\r\n\r\nGroup: Ford\r\nFord Aspire Yellow\r\nFord Aspire Blue\r\nFord Bronco Green\r\nFord Bronco Orange\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             
                Group: Audi
                Audi A3 Orange
                Audi A3 Green
                Audi A3 Blue
                Audi S4 Yellow

                Group: Bently
                Bently Arnage Grey
                Bently Arnage Blue
                Bently Azure Green
                Bently Azure Blue

                Group: Ford
                Ford Aspire Yellow
                Ford Aspire Blue
                Ford Bronco Green
                Ford Bronco Orange

            */

        }

        /// <summary>
        /// LUCENENET: Additional Unit Test.  Low Level test that does not use GroupingSearch
        /// Class. Tests grouping by an Int32 via the
        /// 2 pass by field name approach. Uses FieldCache, not DocValues.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void Group_LowLevelNoCaching_ViaField_Int32Sorted_UsingFieldCache_Top4Groups_Top2DocsEach()
        {
            int[,] numericData = GetNumbers();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            //Normally we can not group on a Int32Field because it's stored as a 8 term trie structure
            //by default.  But by specifying int.MaxValue as the NumericPrecisionStep we force the inverted
            //index to store the value as a single term. This allows us to use it for grouping (although
            //it's no longer good for range queries as they will be slow if the range is large). 

            var int32OneTerm = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = true,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = Documents.NumericType.INT32,
                NumericPrecisionStep = int.MaxValue,             //Ensures a single term is generated not a trie
                IsStored = true
            };
            int32OneTerm.Freeze();

            int rowCount = numericData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < rowCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new Int32Field("major", numericData[i, 0], int32OneTerm));
                doc.Add(new Int32Field("minor", numericData[i, 1], int32OneTerm));
                doc.Add(new StoredField("rev", numericData[i, 2]));
                writer.AddDocument(doc);
            }
            writer.Commit();


            //Begin work to do grouping search manually without use of GroupingSearch class.
            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query query = new MatchAllDocsQuery();
            Filter filter = null;

            int groupOffset = 0;            //The number of groups to skip
            int groupLimit = 4;            //The number of groups to return from the specified group offset
            int groupDocsLimit = 2;        //max number of docs in each group
            int groupDocsOffset = 0;        //within group offset
            string groupField = "major";
            Sort groupSort = new Sort(new SortField(groupField, SortFieldType.INT32));
            Sort sortWithinGroup = new Sort(new SortField("minor", SortFieldType.INT32));
            bool fillSortFields = false;               //Whether to also fill the sort fields per returned group and groups docs.

            int topN = groupOffset + groupLimit;
            int topNInsideGroup = groupDocsOffset + groupDocsLimit;

            //First Pass
            TermFirstPassGroupingCollector firstPassCollector = new TermFirstPassGroupingCollector(groupField, groupSort, topN);
            searcher.Search(query, filter, firstPassCollector);
            IEnumerable<ISearchGroup<BytesRef>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);

            //Second Pass
            TermSecondPassGroupingCollector secondPassCollector = new TermSecondPassGroupingCollector(groupField, topSearchGroups,
                groupSort, sortWithinGroup, topNInsideGroup, getScores: true, getMaxScores: true, fillSortFields);
            searcher.Search(query, filter, secondPassCollector);
            ITopGroups<BytesRef> topGroups = secondPassCollector.GetTopGroups(groupDocsOffset);

            //Validate topGroups are as expected
            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<BytesRef> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    int major = NumericUtils.PrefixCodedToInt32(groupDocs.GroupValue);
                    sb.AppendLine($"Group: {major}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("major").GetInt32Value()} {doc.GetField("minor").GetInt32Value()} {doc.GetField("rev").GetInt32Value()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: 1000\r\n1000 1102 21\r\n1000 1123 45\r\n\r\nGroup: 2000\r\n2000 2222 7\r\n2000 2888 88\r\n\r\nGroup: 3000\r\n3000 3123 11\r\n3000 3222 37\r\n\r\nGroup: 4000\r\n4000 4001 88\r\n4000 4011 10\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             *  
                Group: 1000
                1000 1102 21
                1000 1123 45

                Group: 2000
                2000 2222 7
                2000 2888 88

                Group: 3000
                3000 3123 11
                3000 3222 37

                Group: 4000
                4000 4001 88
                4000 4011 10
            */
        }


        /// <summary>
        /// LUCENENET: Additional Unit Test.  Low Level test that does not use GroupingSearch
        /// Class. Tests grouping by an Int32 via the
        /// 2 pass by fuction/valueSource approach. Uses FieldCache, not DocValues.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void Group_LowLevelNoCaching_ViaFunction_Int32Sorted_UsingFieldCache_Top4Groups_Top2DocsEach()
        {
            int[,] numericData = GetNumbers();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            //Normally we can not group on a Int32Field because it's stored as a 8 term trie structure
            //by default.  But by specifying int.MaxValue as the NumericPrecisionStep we force the inverted
            //index to store the value as a single term. This allows us to use it for grouping (although
            //it's no longer good for range queries as they will be slow if the range is large). 

            var int32OneTerm = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = true,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = Documents.NumericType.INT32,
                NumericPrecisionStep = int.MaxValue,             //Ensures a single term is generated not a trie
                IsStored = true
            };
            int32OneTerm.Freeze();

            int rowCount = numericData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < rowCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new Int32Field("major", numericData[i, 0], int32OneTerm));
                doc.Add(new Int32Field("minor", numericData[i, 1], int32OneTerm));
                doc.Add(new StoredField("rev", numericData[i, 2]));
                writer.AddDocument(doc);
            }
            writer.Commit();


            //Begin work to do grouping search manually without use of GroupingSearch class.
            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query query = new MatchAllDocsQuery();
            Filter filter = null;

            int groupOffset = 0;            //The number of groups to skip
            int groupLimit = 4;            //The number of groups to return from the specified group offset
            int groupDocsLimit = 2;        //max number of docs in each group
            int groupDocsOffset = 0;        //within group offset
            string groupField = "major";
            ValueSource groupByValueSource = new BytesRefFieldSource(groupField);
            Hashtable valueSourceContext = new Hashtable();
            Sort groupSort = new Sort(new SortField(groupField, SortFieldType.INT32));
            Sort sortWithinGroup = new Sort(new SortField("minor", SortFieldType.INT32));
            bool fillSortFields = false;               //Whether to also fill the sort fields per returned group and groups docs.

            int topN = groupOffset + groupLimit;
            int topNInsideGroup = groupDocsOffset + groupDocsLimit;

            //First Pass
            FunctionFirstPassGroupingCollector<MutableValueStr> firstPassCollector =
                                            new FunctionFirstPassGroupingCollector<MutableValueStr>(groupByValueSource, valueSourceContext, groupSort, topN);
            searcher.Search(query, filter, firstPassCollector);
            IEnumerable<ISearchGroup<MutableValueStr>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);

            //Second Pass
            FunctionSecondPassGroupingCollector<MutableValueStr> secondPassCollector =
                                            new FunctionSecondPassGroupingCollector<MutableValueStr>(topSearchGroups, groupSort, sortWithinGroup,
                                                    topNInsideGroup, getScores: true, getMaxScores: true, fillSortFields, groupByValueSource, valueSourceContext);
            searcher.Search(query, filter, secondPassCollector);
            ITopGroups<MutableValue> topGroups = secondPassCollector.GetTopGroups(groupDocsOffset);

            //Validate topGroups are as expected
            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<MutableValueStr> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    int major = NumericUtils.PrefixCodedToInt32(groupDocs.GroupValue.Value);
                    sb.AppendLine($"Group: {major}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("major").GetInt32Value()} {doc.GetField("minor").GetInt32Value()} {doc.GetField("rev").GetInt32Value()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: 1000\r\n1000 1102 21\r\n1000 1123 45\r\n\r\nGroup: 2000\r\n2000 2222 7\r\n2000 2888 88\r\n\r\nGroup: 3000\r\n3000 3123 11\r\n3000 3222 37\r\n\r\nGroup: 4000\r\n4000 4001 88\r\n4000 4011 10\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             *  
                Group: 1000
                1000 1102 21
                1000 1123 45

                Group: 2000
                2000 2222 7
                2000 2888 88

                Group: 3000
                3000 3123 11
                3000 3222 37

                Group: 4000
                4000 4001 88
                4000 4011 10
            */
        }


        /// <summary>
        /// LUCENENET: Additional Unit Test.  Low Level test that does not use GroupingSearch
        /// Class. Tests grouping by an Int32 via the
        /// 2 pass by fuction/valueSource approach. Uses DocValues, not FieldCache.
        /// Demonstrates grouping returning ITopGroups<MutableValueInt32> 
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void Group_LowLevelNoCaching_ViaFunction_Int32Sorted_UsingDocValues_Top4Groups_Top2DocsEach()
        {
            int[,] numericData = GetNumbers();

            Directory indexDir = NewDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);

            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            int rowCount = numericData.GetLength(0);
            Document doc = new Document();
            for (int i = 0; i < rowCount; i++)
            {
                doc.Fields.Clear();
                doc.Add(new Int32Field("major", numericData[i, 0], Field.Store.YES));
                doc.Add(new NumericDocValuesField("major_dv", numericData[i, 0]));
                doc.Add(new Int32Field("minor", numericData[i, 1], Field.Store.YES));
                doc.Add(new NumericDocValuesField("minor_dv", numericData[i, 1]));
                doc.Add(new StoredField("rev", numericData[i, 2]));
                writer.AddDocument(doc);
            }
            writer.Commit();


            //Begin work to do grouping search manually without use of GroupingSearch class.
            IndexReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            Query query = new MatchAllDocsQuery();
            Filter filter = null;

            int groupOffset = 0;            //The number of groups to skip
            int groupLimit = 4;            //The number of groups to return from the specified group offset
            int groupDocsLimit = 2;        //max number of docs in each group
            int groupDocsOffset = 0;        //within group offset
            string groupField = "major_dv";
            ValueSource groupByValueSource = new Int32FieldSource(groupField);
            Hashtable valueSourceContext = new Hashtable();
            Sort groupSort = new Sort(new SortField(groupField, SortFieldType.INT32));
            Sort sortWithinGroup = new Sort(new SortField("minor_dv", SortFieldType.INT32));
            bool fillSortFields = false;               //Whether to also fill the sort fields per returned group and groups docs.

            int topN = groupOffset + groupLimit;
            int topNInsideGroup = groupDocsOffset + groupDocsLimit;

            //First Pass
            FunctionFirstPassGroupingCollector<MutableValueInt32> firstPassCollector =
                                            new FunctionFirstPassGroupingCollector<MutableValueInt32>(groupByValueSource, valueSourceContext, groupSort, topN);
            searcher.Search(query, filter, firstPassCollector);
            IEnumerable<ISearchGroup<MutableValueInt32>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);

            //Second Pass
            FunctionSecondPassGroupingCollector<MutableValueInt32> secondPassCollector =
                                            new FunctionSecondPassGroupingCollector<MutableValueInt32>(topSearchGroups, groupSort, sortWithinGroup,
                                                    topNInsideGroup, getScores: true, getMaxScores: true, fillSortFields, groupByValueSource, valueSourceContext);
            searcher.Search(query, filter, secondPassCollector);
            ITopGroups<MutableValueInt32> topGroups = secondPassCollector.GetTopGroups(groupDocsOffset);

            //Validate topGroups are as expected
            StringBuilder sb = new StringBuilder();
            foreach (GroupDocs<MutableValueInt32> groupDocs in topGroups.Groups)
            {
                sb.AppendLine();
                if (groupDocs.GroupValue != null)
                {
                    int major = groupDocs.GroupValue.Value;
                    sb.AppendLine($"Group: {major}");
                }
                else
                {
                    sb.AppendLine($"Ungrouped");    //Happens when matching documents don't contain the group field
                }

                foreach (ScoreDoc scoreDoc in groupDocs.ScoreDocs)
                {
                    doc = searcher.Doc(scoreDoc.Doc);
                    sb.AppendLine($"{doc.GetField("major").GetInt32Value()} {doc.GetField("minor").GetInt32Value()} {doc.GetField("rev").GetInt32Value()}");
                }
            }

            string output = sb.ToString();
            string expectdValue = "\r\nGroup: 1000\r\n1000 1102 21\r\n1000 1123 45\r\n\r\nGroup: 2000\r\n2000 2222 7\r\n2000 2888 88\r\n\r\nGroup: 3000\r\n3000 3123 11\r\n3000 3222 37\r\n\r\nGroup: 4000\r\n4000 4001 88\r\n4000 4011 10\r\n".Replace("\r\n", Environment.NewLine);
            assertEquals(expectdValue, output);

            /*  Output:
             *  
                Group: 1000
                1000 1102 21
                1000 1123 45

                Group: 2000
                2000 2222 7
                2000 2888 88

                Group: 3000
                3000 3123 11
                3000 3222 37

                Group: 4000
                4000 4001 88
                4000 4011 10
            */
        }

        private string[,] GetCarData()
        {

            return new string[,] {
                { "Toyota", "4Runner" , "Blue" },
                { "Toyota", "4Runner" , "Green" },
                { "Ford", "Bronco" , "Green" },
                { "Ford", "Expedition" , "Yellow" },
                { "Ford", "Expedition" , "Blue" },
                { "Toyota", "Celica" , "Orange" },
                { "Audi", "S4" , "Yellow" },
                { "Audi", "A3" , "Orange" },
                { "Toyota", "Camry" , "Yellow" },
                { "Ford", "F150 Truck" , "Green" },
                { "Bently", "Azure" , "Green" },
                { "Ford", "Bronco" , "Orange" },
                { "Ford", "Aspire" , "Yellow" },
                { "Audi", "A3" , "Green" },
                { "Audi", "S4" , "Blue" },
                { "Bently", "Arnage" , "Grey" },
                { "Toyota", "4Runner" , "Yellow" },
                { "Toyota", "Camry" , "Blue" },
                { "Bently", "Azure" , "Blue" },
                { "Ford", "Bronco" , "Blue" },
                { "Ford", "Expedition" , "Green" },
                { "Ford", "F150 Truck" , "Blue" },
                { "Toyota", "Celica" , "Blue" },
                { "Ford", "F150 Truck" , "Yellow" },
                { "Ford", "Aspire" , "Blue" },
                { "Audi", "A3" , "Blue" },
                { "Bently", "Arnage" , "Blue" },
            };
        }

        private int[,] GetNumbers()
        {

            return new int[,] {
                { 1000, 1102 , 21 },
                { 4000, 4001 , 88 },
                { 8000, 8123 , 28 },
                { 4000, 4011 , 10 },
                { 2000, 2222 , 7 },
                { 3000, 3222 , 37 },
                { 2000, 2888 , 88 },
                { 3000, 3123 , 11 },
                { 8000, 8888 , 8 },
                { 1000, 1123 , 45 },
                { 3000, 3993 , 9 },
                { 8000, 8998 , 92 },
            };
        }


    }
}
