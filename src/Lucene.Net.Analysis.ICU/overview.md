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
<!-- :Post-Release-Update-Version.LUCENE_XY: - several mentions in this file -->

    <meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
    <title>
      Apache Lucene ICU integration module
    </title>

This module exposes functionality from 
[ICU](http://site.icu-project.org/) to Apache Lucene. ICU4J is a Java
library that enhances Java's internationalization support by improving 
performance, keeping current with the Unicode Standard, and providing richer
APIs. 

For an introduction to Lucene's analysis API, see the [](xref:Lucene.Net.Analysis) package documentation.

 This module exposes the following functionality: 

*   [Text Segmentation](#segmentation): Tokenizes text based on 
  properties and rules defined in Unicode.
*   [Collation](#collation): Compare strings according to the 
  conventions and standards of a particular language, region or country.
*   [Normalization](#normalization): Converts text to a unique,
  equivalent form.
*   [Case Folding](#casefolding): Removes case distinctions with
  Unicode's Default Caseless Matching algorithm.
*   [Search Term Folding](#searchfolding): Removes distinctions
  (such as accent marks) between similar characters for a loose or fuzzy search.
*   [Text Transformation](#transform): Transforms Unicode text in
  a context-sensitive fashion: e.g. mapping Traditional to Simplified Chinese

* * *

# [Text Segmentation]()

 Text Segmentation (Tokenization) divides document and query text into index terms (typically words). Unicode provides special properties and rules so that this can be done in a manner that works well with most languages. 

 Text Segmentation implements the word segmentation specified in [Unicode Text Segmentation](http://unicode.org/reports/tr29/). Additionally the algorithm can be tailored based on writing system, for example text in the Thai script is automatically delegated to a dictionary-based segmentation algorithm. 

## Use Cases

*   As a more thorough replacement for StandardTokenizer that works well for
    most languages. 

## Example Usages

### Tokenizing multilanguage text

      /**
       * This tokenizer will work well in general for most languages.
       */
      Tokenizer tokenizer = new ICUTokenizer(reader);

* * *

# [Collation]()

 `ICUCollationKeyAnalyzer` converts each token into its binary `CollationKey` using the provided `Collator`, allowing it to be stored as an index term. 

 `ICUCollationKeyAnalyzer` depends on ICU4J to produce the `CollationKey`s. 

## Use Cases

*   Efficient sorting of terms in languages that use non-Unicode character 
    orderings.  (Lucene Sort using a Locale can be very slow.) 

*   Efficient range queries over fields that contain terms in languages that 
    use non-Unicode character orderings.  (Range queries using a Locale can be
    very slow.)

*   Effective Locale-specific normalization (case differences, diacritics, etc.).
    ([](xref:Lucene.Net.Analysis.Core.LowerCaseFilter) and 
    [](xref:Lucene.Net.Analysis.Miscellaneous.ASCIIFoldingFilter) provide these services
    in a generic way that doesn't take into account locale-specific needs.)

## Example Usages

### Farsi Range Queries

      Collator collator = Collator.getInstance(new ULocale("ar"));
      ICUCollationKeyAnalyzer analyzer = new ICUCollationKeyAnalyzer(Version.LUCENE_48, collator);
      RAMDirectory ramDir = new RAMDirectory();
      IndexWriter writer = new IndexWriter(ramDir, new IndexWriterConfig(Version.LUCENE_48, analyzer));
      Document doc = new Document();
      doc.add(new Field("content", "\u0633\u0627\u0628", 
                        Field.Store.YES, Field.Index.ANALYZED));
      writer.addDocument(doc);
      writer.close();
      IndexSearcher is = new IndexSearcher(ramDir, true);

      QueryParser aqp = new QueryParser(Version.LUCENE_48, "content", analyzer);
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
        = new ICUCollationKeyAnalyzer(Version.LUCENE_48, Collator.getInstance(new ULocale("da", "dk")));
      RAMDirectory indexStore = new RAMDirectory();
      IndexWriter writer = new IndexWriter(indexStore, new IndexWriterConfig(Version.LUCENE_48, analyzer));
      String[] tracer = new String[] { "A", "B", "C", "D", "E" };
      String[] data = new String[] { "HAT", "HUT", "H\u00C5T", "H\u00D8T", "HOT" };
      String[] sortedTracerOrder = new String[] { "A", "E", "B", "D", "C" };
      for (int i = 0 ; i < data.length="" ;="" ++i)="" {="" document="" doc="new" document();="" doc.add(new="" field("tracer",="" tracer[i],="" field.store.yes,="" field.index.no));="" doc.add(new="" field("contents",="" data[i],="" field.store.no,="" field.index.analyzed));="" writer.adddocument(doc);="" }="" writer.close();="" indexsearcher="" searcher="new" indexsearcher(indexstore,="" true);="" sort="" sort="new" sort();="" sort.setsort(new="" sortfield("contents",="" sortfield.string));="" query="" query="new" matchalldocsquery();="" scoredoc[]="" result="searcher.search(query," null,="" 1000,="" sort).scoredocs;="" for="" (int="" i="0" ;="" i="">< result.length="" ;="" ++i)="" {="" document="" doc="searcher.doc(result[i].doc);" assertequals(sortedtracerorder[i],="" doc.getvalues("tracer")[0]);="" }="">

### Turkish Case Normalization

      Collator collator = Collator.getInstance(new ULocale("tr", "TR"));
      collator.setStrength(Collator.PRIMARY);
      Analyzer analyzer = new ICUCollationKeyAnalyzer(Version.LUCENE_48, collator);
      RAMDirectory ramDir = new RAMDirectory();
      IndexWriter writer = new IndexWriter(ramDir, new IndexWriterConfig(Version.LUCENE_48, analyzer));
      Document doc = new Document();
      doc.add(new Field("contents", "DIGY", Field.Store.NO, Field.Index.ANALYZED));
      writer.addDocument(doc);
      writer.close();
      IndexSearcher is = new IndexSearcher(ramDir, true);
      QueryParser parser = new QueryParser(Version.LUCENE_48, "contents", analyzer);
      Query query = parser.parse("d\u0131gy");   // U+0131: dotless i
      ScoreDoc[] result = is.search(query, null, 1000).scoreDocs;
      assertEquals("The index Term should be included.", 1, result.length);

## Caveats and Comparisons

 **WARNING:** Make sure you use exactly the same `Collator` at index and query time -- `CollationKey`s are only comparable when produced by the same `Collator`. Since {@link java.text.RuleBasedCollator}s are not independently versioned, it is unsafe to search against stored `CollationKey`s unless the following are exactly the same (best practice is to store this information with the index and check that they remain the same at query time): 

1.  JVM vendor
2.  JVM version, including patch version
3.  The language (and country and variant, if specified) of the Locale
    used when constructing the collator via
    {@link java.text.Collator#getInstance(java.util.Locale)}.

4.  The collation strength used - see {@link java.text.Collator#setStrength(int)}

 `ICUCollationKeyAnalyzer` uses ICU4J's `Collator`, which makes its version available, thus allowing collation to be versioned independently from the JVM. `ICUCollationKeyAnalyzer` is also significantly faster and generates significantly shorter keys than `CollationKeyAnalyzer`. See [http://site.icu-project.org/charts/collation-icu4j-sun](http://site.icu-project.org/charts/collation-icu4j-sun) for key generation timing and key length comparisons between ICU4J and `java.text.Collator` over several languages. 

 `CollationKey`s generated by `java.text.Collator`s are not compatible with those those generated by ICU Collators. Specifically, if you use `CollationKeyAnalyzer` to generate index terms, do not use `ICUCollationKeyAnalyzer` on the query side, or vice versa. 

* * *

# [Normalization]()

 `ICUNormalizer2Filter` normalizes term text to a [Unicode Normalization Form](http://unicode.org/reports/tr15/), so that [equivalent](http://en.wikipedia.org/wiki/Unicode_equivalence) forms are standardized to a unique form. 

## Use Cases

*   Removing differences in width for Asian-language text. 

*   Standardizing complex text with non-spacing marks so that characters are 
  ordered consistently.

## Example Usages

### Normalizing text to NFC

      /**
       * Normalizer2 objects are unmodifiable and immutable.
       */
      Normalizer2 normalizer = Normalizer2.getInstance(null, "nfc", Normalizer2.Mode.COMPOSE);
      /**
       * This filter will normalize to NFC.
       */
      TokenStream tokenstream = new ICUNormalizer2Filter(tokenizer, normalizer);

* * *

# [Case Folding]()

 Default caseless matching, or case-folding is more than just conversion to lowercase. For example, it handles cases such as the Greek sigma, so that "Μάϊος" and "ΜΆΪΟΣ" will match correctly. 

 Case-folding is still only an approximation of the language-specific rules governing case. If the specific language is known, consider using ICUCollationKeyFilter and indexing collation keys instead. This implementation performs the "full" case-folding specified in the Unicode standard, and this may change the length of the term. For example, the German ß is case-folded to the string 'ss'. 

 Case folding is related to normalization, and as such is coupled with it in this integration. To perform case-folding, you use normalization with the form "nfkc_cf" (which is the default). 

## Use Cases

*   As a more thorough replacement for LowerCaseFilter that has good behavior
    for most languages.

## Example Usages

### Lowercasing text

      /**
       * This filter will case-fold and normalize to NFKC.
       */
      TokenStream tokenstream = new ICUNormalizer2Filter(tokenizer);

* * *

# [Search Term Folding]()

 Search term folding removes distinctions (such as accent marks) between similar characters. It is useful for a fuzzy or loose search. 

 Search term folding implements many of the foldings specified in [Character Foldings](http://www.unicode.org/reports/tr30/tr30-4.html) as a special normalization form. This folding applies NFKC, Case Folding, and many character foldings recursively. 

## Use Cases

*   As a more thorough replacement for ASCIIFoldingFilter and LowerCaseFilter 
    that applies the same ideas to many more languages. 

## Example Usages

### Removing accents

      /**
       * This filter will case-fold, remove accents and other distinctions, and
       * normalize to NFKC.
       */
      TokenStream tokenstream = new ICUFoldingFilter(tokenizer);

* * *

# [Text Transformation]()

 ICU provides text-transformation functionality via its Transliteration API. This allows you to transform text in a variety of ways, taking context into account. 

 For more information, see the [User's Guide](http://userguide.icu-project.org/transforms/general) and [Rule Tutorial](http://userguide.icu-project.org/transforms/general/rules). 

## Use Cases

*   Convert Traditional to Simplified 

*   Transliterate between different writing systems: e.g. Romanization

## Example Usages

### Convert Traditional to Simplified

      /**
       * This filter will map Traditional Chinese to Simplified Chinese
       */
      TokenStream tokenstream = new ICUTransformFilter(tokenizer, Transliterator.getInstance("Traditional-Simplified"));

### Transliterate Serbian Cyrillic to Serbian Latin

      /**
       * This filter will map Serbian Cyrillic to Serbian Latin according to BGN rules
       */
      TokenStream tokenstream = new ICUTransformFilter(tokenizer, Transliterator.getInstance("Serbian-Latin/BGN"));

* * *

# [Backwards Compatibility]()

 This module exists to provide up-to-date Unicode functionality that supports the most recent version of Unicode (currently 6.3). However, some users who wish for stronger backwards compatibility can restrict [](xref:Lucene.Net.Analysis.Icu.ICUNormalizer2Filter) to operate on only a specific Unicode Version by using a {@link com.ibm.icu.text.FilteredNormalizer2}. 

## Example Usages

### Restricting normalization to Unicode 5.0

      /**
       * This filter will do NFC normalization, but will ignore any characters that
       * did not exist as of Unicode 5.0. Because of the normalization stability policy
       * of Unicode, this is an easy way to force normalization to a specific version.
       */
        Normalizer2 normalizer = Normalizer2.getInstance(null, "nfc", Normalizer2.Mode.COMPOSE);
        UnicodeSet set = new UnicodeSet("[:age=5.0:]");
        // see FilteredNormalizer2 docs, the set should be frozen or performance will suffer
        set.freeze(); 
        FilteredNormalizer2 unicode50 = new FilteredNormalizer2(normalizer, set);
        TokenStream tokenstream = new ICUNormalizer2Filter(tokenizer, unicode50);