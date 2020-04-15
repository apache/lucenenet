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

 Unicode collation support. `Collation` converts each token into its binary `CollationKey` using the provided `Collator`, allowing it to be stored as an index term. 

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

      // "fa" Locale is not supported by Sun JDK 1.4 or 1.5
      Collator collator = Collator.getInstance(new Locale("ar"));
      CollationKeyAnalyzer analyzer = new CollationKeyAnalyzer(version, collator);
      RAMDirectory ramDir = new RAMDirectory();
      IndexWriter writer = new IndexWriter(ramDir, new IndexWriterConfig(version, analyzer));
      Document doc = new Document();
      doc.add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
      writer.addDocument(doc);
      writer.close();
      IndexReader ir = DirectoryReader.open(ramDir);
      IndexSearcher is = new IndexSearcher(ir);
    
  QueryParser aqp = new QueryParser(version, "content", analyzer);
      aqp.setAnalyzeRangeTerms(true);

      // Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
      // orders the U+0698 character before the U+0633 character, so the single
      // indexed Term above should NOT be returned by a ConstantScoreRangeQuery
      // with a Farsi Collator (or an Arabic one for the case when Farsi is not
      // supported).
      ScoreDoc[] result
        = is.search(aqp.parse("[ \u062F TO \u0698 ]"), null, 1000).scoreDocs;
      assertEquals("The index Term should not be included.", 0, result.length);

### Danish Sorting

      Analyzer analyzer 
        = new CollationKeyAnalyzer(version, Collator.getInstance(new Locale("da", "dk")));
      RAMDirectory indexStore = new RAMDirectory();
      IndexWriter writer = new IndexWriter(indexStore, new IndexWriterConfig(version, analyzer));
      String[] tracer = new String[] { "A", "B", "C", "D", "E" };
      String[] data = new String[] { "HAT", "HUT", "H\u00C5T", "H\u00D8T", "HOT" };
      String[] sortedTracerOrder = new String[] { "A", "E", "B", "D", "C" };
      for (int i = 0 ; i < data.length="" ;="" ++i)="" {="" document="" doc="new" document();="" doc.add(new="" storedfield("tracer",="" tracer[i]));="" doc.add(new="" textfield("contents",="" data[i],="" field.store.no));="" writer.adddocument(doc);="" }="" writer.close();="" indexreader="" ir="DirectoryReader.open(indexStore);" indexsearcher="" searcher="new" indexsearcher(ir);="" sort="" sort="new" sort();="" sort.setsort(new="" sortfield("contents",="" sortfield.string));="" query="" query="new" matchalldocsquery();="" scoredoc[]="" result="searcher.search(query," null,="" 1000,="" sort).scoredocs;="" for="" (int="" i="0" ;="" i="">< result.length="" ;="" ++i)="" {="" document="" doc="searcher.doc(result[i].doc);" assertequals(sortedtracerorder[i],="" doc.getvalues("tracer")[0]);="" }="">

### Turkish Case Normalization

      Collator collator = Collator.getInstance(new Locale("tr", "TR"));
      collator.setStrength(Collator.PRIMARY);
      Analyzer analyzer = new CollationKeyAnalyzer(version, collator);
      RAMDirectory ramDir = new RAMDirectory();
      IndexWriter writer = new IndexWriter(ramDir, new IndexWriterConfig(version, analyzer));
      Document doc = new Document();
      doc.add(new TextField("contents", "DIGY", Field.Store.NO));
      writer.addDocument(doc);
      writer.close();
      IndexReader ir = DirectoryReader.open(ramDir);
      IndexSearcher is = new IndexSearcher(ir);
      QueryParser parser = new QueryParser(version, "contents", analyzer);
      Query query = parser.parse("d\u0131gy");   // U+0131: dotless i
      ScoreDoc[] result = is.search(query, null, 1000).scoreDocs;
      assertEquals("The index Term should be included.", 1, result.length);

## Caveats and Comparisons

 __WARNING:__ Make sure you use exactly the same `Collator` at index and query time -- `CollationKey`s are only comparable when produced by the same `Collator`. Since {@link java.text.RuleBasedCollator}s are not independently versioned, it is unsafe to search against stored `CollationKey`s unless the following are exactly the same (best practice is to store this information with the index and check that they remain the same at query time): 

1.  JVM vendor

2.  JVM version, including patch version

3.  The language (and country and variant, if specified) of the Locale
    used when constructing the collator via
    {@link java.text.Collator#getInstance(java.util.Locale)}.

4.  The collation strength used - see {@link java.text.Collator#setStrength(int)}

 `ICUCollationKeyAnalyzer`, available in the [icu analysis module]({@docRoot}/../analyzers-icu/overview-summary.html), uses ICU4J's `Collator`, which makes its version available, thus allowing collation to be versioned independently from the JVM. `ICUCollationKeyAnalyzer` is also significantly faster and generates significantly shorter keys than `CollationKeyAnalyzer`. See [http://site.icu-project.org/charts/collation-icu4j-sun](http://site.icu-project.org/charts/collation-icu4j-sun) for key generation timing and key length comparisons between ICU4J and `java.text.Collator` over several languages. 

 `CollationKey`s generated by `java.text.Collator`s are not compatible with those those generated by ICU Collators. Specifically, if you use `CollationKeyAnalyzer` to generate index terms, do not use `ICUCollationKeyAnalyzer` on the query side, or vice versa. 