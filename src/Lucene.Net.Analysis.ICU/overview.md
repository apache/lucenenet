---
uid: Lucene.Net.Analysis.Icu
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
<!-- :Post-Release-Update-Version.LUCENE_XY: - several mentions in this file -->

This module exposes functionality from 
[ICU](http://site.icu-project.org/) to Apache Lucene. ICU4N is a .NET
library that enhances .NET's internationalization support by improving 
performance, keeping current with the Unicode Standard, and providing richer
APIs.

> [!NOTE]
> The <xref:Lucene.Net.Analysis.Icu> namespace was ported from Lucene 7.1.0 to get a more up-to-date version of Unicode than what shipped with Lucene 4.8.0.

> [!NOTE]
> Since the .NET platform doesn't provide a BreakIterator class (or similar), the functionality that utilizes it was consolidated from Java Lucene's analyzers-icu package, <xref:Lucene.Net.Analysis.Common> and <xref:Lucene.Net.Highlighter> into this unified package.
> [!WARNING]
> While ICU4N's BreakIterator has customizable rules, its default behavior is not the same as the one in the JDK. When using any features of this package outside of the <xref:Lucene.Net.Analysis.Icu> namespace, they will behave differently than they do in Java Lucene and the rules may need some tweaking to fit your needs. See the [Break Rules](http://userguide.icu-project.org/boundaryanalysis/break-rules) ICU documentation for details on how to customize `ICU4N.Text.RuleBaseBreakIterator`.



For an introduction to Lucene's analysis API, see the <xref:Lucene.Net.Analysis> package documentation.

 This module exposes the following functionality: 

*   [Text Segmentation](#text-segmentation): Tokenizes text based on 
  properties and rules defined in Unicode.

*   [Collation](#collation): Compare strings according to the 
  conventions and standards of a particular language, region or country.

*   [Normalization](#normalization): Converts text to a unique,
  equivalent form.

*   [Case Folding](#case-folding): Removes case distinctions with
  Unicode's Default Caseless Matching algorithm.

*   [Search Term Folding](#search-term-folding): Removes distinctions
  (such as accent marks) between similar characters for a loose or fuzzy search.

*   [Text Transformation](#text-transform): Transforms Unicode text in
  a context-sensitive fashion: e.g. mapping Traditional to Simplified Chinese

* * *

# Text Segmentation

 Text Segmentation (Tokenization) divides document and query text into index terms (typically words). Unicode provides special properties and rules so that this can be done in a manner that works well with most languages. 

 Text Segmentation implements the word segmentation specified in [Unicode Text Segmentation](http://unicode.org/reports/tr29/). Additionally the algorithm can be tailored based on writing system, for example text in the Thai script is automatically delegated to a dictionary-based segmentation algorithm. 

## Use Cases

*   As a more thorough replacement for StandardTokenizer that works well for
    most languages. 

## Example Usages

### Tokenizing multilanguage text

```cs
// This tokenizer will work well in general for most languages.
Tokenizer tokenizer = new ICUTokenizer(reader);
```

* * *

# Collation

 <xref:Lucene.Net.Collation.ICUCollationKeyAnalyzer> converts each token into its binary `CollationKey` using the provided `Collator`, allowing it to be stored as an index term. 

 <xref:Lucene.Net.Collation.ICUCollationKeyAnalyzer> depends on ICU4N to produce the `CollationKey`s. 

## Use Cases

*   Efficient sorting of terms in languages that use non-Unicode character 
    orderings.  (Lucene Sort using a CultureInfo can be very slow.) 

*   Efficient range queries over fields that contain terms in languages that 
    use non-Unicode character orderings.  (Range queries using a CultureInfo can be
    very slow.)

*   Effective Locale-specific normalization (case differences, diacritics, etc.).
    (<xref:Lucene.Net.Analysis.Core.LowerCaseFilter> and 
    <xref:Lucene.Net.Analysis.Miscellaneous.ASCIIFoldingFilter> provide these services
    in a generic way that doesn't take into account locale-specific needs.)

## Example Usages

### Farsi Range Queries

```cs
const LuceneVersion matchVersion = LuceneVersion.LUCENE_48;
Collator collator = Collator.GetInstance(new UCultureInfo("ar"));
ICUCollationKeyAnalyzer analyzer = new ICUCollationKeyAnalyzer(matchVersion, collator);
RAMDirectory ramDir = new RAMDirectory();
using IndexWriter writer = new IndexWriter(ramDir, new IndexWriterConfig(matchVersion, analyzer));
writer.AddDocument(new Document {
    new TextField("content", "\u0633\u0627\u0628", Field.Store.YES)
});
using IndexReader reader = writer.GetReader(applyAllDeletes: true);
writer.Dispose();
IndexSearcher searcher = new IndexSearcher(reader);

QueryParser queryParser = new QueryParser(matchVersion, "content", analyzer)
{
    AnalyzeRangeTerms = true
};

// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
// orders the U+0698 character before the U+0633 character, so the single
// indexed Term above should NOT be returned by a ConstantScoreRangeQuery
// with a Farsi Collator (or an Arabic one for the case when Farsi is not
// supported).
ScoreDoc[] result = searcher.Search(queryParser.Parse("[ \u062F TO \u0698 ]"), null, 1000).ScoreDocs;
Assert.AreEqual(0, result.Length, "The index Term should not be included.");
```

### Danish Sorting

```cs
const LuceneVersion matchVersion = LuceneVersion.LUCENE_48;
Analyzer analyzer = new ICUCollationKeyAnalyzer(matchVersion, Collator.GetInstance(new UCultureInfo("da-dk")));
string indexPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
Directory dir = FSDirectory.Open(indexPath);
using IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(matchVersion, analyzer));
string[] tracer = new string[] { "A", "B", "C", "D", "E" };
string[] data = new string[] { "HAT", "HUT", "H\u00C5T", "H\u00D8T", "HOT" };
string[] sortedTracerOrder = new string[] { "A", "E", "B", "D", "C" };
for (int i = 0; i < data.Length; ++i)
{
    writer.AddDocument(new Document
    {
        new StringField("tracer", tracer[i], Field.Store.YES),
        new TextField("contents", data[i], Field.Store.NO)
    });
}
using IndexReader reader = writer.GetReader(applyAllDeletes: true);
writer.Dispose();
IndexSearcher searcher = new IndexSearcher(reader);
Sort sort = new Sort();
sort.SetSort(new SortField("contents",  SortFieldType.STRING));
Query query = new MatchAllDocsQuery();
ScoreDoc[] result = searcher.Search(query, null, 1000, sort).ScoreDocs;
for (int i = 0; i < result.Length; ++i)
{
    Document doc = searcher.Doc(result[i].Doc);
    Assert.AreEqual(sortedTracerOrder[i], doc.GetValues("tracer")[0]);
}
```

### Turkish Case Normalization

```cs
const LuceneVersion matchVersion = LuceneVersion.LUCENE_48;
Collator collator = Collator.GetInstance(new UCultureInfo("tr-TR"));
collator.Strength = CollationStrength.Primary;
Analyzer analyzer = new ICUCollationKeyAnalyzer(matchVersion, collator);
string indexPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
Directory dir = FSDirectory.Open(indexPath);
using IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(matchVersion, analyzer));
writer.AddDocument(new Document {
    new TextField("contents", "DIGY", Field.Store.NO)
});
using IndexReader reader = writer.GetReader(applyAllDeletes: true);
writer.Dispose();
IndexSearcher searcher = new IndexSearcher(reader);
QueryParser parser = new QueryParser(matchVersion, "contents", analyzer);
Query query = parser.Parse("d\u0131gy");   // U+0131: dotless i
ScoreDoc[] result = searcher.Search(query, null, 1000).ScoreDocs;
Assert.AreEqual(1, result.Length, "The index Term should be included.");
```

## Caveats and Comparisons

 `ICUCollationKeyAnalyzer` uses ICU4N's `Collator`, which makes its version available, thus allowing collation to be versioned independently from the .NET target framework. `ICUCollationKeyAnalyzer` is also fast. 

 `SortKey`s generated by `CompareInfo`s are not compatible with those those generated by ICU Collators. Specifically, if you use `CollationKeyAnalyzer` to generate index terms, do not use `ICUCollationKeyAnalyzer` on the query side, or vice versa. 

* * *

# Normalization

 <xref:Lucene.Net.Analysis.Icu.ICUNormalizer2Filter> normalizes term text to a [Unicode Normalization Form](http://unicode.org/reports/tr15/), so that [equivalent](http://en.wikipedia.org/wiki/Unicode_equivalence) forms are standardized to a unique form. 

## Use Cases

*   Removing differences in width for Asian-language text. 

*   Standardizing complex text with non-spacing marks so that characters are 
  ordered consistently.

## Example Usages

### Normalizing text to NFC

```cs
// Normalizer2 objects are unmodifiable and immutable.
Normalizer2 normalizer = Normalizer2.GetInstance(null, "nfc", Normalizer2Mode.Compose);
// This filter will normalize to NFC.
TokenStream tokenstream = new ICUNormalizer2Filter(tokenizer, normalizer);
```

* * *

# Case Folding

 Default caseless matching, or case-folding is more than just conversion to lowercase. For example, it handles cases such as the Greek sigma, so that "Μάϊος" and "ΜΆΪΟΣ" will match correctly. 

 Case-folding is still only an approximation of the language-specific rules governing case. If the specific language is known, consider using ICUCollationKeyFilter and indexing collation keys instead. This implementation performs the "full" case-folding specified in the Unicode standard, and this may change the length of the term. For example, the German ß is case-folded to the string 'ss'. 

 Case folding is related to normalization, and as such is coupled with it in this integration. To perform case-folding, you use normalization with the form "nfkc_cf" (which is the default). 

## Use Cases

*   As a more thorough replacement for LowerCaseFilter that has good behavior
    for most languages.

## Example Usages

### Lowercasing text

```cs
// This filter will case-fold and normalize to NFKC.
TokenStream tokenstream = new ICUNormalizer2Filter(tokenizer);
```

* * *

# Search Term Folding

 Search term folding removes distinctions (such as accent marks) between similar characters. It is useful for a fuzzy or loose search. 

 Search term folding implements many of the foldings specified in [Character Foldings](http://www.unicode.org/reports/tr30/tr30-4.html) as a special normalization form. This folding applies NFKC, Case Folding, and many character foldings recursively. 

## Use Cases

*   As a more thorough replacement for ASCIIFoldingFilter and LowerCaseFilter 
    that applies the same ideas to many more languages. 

## Example Usages

### Removing accents

```cs
// This filter will case-fold, remove accents and other distinctions, and
// normalize to NFKC.
TokenStream tokenstream = new ICUFoldingFilter(tokenizer);
```

* * *

# Text Transformation

 ICU provides text-transformation functionality via its Transliteration API. This allows you to transform text in a variety of ways, taking context into account. 

 For more information, see the [User's Guide](http://userguide.icu-project.org/transforms/general) and [Rule Tutorial](http://userguide.icu-project.org/transforms/general/rules). 

## Use Cases

*   Convert Traditional to Simplified 

*   Transliterate between different writing systems: e.g. Romanization

## Example Usages

### Convert Traditional to Simplified

```cs
// This filter will map Traditional Chinese to Simplified Chinese
TokenStream tokenstream = new ICUTransformFilter(tokenizer, Transliterator.GetInstance("Traditional-Simplified"));
```

### Transliterate Serbian Cyrillic to Serbian Latin

```cs
// This filter will map Serbian Cyrillic to Serbian Latin according to BGN rules
TokenStream tokenstream = new ICUTransformFilter(tokenizer, Transliterator.GetInstance("Serbian-Latin/BGN"));
```

* * *

# Backwards Compatibility

 This module exists to provide up-to-date Unicode functionality that supports the most recent version of Unicode (currently 8.0). However, some users who wish for stronger backwards compatibility can restrict <xref:Lucene.Net.Analysis.Icu.ICUNormalizer2Filter> to operate on only a specific Unicode Version by using a FilteredNormalizer2. 

## Example Usages

### Restricting normalization to Unicode 5.0

```cs
// This filter will do NFC normalization, but will ignore any characters that
// did not exist as of Unicode 5.0. Because of the normalization stability policy
// of Unicode, this is an easy way to force normalization to a specific version.
Normalizer2 normalizer = Normalizer2.GetInstance(null, "nfc", Normalizer2Mode.Compose);
UnicodeSet set = new UnicodeSet("[:age=5.0:]");
// see FilteredNormalizer2 docs, the set should be frozen or performance will suffer
set.Freeze();
FilteredNormalizer2 unicode50 = new FilteredNormalizer2(normalizer, set);
TokenStream tokenstream = new ICUNormalizer2Filter(tokenizer, unicode50);
```