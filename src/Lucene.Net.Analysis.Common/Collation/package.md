---
uid: Lucene.Net.Collation
summary: *content
---

<!--
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->

 Unicode collation support. `Collation` converts each token into its binary `System.Globalization.SortKey` using the provided `System.Globalization.CompareInfo` (the .NET platform collator), allowing it to be stored as an index term.

## Use Cases

*   Efficient sorting of terms in languages that use non-Unicode character
    orderings.  (Lucene Sort using a Locale can be very slow.)

*   Efficient range queries over fields that contain terms in languages that
    use non-Unicode character orderings.  (Range queries using a Locale can be
    very slow.)

*   Effective Locale-specific normalization (case differences, diacritics, etc.).
    (<xref:Lucene.Net.Analysis.Core.LowerCaseFilter> and
    <xref:Lucene.Net.Analysis.Miscellaneous.ASCIIFoldingFilter> provide these services
    in a generic way that doesn't take into account locale-specific needs.)

## Example Usages

### Farsi Range Queries

```cs
CompareInfo collator = CompareInfo.GetCompareInfo("ar");
CollationKeyAnalyzer analyzer = new CollationKeyAnalyzer(LuceneVersion.LUCENE_48, collator);
Store.Directory ramDir = new RAMDirectory();
IndexWriter writer = new IndexWriter(ramDir, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer));
Document doc = new Document();
doc.Add(new TextField("content", "ساب", Field.Store.YES));
writer.AddDocument(doc);
writer.Dispose();
IndexReader ir = DirectoryReader.Open(ramDir);
IndexSearcher searcher = new IndexSearcher(ir);

QueryParser aqp = new QueryParser(LuceneVersion.LUCENE_48, "content", analyzer);
aqp.AnalyzeRangeTerms = true;

// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
// orders the U+0698 character before the U+0633 character, so the single
// indexed Term above should NOT be returned by a TermRangeQuery with a
// Farsi Collator (or an Arabic one for the case when Farsi is not supported).
ScoreDoc[] result = searcher.Search(aqp.Parse("[ د TO ژ ]"), null, 1000).ScoreDocs;
assertEquals("The index Term should not be included.", 0, result.Length);
```

### Danish Sorting

```cs
Analyzer analyzer = new CollationKeyAnalyzer(LuceneVersion.LUCENE_48, CompareInfo.GetCompareInfo("da-DK"));
Store.Directory indexStore = new RAMDirectory();
IndexWriter writer = new IndexWriter(indexStore, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer));
string[] tracer = new string[] { "A", "B", "C", "D", "E" };
string[] data = new string[] { "HAT", "HUT", "HÅT", "HØT", "HOT" };
string[] sortedTracerOrder = new string[] { "A", "E", "B", "D", "C" };
for (int i = 0; i < data.Length; ++i)
{
    Document doc = new Document();
    doc.Add(new StoredField("tracer", tracer[i]));
    doc.Add(new TextField("contents", data[i], Field.Store.NO));
    writer.AddDocument(doc);
}
writer.Dispose();
IndexReader ir = DirectoryReader.Open(indexStore);
IndexSearcher searcher = new IndexSearcher(ir);
Sort sort = new Sort();
sort.SetSort(new SortField("contents", SortFieldType.STRING));
Query query = new MatchAllDocsQuery();
ScoreDoc[] result = searcher.Search(query, null, 1000, sort).ScoreDocs;
for (int i = 0; i < result.Length; ++i)
{
    Document doc = searcher.Doc(result[i].Doc);
    assertEquals(sortedTracerOrder[i], doc.GetValues("tracer")[0]);
}
```

### Turkish Case Normalization

```cs
// Primary collation strength is approximated with CompareOptions on the platform collator.
CompareInfo collator = CompareInfo.GetCompareInfo("tr-TR");
Analyzer analyzer = new CollationKeyAnalyzer(LuceneVersion.LUCENE_48, collator,
    CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
Store.Directory ramDir = new RAMDirectory();
IndexWriter writer = new IndexWriter(ramDir, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer));
Document doc = new Document();
doc.Add(new TextField("contents", "DIGY", Field.Store.NO));
writer.AddDocument(doc);
writer.Dispose();
IndexReader ir = DirectoryReader.Open(ramDir);
IndexSearcher searcher = new IndexSearcher(ir);
QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "contents", analyzer);
Query query = parser.Parse("dıgy");   // U+0131: dotless i
ScoreDoc[] result = searcher.Search(query, null, 1000).ScoreDocs;
assertEquals("The index Term should be included.", 1, result.Length);
```

## Caveats and Comparisons

 __WARNING:__ Make sure you use exactly the same collator (`System.Globalization.CompareInfo` *and* `System.Globalization.CompareOptions`) at index and query time -- `System.Globalization.SortKey`s are only comparable when produced by the same collator. Since the platform collator is not independently versioned, it is unsafe to search against stored `System.Globalization.SortKey`s unless the following are exactly the same (best practice is to store this information with the index and check that they remain the same at query time):

1.  The .NET runtime version, and the active globalization backend. .NET Framework uses Windows NLS, while .NET 5+ uses ICU by default; the two produce different sort keys and orderings.

2.  The language (and country and variant, if specified) of the culture used when obtaining the collator via `System.Globalization.CompareInfo.GetCompareInfo`.

3.  The `System.Globalization.CompareOptions` used - which approximate the collation strength (for example `IgnoreCase | IgnoreNonSpace` for primary strength) and Unicode normalization (decomposition).

 > [!NOTE]
 > Unlike Lucene's `java.text.RuleBasedCollator`, the .NET `System.Globalization.CompareInfo` does not support tailored (custom) collation rules, so <xref:Lucene.Net.Collation.CollationKeyFilterFactory> does not accept a `custom` ruleset. Before generating a sort key, each term is normalized to Unicode Normalization Form C (NFC) so that decomposed and precomposed input collate consistently across globalization backends.

 The `ICUCollationKeyAnalyzer`, available in the [Lucene.Net.ICU](../icu/overview.html) package, uses [ICU4N](https://www.nuget.org/packages/ICU4N/)'s `Collator`, which makes its version available, thus allowing collation to be versioned independently from the .NET runtime. `ICUCollationKeyAnalyzer` also generates significantly shorter keys than `CollationKeyAnalyzer`, and supports tailored (custom) rulesets. See [http://site.icu-project.org/charts/collation-icu4j-sun](http://site.icu-project.org/charts/collation-icu4j-sun) for key generation timing and key length comparisons between ICU4J and `java.text.Collator` over several languages.

 `System.Globalization.SortKey`s generated by the platform collator are not compatible with those generated by ICU Collators. Specifically, if you use `CollationKeyAnalyzer` to generate index terms, do not use `ICUCollationKeyAnalyzer` on the query side, or vice versa.
