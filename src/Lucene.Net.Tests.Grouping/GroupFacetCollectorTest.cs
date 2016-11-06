using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

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

    public class GroupFacetCollectorTest : AbstractGroupingTestCase
    {
        [Test]
        public void TestSimple()
        {
            string groupField = "hotel";
            FieldType customType = new FieldType();
            customType.Stored = true;

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random(),
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            bool canUseDV = !"Lucene3x".Equals(w.w.Config.Codec.Name, StringComparison.Ordinal);
            bool useDv = canUseDV && Random().nextBoolean();

            // 0
            Document doc = new Document();
            AddField(doc, groupField, "a", useDv);
            AddField(doc, "airport", "ams", useDv);
            AddField(doc, "duration", "5", useDv);
            w.AddDocument(doc);

            // 1
            doc = new Document();
            AddField(doc, groupField, "a", useDv);
            AddField(doc, "airport", "dus", useDv);
            AddField(doc, "duration", "10", useDv);
            w.AddDocument(doc);

            // 2
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            AddField(doc, "airport", "ams", useDv);
            AddField(doc, "duration", "10", useDv);
            w.AddDocument(doc);
            w.Commit(); // To ensure a second segment

            // 3
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            AddField(doc, "airport", "ams", useDv);
            AddField(doc, "duration", "5", useDv);
            w.AddDocument(doc);

            // 4
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            AddField(doc, "airport", "ams", useDv);
            AddField(doc, "duration", "5", useDv);
            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.Reader);

            List<TermGroupFacetCollector.FacetEntry> entries = null;
            AbstractGroupFacetCollector groupedAirportFacetCollector = null;
            TermGroupFacetCollector.GroupedFacetResult airportResult = null;

            foreach (int limit in new int[] { 2, 10, 100, int.MaxValue })
            {
                // any of these limits is plenty for the data we have

                groupedAirportFacetCollector = CreateRandomCollector
                  (useDv ? "hotel_dv" : "hotel",
                   useDv ? "airport_dv" : "airport", null, false);
                indexSearcher.Search(new MatchAllDocsQuery(), groupedAirportFacetCollector);
                int maxOffset = 5;
                airportResult = groupedAirportFacetCollector.MergeSegmentResults
                    (int.MaxValue == limit ? limit : maxOffset + limit, 0, false);


                assertEquals(3, airportResult.TotalCount);
                assertEquals(0, airportResult.TotalMissingCount);

                entries = airportResult.GetFacetEntries(maxOffset, limit);
                assertEquals(0, entries.size());

                entries = airportResult.GetFacetEntries(0, limit);
                assertEquals(2, entries.size());
                assertEquals("ams", entries[0].Value.Utf8ToString());
                assertEquals(2, entries[0].Count);
                assertEquals("dus", entries[1].Value.Utf8ToString());
                assertEquals(1, entries[1].Count);

                entries = airportResult.GetFacetEntries(1, limit);
                assertEquals(1, entries.size());
                assertEquals("dus", entries[0].Value.Utf8ToString());
                assertEquals(1, entries[0].Count);
            }

            AbstractGroupFacetCollector groupedDurationFacetCollector = CreateRandomCollector(useDv ? "hotel_dv" : "hotel", useDv ? "duration_dv" : "duration", null, false);
            indexSearcher.Search(new MatchAllDocsQuery(), groupedDurationFacetCollector);
            TermGroupFacetCollector.GroupedFacetResult durationResult = groupedDurationFacetCollector.MergeSegmentResults(10, 0, false);
            assertEquals(4, durationResult.TotalCount);
            assertEquals(0, durationResult.TotalMissingCount);

            entries = durationResult.GetFacetEntries(0, 10);
            assertEquals(2, entries.size());
            assertEquals("10", entries[0].Value.Utf8ToString());
            assertEquals(2, entries[0].Count);
            assertEquals("5", entries[1].Value.Utf8ToString());
            assertEquals(2, entries[1].Count);

            // 5
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            // missing airport
            if (useDv)
            {
                AddField(doc, "airport", "", useDv);
            }
            AddField(doc, "duration", "5", useDv);
            w.AddDocument(doc);

            // 6
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            AddField(doc, "airport", "bru", useDv);
            AddField(doc, "duration", "10", useDv);
            w.AddDocument(doc);

            // 7
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            AddField(doc, "airport", "bru", useDv);
            AddField(doc, "duration", "15", useDv);
            w.AddDocument(doc);

            // 8
            doc = new Document();
            AddField(doc, groupField, "a", useDv);
            AddField(doc, "airport", "bru", useDv);
            AddField(doc, "duration", "10", useDv);
            w.AddDocument(doc);

            indexSearcher.IndexReader.Dispose();
            indexSearcher = NewSearcher(w.Reader);
            groupedAirportFacetCollector = CreateRandomCollector(useDv ? "hotel_dv" : "hotel", useDv ? "airport_dv" : "airport", null, !useDv);
            indexSearcher.Search(new MatchAllDocsQuery(), groupedAirportFacetCollector);
            airportResult = groupedAirportFacetCollector.MergeSegmentResults(3, 0, true);
            entries = airportResult.GetFacetEntries(1, 2);
            assertEquals(2, entries.size());
            if (useDv)
            {
                assertEquals(6, airportResult.TotalCount);
                assertEquals(0, airportResult.TotalMissingCount);
                assertEquals("bru", entries[0].Value.Utf8ToString());
                assertEquals(2, entries[0].Count);
                assertEquals("", entries[1].Value.Utf8ToString());
                assertEquals(1, entries[1].Count);
            }
            else
            {
                assertEquals(5, airportResult.TotalCount);
                assertEquals(1, airportResult.TotalMissingCount);
                assertEquals("bru", entries[0].Value.Utf8ToString());
                assertEquals(2, entries[0].Count);
                assertEquals("dus", entries[1].Value.Utf8ToString());
                assertEquals(1, entries[1].Count);
            }

            groupedDurationFacetCollector = CreateRandomCollector(useDv ? "hotel_dv" : "hotel", useDv ? "duration_dv" : "duration", null, false);
            indexSearcher.Search(new MatchAllDocsQuery(), groupedDurationFacetCollector);
            durationResult = groupedDurationFacetCollector.MergeSegmentResults(10, 2, true);
            assertEquals(5, durationResult.TotalCount);
            assertEquals(0, durationResult.TotalMissingCount);

            entries = durationResult.GetFacetEntries(1, 1);
            assertEquals(1, entries.size());
            assertEquals("5", entries[0].Value.Utf8ToString());
            assertEquals(2, entries[0].Count);

            // 9
            doc = new Document();
            AddField(doc, groupField, "c", useDv);
            AddField(doc, "airport", "bru", useDv);
            AddField(doc, "duration", "15", useDv);
            w.AddDocument(doc);

            // 10
            doc = new Document();
            AddField(doc, groupField, "c", useDv);
            AddField(doc, "airport", "dus", useDv);
            AddField(doc, "duration", "10", useDv);
            w.AddDocument(doc);

            indexSearcher.IndexReader.Dispose();
            indexSearcher = NewSearcher(w.Reader);
            groupedAirportFacetCollector = CreateRandomCollector(useDv ? "hotel_dv" : "hotel", useDv ? "airport_dv" : "airport", null, false);
            indexSearcher.Search(new MatchAllDocsQuery(), groupedAirportFacetCollector);
            airportResult = groupedAirportFacetCollector.MergeSegmentResults(10, 0, false);
            entries = airportResult.GetFacetEntries(0, 10);
            if (useDv)
            {
                assertEquals(8, airportResult.TotalCount);
                assertEquals(0, airportResult.TotalMissingCount);
                assertEquals(4, entries.size());
                assertEquals("", entries[0].Value.Utf8ToString());
                assertEquals(1, entries[0].Count);
                assertEquals("ams", entries[1].Value.Utf8ToString());
                assertEquals(2, entries[1].Count);
                assertEquals("bru", entries[2].Value.Utf8ToString());
                assertEquals(3, entries[2].Count);
                assertEquals("dus", entries[3].Value.Utf8ToString());
                assertEquals(2, entries[3].Count);
            }
            else
            {
                assertEquals(7, airportResult.TotalCount);
                assertEquals(1, airportResult.TotalMissingCount);
                assertEquals(3, entries.size());
                assertEquals("ams", entries[0].Value.Utf8ToString());
                assertEquals(2, entries[0].Count);
                assertEquals("bru", entries[1].Value.Utf8ToString());
                assertEquals(3, entries[1].Count);
                assertEquals("dus", entries[2].Value.Utf8ToString());
                assertEquals(2, entries[2].Count);
            }

            groupedDurationFacetCollector = CreateRandomCollector(useDv ? "hotel_dv" : "hotel", useDv ? "duration_dv" : "duration", "1", false);
            indexSearcher.Search(new MatchAllDocsQuery(), groupedDurationFacetCollector);
            durationResult = groupedDurationFacetCollector.MergeSegmentResults(10, 0, true);
            assertEquals(5, durationResult.TotalCount);
            assertEquals(0, durationResult.TotalMissingCount);

            entries = durationResult.GetFacetEntries(0, 10);
            assertEquals(2, entries.size());
            assertEquals("10", entries[0].Value.Utf8ToString());
            assertEquals(3, entries[0].Count);
            assertEquals("15", entries[1].Value.Utf8ToString());
            assertEquals(2, entries[1].Count);

            w.Dispose();
            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMVGroupedFacetingWithDeletes()
        {
            string groupField = "hotel";
            FieldType customType = new FieldType();
            customType.Stored = (true);

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random(),
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
            bool useDv = false;

            // Cannot assert this since we use NoMergePolicy:
            w.DoRandomForceMergeAssert = (false);

            // 0
            Document doc = new Document();
            doc.Add(new StringField("x", "x", Field.Store.NO));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            AddField(doc, groupField, "a", useDv);
            doc.Add(new StringField("airport", "ams", Field.Store.NO));
            w.AddDocument(doc);

            w.Commit();
            w.DeleteDocuments(new TermQuery(new Term("airport", "ams")));

            // 2
            doc = new Document();
            AddField(doc, groupField, "a", useDv);
            doc.Add(new StringField("airport", "ams", Field.Store.NO));
            w.AddDocument(doc);

            // 3
            doc = new Document();
            AddField(doc, groupField, "a", useDv);
            doc.Add(new StringField("airport", "dus", Field.Store.NO));

            w.AddDocument(doc);

            // 4
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            doc.Add(new StringField("airport", "ams", Field.Store.NO));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            doc.Add(new StringField("airport", "ams", Field.Store.NO));
            w.AddDocument(doc);

            // 6
            doc = new Document();
            AddField(doc, groupField, "b", useDv);
            doc.Add(new StringField("airport", "ams", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();

            // 7
            doc = new Document();
            doc.Add(new StringField("x", "x", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();

            w.Dispose();
            IndexSearcher indexSearcher = NewSearcher(DirectoryReader.Open(dir));
            AbstractGroupFacetCollector groupedAirportFacetCollector = CreateRandomCollector(groupField, "airport", null, true);
            indexSearcher.Search(new MatchAllDocsQuery(), groupedAirportFacetCollector);
            TermGroupFacetCollector.GroupedFacetResult airportResult = groupedAirportFacetCollector.MergeSegmentResults(10, 0, false);
            assertEquals(3, airportResult.TotalCount);
            assertEquals(1, airportResult.TotalMissingCount);

            List<TermGroupFacetCollector.FacetEntry> entries = airportResult.GetFacetEntries(0, 10);
            assertEquals(2, entries.size());
            assertEquals("ams", entries[0].Value.Utf8ToString());
            assertEquals(2, entries[0].Count);
            assertEquals("dus", entries[1].Value.Utf8ToString());
            assertEquals(1, entries[1].Count);

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private void AddField(Document doc, string field, string value, bool canUseIDV)
        {
            doc.Add(new StringField(field, value, Field.Store.NO));
            if (canUseIDV)
            {
                doc.Add(new SortedDocValuesField(field + "_dv", new BytesRef(value)));
            }
        }

        [Test]
        public void TestRandom()
        {
            Random random = Random();
            int numberOfRuns = TestUtil.NextInt(random, 3, 6);
            for (int indexIter = 0; indexIter < numberOfRuns; indexIter++)
            {
                bool multipleFacetsPerDocument = random.nextBoolean();
                IndexContext context = CreateIndexContext(multipleFacetsPerDocument);
                IndexSearcher searcher = NewSearcher(context.indexReader);

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: searcher=" + searcher);
                }

                for (int searchIter = 0; searchIter < 100; searchIter++)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: searchIter=" + searchIter);
                    }
                    bool useDv = !multipleFacetsPerDocument && context.useDV && random.nextBoolean();
                    string searchTerm = context.contentStrings[random.nextInt(context.contentStrings.Length)];
                    int limit = random.nextInt(context.facetValues.size());
                    int offset = random.nextInt(context.facetValues.size() - limit);
                    int size = offset + limit;
                    int minCount = random.nextBoolean() ? 0 : random.nextInt(1 + context.facetWithMostGroups / 10);
                    bool orderByCount = random.nextBoolean();
                    string randomStr = GetFromSet(context.facetValues, random.nextInt(context.facetValues.size()));
                    string facetPrefix;
                    if (randomStr == null)
                    {
                        facetPrefix = null;
                    }
                    else
                    {
                        int codePointLen = randomStr.CodePointCount(0, randomStr.Length);
                        int randomLen = random.nextInt(codePointLen);
                        if (codePointLen == randomLen - 1)
                        {
                            facetPrefix = null;
                        }
                        else
                        {
                            int end = randomStr.OffsetByCodePoints(0, randomLen);
                            facetPrefix = random.nextBoolean() ? null : randomStr.Substring(end);
                        }
                    }

                    GroupedFacetResult expectedFacetResult = CreateExpectedFacetResult(searchTerm, context, offset, limit, minCount, orderByCount, facetPrefix);
                    AbstractGroupFacetCollector groupFacetCollector = CreateRandomCollector(useDv ? "group_dv" : "group", useDv ? "facet_dv" : "facet", facetPrefix, multipleFacetsPerDocument);
                    searcher.Search(new TermQuery(new Term("content", searchTerm)), groupFacetCollector);
                    TermGroupFacetCollector.GroupedFacetResult actualFacetResult = groupFacetCollector.MergeSegmentResults(size, minCount, orderByCount);

                    List<TermGroupFacetCollector.FacetEntry> expectedFacetEntries = expectedFacetResult.GetFacetEntries();
                    List<TermGroupFacetCollector.FacetEntry> actualFacetEntries = actualFacetResult.GetFacetEntries(offset, limit);

                    if (VERBOSE)
                    {
                        Console.WriteLine("Use DV: " + useDv);
                        Console.WriteLine("Collector: " + groupFacetCollector.GetType().Name);
                        Console.WriteLine("Num group: " + context.numGroups);
                        Console.WriteLine("Num doc: " + context.numDocs);
                        Console.WriteLine("Index iter: " + indexIter);
                        Console.WriteLine("multipleFacetsPerDocument: " + multipleFacetsPerDocument);
                        Console.WriteLine("Search iter: " + searchIter);

                        Console.WriteLine("Search term: " + searchTerm);
                        Console.WriteLine("Min count: " + minCount);
                        Console.WriteLine("Facet offset: " + offset);
                        Console.WriteLine("Facet limit: " + limit);
                        Console.WriteLine("Facet prefix: " + facetPrefix);
                        Console.WriteLine("Order by count: " + orderByCount);

                        Console.WriteLine("\n=== Expected: \n");
                        Console.WriteLine("Total count " + expectedFacetResult.TotalCount);
                        Console.WriteLine("Total missing count " + expectedFacetResult.TotalMissingCount);
                        int counter = 0;
                        foreach (TermGroupFacetCollector.FacetEntry expectedFacetEntry in expectedFacetEntries)
                        {
                            Console.WriteLine(
                                string.Format(CultureInfo.InvariantCulture,
                                    "{0}. Expected facet value {1} with count {2}",
                                    counter++, expectedFacetEntry.Value.Utf8ToString(), expectedFacetEntry.Count
                                )
                            );
                        }

                        Console.WriteLine("\n=== Actual: \n");
                        Console.WriteLine("Total count " + actualFacetResult.TotalCount);
                        Console.WriteLine("Total missing count " + actualFacetResult.TotalMissingCount);
                        counter = 0;
                        foreach (TermGroupFacetCollector.FacetEntry actualFacetEntry in actualFacetEntries)
                        {
                            Console.WriteLine(
                                string.Format(CultureInfo.InvariantCulture,
                                    "{0}. Actual facet value {1} with count {2}",
                                    counter++, actualFacetEntry.Value.Utf8ToString(), actualFacetEntry.Count
                                )
                            );
                        }
                        Console.WriteLine("\n===================================================================================");
                    }

                    assertEquals(expectedFacetResult.TotalCount, actualFacetResult.TotalCount);
                    assertEquals(expectedFacetResult.TotalMissingCount, actualFacetResult.TotalMissingCount);
                    assertEquals(expectedFacetEntries.size(), actualFacetEntries.size());
                    for (int i = 0; i < expectedFacetEntries.size(); i++)
                    {
                        TermGroupFacetCollector.FacetEntry expectedFacetEntry = expectedFacetEntries[i];
                        TermGroupFacetCollector.FacetEntry actualFacetEntry = actualFacetEntries[i];
                        assertEquals("i=" + i + ": " + expectedFacetEntry.Value.Utf8ToString() + " != " + actualFacetEntry.Value.Utf8ToString(), expectedFacetEntry.Value, actualFacetEntry.Value);
                        assertEquals("i=" + i + ": " + expectedFacetEntry.Count + " != " + actualFacetEntry.Count, expectedFacetEntry.Count, actualFacetEntry.Count);
                    }
                }

                context.indexReader.Dispose();
                context.dir.Dispose();
            }
        }

        internal class ComparerAnonymousHelper1 : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                if (a == b)
                {
                    return 0;
                }
                else if (a == null)
                {
                    return -1;
                }
                else if (b == null)
                {
                    return 1;
                }
                else
                {
                    return a.CompareToOrdinal(b);
                }
            }
        }

        private IndexContext CreateIndexContext(bool multipleFacetValuesPerDocument)
        {
            Random random = Random();
            int numDocs = TestUtil.NextInt(random, 138, 1145) * RANDOM_MULTIPLIER;
            int numGroups = TestUtil.NextInt(random, 1, numDocs / 4);
            int numFacets = TestUtil.NextInt(random, 1, numDocs / 6);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: numDocs=" + numDocs + " numGroups=" + numGroups);
            }

            List<string> groups = new List<string>();
            for (int i = 0; i < numGroups; i++)
            {
                groups.Add(GenerateRandomNonEmptyString());
            }
            List<string> facetValues = new List<string>();
            for (int i = 0; i < numFacets; i++)
            {
                facetValues.Add(GenerateRandomNonEmptyString());
            }
            string[] contentBrs = new string[TestUtil.NextInt(random, 2, 20)];
            if (VERBOSE)
            {
                Console.WriteLine("TEST: create fake content");
            }
            for (int contentIDX = 0; contentIDX < contentBrs.Length; contentIDX++)
            {
                contentBrs[contentIDX] = GenerateRandomNonEmptyString();
                if (VERBOSE)
                {
                    Console.WriteLine("  content=" + contentBrs[contentIDX]);
                }
            }

            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(
                random,
                dir,
                NewIndexWriterConfig(
                    TEST_VERSION_CURRENT,
                    new MockAnalyzer(random)
                )
            );
            bool canUseDV = !"Lucene3x".Equals(writer.w.Config.Codec.Name, StringComparison.Ordinal);
            bool useDv = canUseDV && !multipleFacetValuesPerDocument && random.nextBoolean();

            Document doc = new Document();
            Document docNoGroup = new Document();
            Document docNoFacet = new Document();
            Document docNoGroupNoFacet = new Document();
            Field group = NewStringField("group", "", Field.Store.NO);
            Field groupDc = new SortedDocValuesField("group_dv", new BytesRef());
            if (useDv)
            {
                doc.Add(groupDc);
                docNoFacet.Add(groupDc);
            }
            doc.Add(group);
            docNoFacet.Add(group);
            Field[] facetFields;
            if (useDv)
            {
                Debug.Assert(!multipleFacetValuesPerDocument);
                facetFields = new Field[2];
                facetFields[0] = NewStringField("facet", "", Field.Store.NO);
                doc.Add(facetFields[0]);
                docNoGroup.Add(facetFields[0]);
                facetFields[1] = new SortedDocValuesField("facet_dv", new BytesRef());
                doc.Add(facetFields[1]);
                docNoGroup.Add(facetFields[1]);
            }
            else
            {
                facetFields = multipleFacetValuesPerDocument ? new Field[2 + random.nextInt(6)] : new Field[1];
                for (int i = 0; i < facetFields.Length; i++)
                {
                    facetFields[i] = NewStringField("facet", "", Field.Store.NO);
                    doc.Add(facetFields[i]);
                    docNoGroup.Add(facetFields[i]);
                }
            }
            Field content = NewStringField("content", "", Field.Store.NO);
            doc.Add(content);
            docNoGroup.Add(content);
            docNoFacet.Add(content);
            docNoGroupNoFacet.Add(content);

            // LUCENENET NOTE: TreeSet (the class used in Java) allows duplicate keys. However, SortedSet seems to work,
            // and based on the name of the variable, presuming the entries are meant to be unique.
            ISet<string> uniqueFacetValues = new SortedSet<string>(new ComparerAnonymousHelper1());

            // LUCENENET NOTE: Need HashMap here because of null keys
            IDictionary<string, HashMap<string, ISet<string>>> searchTermToFacetToGroups = new Dictionary<string, HashMap<string, ISet<string>>>();
            int facetWithMostGroups = 0;
            for (int i = 0; i < numDocs; i++)
            {
                string groupValue;
                if (random.nextInt(24) == 17)
                {
                    // So we test the "doc doesn't have the group'd
                    // field" case:
                    if (useDv)
                    {
                        groupValue = "";
                    }
                    else
                    {
                        groupValue = null;
                    }
                }
                else
                {
                    groupValue = groups[random.nextInt(groups.size())];
                }

                string contentStr = contentBrs[random.nextInt(contentBrs.Length)];
                if (!searchTermToFacetToGroups.ContainsKey(contentStr))
                {
                    searchTermToFacetToGroups[contentStr] = new HashMap<string, ISet<string>>();
                }
                IDictionary<string, ISet<string>> facetToGroups = searchTermToFacetToGroups[contentStr];

                List<string> facetVals = new List<string>();
                if (useDv || random.nextInt(24) != 18)
                {
                    if (useDv)
                    {
                        string facetValue = facetValues[random.nextInt(facetValues.size())];
                        uniqueFacetValues.Add(facetValue);
                        if (!facetToGroups.ContainsKey(facetValue))
                        {
                            facetToGroups[facetValue] = new HashSet<string>();
                        }
                        ISet<string> groupsInFacet = facetToGroups[facetValue];
                        groupsInFacet.add(groupValue);
                        if (groupsInFacet.size() > facetWithMostGroups)
                        {
                            facetWithMostGroups = groupsInFacet.size();
                        }
                        facetFields[0].StringValue = (facetValue);
                        facetFields[1].BytesValue = (new BytesRef(facetValue));
                        facetVals.Add(facetValue);
                    }
                    else
                    {
                        foreach (Field facetField in facetFields)
                        {
                            string facetValue = facetValues[random.nextInt(facetValues.size())];
                            uniqueFacetValues.Add(facetValue);
                            if (!facetToGroups.ContainsKey(facetValue))
                            {
                                facetToGroups[facetValue] = new HashSet<string>();
                            }
                            ISet<string> groupsInFacet = facetToGroups[facetValue];
                            groupsInFacet.add(groupValue);
                            if (groupsInFacet.size() > facetWithMostGroups)
                            {
                                facetWithMostGroups = groupsInFacet.size();
                            }
                            facetField.StringValue = (facetValue);
                            facetVals.Add(facetValue);
                        }
                    }
                }
                else
                {
                    uniqueFacetValues.Add(null);
                    if (!facetToGroups.ContainsKey(null))
                    {
                        facetToGroups.Put(null, new HashSet<string>());
                    }
                    ISet<string> groupsInFacet = facetToGroups[null];
                    groupsInFacet.add(groupValue);
                    if (groupsInFacet.size() > facetWithMostGroups)
                    {
                        facetWithMostGroups = groupsInFacet.size();
                    }
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  doc content=" + contentStr + " group=" + (groupValue == null ? "null" : groupValue) + " facetVals=" + facetVals);
                }

                if (groupValue != null)
                {
                    if (useDv)
                    {
                        groupDc.BytesValue = (new BytesRef(groupValue));
                    }
                    group.StringValue = (groupValue);
                }
                else if (useDv)
                {
                    // DV cannot have missing values:
                    groupDc.BytesValue = (new BytesRef());
                }
                content.StringValue = (contentStr);
                if (groupValue == null && !facetVals.Any())
                {
                    writer.AddDocument(docNoGroupNoFacet);
                }
                else if (!facetVals.Any())
                {
                    writer.AddDocument(docNoFacet);
                }
                else if (groupValue == null)
                {
                    writer.AddDocument(docNoGroup);
                }
                else
                {
                    writer.AddDocument(doc);
                }
            }

            DirectoryReader reader = writer.Reader;
            writer.Dispose();

            return new IndexContext(searchTermToFacetToGroups, reader, numDocs, dir, facetWithMostGroups, numGroups, contentBrs, uniqueFacetValues, useDv);
        }

        internal class ComparerAnonymousHelper2 : IComparer<TermGroupFacetCollector.FacetEntry>
        {
            private readonly bool orderByCount;

            public ComparerAnonymousHelper2(bool orderByCount)
            {
                this.orderByCount = orderByCount;
            }

            public int Compare(AbstractGroupFacetCollector.FacetEntry a, AbstractGroupFacetCollector.FacetEntry b)
            {
                if (orderByCount)
                {
                    int cmp = b.Count - a.Count;
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }
                return a.Value.CompareTo(b.Value);
            }
        }

        private GroupedFacetResult CreateExpectedFacetResult(string searchTerm, IndexContext context, int offset, int limit, int minCount, bool orderByCount, string facetPrefix)
        {
            HashMap<string, ISet<string>> facetGroups;
            if (!context.searchTermToFacetGroups.TryGetValue(searchTerm, out facetGroups))
            {
                facetGroups = new HashMap<string, ISet<string>>();
            }

            int totalCount = 0;
            int totalMissCount = 0;
            ISet<string> facetValues;
            if (facetPrefix != null)
            {
                facetValues = new HashSet<string>();
                foreach (string facetValue in context.facetValues)
                {
                    if (facetValue != null && facetValue.StartsWith(facetPrefix, StringComparison.Ordinal))
                    {
                        facetValues.add(facetValue);
                    }
                }
            }
            else
            {
                facetValues = context.facetValues;
            }

            List<TermGroupFacetCollector.FacetEntry> entries = new List<TermGroupFacetCollector.FacetEntry>(facetGroups.size());
            // also includes facets with count 0
            foreach (string facetValue in facetValues)
            {
                if (facetValue == null)
                {
                    continue;
                }

                ISet<string> groups = facetGroups.ContainsKey(facetValue) ? facetGroups[facetValue] : null;
                int count = groups != null ? groups.size() : 0;
                if (count >= minCount)
                {
                    entries.Add(new TermGroupFacetCollector.FacetEntry(new BytesRef(facetValue), count));
                }
                totalCount += count;
            }

            // Only include null count when no facet prefix is specified
            if (facetPrefix == null)
            {
                ISet<string> groups = facetGroups[null];
                if (groups != null)
                {
                    totalMissCount = groups.size();
                }
            }

            entries.Sort(new ComparerAnonymousHelper2(orderByCount));

            int endOffset = offset + limit;
            List<TermGroupFacetCollector.FacetEntry> entriesResult;
            if (offset >= entries.size())
            {
                entriesResult = new List<TermGroupFacetCollector.FacetEntry>();
            }
            else if (endOffset >= entries.size())
            {
                entriesResult = entries.GetRange(offset, entries.size() - offset);
            }
            else
            {
                entriesResult = entries.GetRange(offset, endOffset - offset);
            }
            return new GroupedFacetResult(totalCount, totalMissCount, entriesResult);
        }

        private AbstractGroupFacetCollector CreateRandomCollector(string groupField, string facetField, string facetPrefix, bool multipleFacetsPerDocument)
        {
            BytesRef facetPrefixBR = facetPrefix == null ? null : new BytesRef(facetPrefix);
            // DocValues cannot be multi-valued:
            Debug.Assert(!multipleFacetsPerDocument || !groupField.EndsWith("_dv", StringComparison.Ordinal));
            return TermGroupFacetCollector.CreateTermGroupFacetCollector(groupField, facetField, multipleFacetsPerDocument, facetPrefixBR, Random().nextInt(1024));
        }

        private string GetFromSet(ISet<string> set, int index)
        {
            int currentIndex = 0;
            foreach (string bytesRef in set)
            {
                if (currentIndex++ == index)
                {
                    return bytesRef;
                }
            }

            return null;
        }

        internal class IndexContext
        {
            internal readonly int numDocs;
            internal readonly DirectoryReader indexReader;
            internal readonly IDictionary<string, HashMap<string, ISet<string>>> searchTermToFacetGroups;
            internal readonly ISet<string> facetValues;
            internal readonly Directory dir;
            internal readonly int facetWithMostGroups;
            internal readonly int numGroups;
            internal readonly string[] contentStrings;
            internal readonly bool useDV;

            public IndexContext(IDictionary<string, HashMap<string, ISet<string>>> searchTermToFacetGroups, DirectoryReader r,
                                int numDocs, Directory dir, int facetWithMostGroups, int numGroups, string[] contentStrings, ISet<string> facetValues, bool useDV)
            {
                this.searchTermToFacetGroups = searchTermToFacetGroups;
                this.indexReader = r;
                this.numDocs = numDocs;
                this.dir = dir;
                this.facetWithMostGroups = facetWithMostGroups;
                this.numGroups = numGroups;
                this.contentStrings = contentStrings;
                this.facetValues = facetValues;
                this.useDV = useDV;
            }
        }

        internal class GroupedFacetResult
        {

            internal readonly int totalCount;
            internal readonly int totalMissingCount;
            internal readonly List<TermGroupFacetCollector.FacetEntry> facetEntries;

            internal GroupedFacetResult(int totalCount, int totalMissingCount, List<TermGroupFacetCollector.FacetEntry> facetEntries)
            {
                this.totalCount = totalCount;
                this.totalMissingCount = totalMissingCount;
                this.facetEntries = facetEntries;
            }

            public int TotalCount
            {
                get { return totalCount; }
            }

            public int TotalMissingCount
            {
                get { return totalMissingCount; }
            }

            public List<TermGroupFacetCollector.FacetEntry> GetFacetEntries()
            {
                return facetEntries;
            }
        }
    }
}
