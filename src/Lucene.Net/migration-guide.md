---
uid: Lucene.Net.Migration.Guide
title: Apache Lucene.NET 4.8.0 Migration Guide
description: The Migration Guide for Lucene.NET 4.8.0
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

# Apache Lucene.NET 4.8.0 Migration Guide

## .NET API Conventions

Several Java conventions were replaced with their .NET counterparts:

* Classes suffixed with `Comparator` are now suffixed with `Comparer`.

* Most iterator classes were converted to .NET enumerators.

  * Instead of `Iterator()`, call `GetEnumerator()` (in some cases, it may be `GetIterator()`).

  * Instead of `HasNext()`, call `MoveNext()` however note that this will advance the position of the enumerator.

  * Instead of `Next()` the return value can be retrieved from the `Current` property after calling `MoveNext()`.

* Classes and members that include numeric type names now use the language-agnostic .NET name. For example:

  * Instead of `Short` or `GetShort()` use `Int16` or `GetInt16()`.

  * Instead of `Integer` or `GetInteger()` use `Int32` or `GetInt32()`.

  * Instead of `Long` or `GetLong()` use `Int64` or `GetInt64()`.

  * Instead of `Float` use `Single`. Note that `Lucene.Net.Queries.Function.ValueSources.SingleFunction` was renamed `Lucene.Net.Queries.Function.ValueSources.SingularFunction` to distinguish it from the `System.Single` data type.

* For collections, the `Size` property is now named `Count`.

* For arrays and files, the `Size` property is now named `Length`.

* For `IndexInput` and `IndexOutput` subclasses, `GetFilePointer()` method has been changed to a `Position` property to match `System.IO.FileStream.Position`.

* Some classes, enums, and interfaces have been de-nested from their original Lucene location to make them easier to find when using Intellisense.

* Some methods were lacking a verb, so the verb `Get` was added to make the method's function more clear. For example, instead of `Analysis.TokenStream()` we now have `Analysis.GetTokenStream()`.

## Four-dimensional enumerations

Flexible indexing changed the low level fields/terms/docs/positions
enumeration APIs.  Here are the major changes:

* Terms are now binary in nature (arbitrary `byte[]`), represented
  by the `BytesRef` class (which provides an offset + length "slice"
  into an existing `byte[]`).

* Fields are separately enumerated (`Fields.GetEnumerator()`) from the terms
  within each field (`TermEnum`).  So instead of this:
    ```cs
    TermEnum termsEnum = ...;
    while (termsEnum.Next())
    {
        Term t = termsEnum.Term;
        Console.WriteLine("field=" + t.Field + "; text=" + t.Text);
    }
    ```
    Do this:
    ```cs
    foreach (string field in fields)
    {
        Terms terms = fields.GetTerms(field);
        TermsEnum termsEnum = terms.GetEnumerator();
        BytesRef text;
        while(termsEnum.MoveNext())
        {
            Console.WriteLine("field=" + field + "; text=" + termsEnum.Current.Utf8ToString());
        }
    }
    ```

* `TermDocs` is renamed to `DocsEnum`.  Instead of this:
    ```cs
    while (td.Next())
    {
        int doc = td.Doc;
        ...
    }
    ```
    do this:
    ```cs
    int doc;
    while ((doc = td.Next()) != DocsEnum.NO_MORE_DOCS)
    {
        ...
    }
    ```
    Instead of this:
    ```cs
    if (td.SkipTo(target))
    {
        int doc = td.Doc;
        ...
    }
    ```
    do this:
    ```cs
    if ((doc = td.Advance(target)) != DocsEnum.NO_MORE_DOCS)
    {
        ...
    }
    ```

* `TermPositions` is renamed to `DocsAndPositionsEnum`, and no longer
  extends the docs only enumerator (`DocsEnum`).

* Deleted docs are no longer implicitly filtered from
  docs/positions enums.  Instead, you pass a `IBits`
  `SkipDocs` (set bits are skipped) when obtaining the enums.  Also,
  you can now ask a reader for its deleted docs.

* The docs/positions enums cannot seek to a term.  Instead,
  `TermsEnum` is able to seek, and then you request the
  docs/positions enum from that `TermsEnum`.

* `TermsEnum`'s seek method returns more information.  So instead of this:
    ```cs
    Term t;
    TermEnum termEnum = reader.Terms(t);
    if (t.Equals(termEnum.Term))
    {
        ...
    }
    ```
    do this:
    ```cs
    TermsEnum termsEnum = ...;
    BytesRef text;
    if (termsEnum.Seek(text) == TermsEnum.SeekStatus.FOUND)
    {
        ...
    }
    ```
    `SeekStatus` also contains `END` (enumerator is done) and `NOT_FOUND` (term was not found but enumerator is now positioned to the next term).

* `TermsEnum` has an `Ord` property, returning the long numeric
  ordinal (ie, first term is 0, next is 1, and so on) for the term
  it's not positioned to.  There is also a corresponding Seek(long
  ord) method.  Note that these members are optional; in
  particular the `MultiFields` `TermsEnum` does not implement them.

* How you obtain the enums has changed.  The primary entry point is
  the `Fields` class.  If you know your reader is a single segment
  reader, do this:
    ```cs
    Fields fields = reader.Fields();
    if (fields != null)
    {
        ...
    }
    ```
    If the reader might be multi-segment, you must do this:
    ```cs
    Fields fields = MultiFields.GetFields(reader);
    if (fields != null)
    {
        ...
    }
    ```
    The fields may be `null` (eg if the reader has no fields).<br/>
    Note that the `MultiFields` approach entails a performance hit on `MultiReaders`, as it must merge terms/docs/positions on the fly. It's generally better to instead get the sequential readers (use `Lucene.Net.Util.ReaderUtil`) and then step through those readers yourself, if you can (this is how Lucene drives searches).<br/>
    If you pass a `SegmentReader` to `MultiFields.GetFields()` it will simply return `reader.GetFields()`, so there is no performance hit in that case.<br/>
    Once you have a non-null `Fields` you can do this:
    ```cs
    Terms terms = fields.GetTerms("field");
    if (terms != null)
    {
        ...
    }
    ```
    The terms may be `null` (eg if the field does not exist).<br/>
    Once you have a non-null terms you can get an enum like this:
    ```cs
    TermsEnum termsEnum = terms.GetIterator();
    ```
    The returned `TermsEnum` will not be `null`.<br/>
    You can then .Next() through the TermsEnum, or Seek.  If you want a `DocsEnum`, do this:
    ```cs
    IBits liveDocs = reader.GetLiveDocs();
    DocsEnum docsEnum = null;

    docsEnum = termsEnum.Docs(liveDocs, docsEnum, needsFreqs);
    ```
    You can pass in a prior `DocsEnum` and it will be reused if possible.<br/>
    Likewise for `DocsAndPositionsEnum`.<br/>
    `IndexReader` has several sugar methods (which just go through the above steps, under the hood).  Instead of:
    ```cs
    Term t;
    TermDocs termDocs = reader.TermDocs;
    termDocs.Seek(t);
    ```
    do this:
    ```cs
    Term t;
    DocsEnum docsEnum = reader.GetTermDocsEnum(t);
    ```
    Likewise for `DocsAndPositionsEnum`.

## [LUCENE-2380](https://issues.apache.org/jira/browse/LUCENE-2380): FieldCache.GetStrings/Index --> FieldCache.GetDocTerms/Index

* The field values returned when sorting by `SortField.STRING` are now
  `BytesRef`.  You can call `value.Utf8ToString()` to convert back to
  string, if necessary.

* In `FieldCache`, `GetStrings` (returning `string[]`) has been replaced
  with `GetTerms` (returning a `BinaryDocValues` instance).
  `BinaryDocValues` provides a `Get` method, taking a `docID` and a `BytesRef`
  to fill (which must not be `null`), and it fills it in with the
  reference to the bytes for that term.<br/>
    If you had code like this before:
    ```cs
    string[] values = FieldCache.DEFAULT.GetStrings(reader, field);
    ...
    string aValue = values[docID];
    ```
    you can do this instead:
    ```cs
    BinaryDocValues values = FieldCache.DEFAULT.GetTerms(reader, field);
    ...
    BytesRef term = new BytesRef();
    values.Get(docID, term);
    string aValue = term.Utf8ToString();
    ```
    Note however that it can be costly to convert to `String`, so it's better to work directly with the `BytesRef`.

* Similarly, in `FieldCache`, GetStringIndex (returning a `StringIndex`
  instance, with direct arrays `int[]` order and `String[]` lookup) has
  been replaced with `GetTermsIndex` (returning a
  `SortedDocValues` instance).  `SortedDocValues` provides the
  `GetOrd(int docID)` method to lookup the int order for a document,
  `LookupOrd(int ord, BytesRef result)` to lookup the term from a given
  order, and the sugar method `Get(int docID, BytesRef result)`
  which internally calls `GetOrd` and then `LookupOrd`.<br/>
    If you had code like this before:
    ```cs
    StringIndex idx = FieldCache.DEFAULT.GetStringIndex(reader, field);
    ...
    int ord = idx.order[docID];
    String aValue = idx.lookup[ord];
    ```
    you can do this instead:
    ```cs
    DocTermsIndex idx = FieldCache.DEFAULT.GetTermsIndex(reader, field);
    ...
    int ord = idx.GetOrd(docID);
    BytesRef term = new BytesRef();
    idx.LookupOrd(ord, term);
    string aValue = term.Utf8ToString();
    ```
    Note however that it can be costly to convert to `String`, so it's better to work directly with the `BytesRef`.<br/>
    `DocTermsIndex` also has a `GetTermsEnum()` method, which returns an iterator (`TermsEnum`) over the term values in the index (ie, iterates ord = 0..NumOrd-1).

* `FieldComparator.StringComparatorLocale` has been removed.
  (it was very CPU costly since it does not compare using
  indexed collation keys; use CollationKeyFilter for better
  performance), since it converts `BytesRef` -> `String` on the fly.

* `FieldComparator.StringOrdValComparator` has been renamed to
  `FieldComparer.TermOrdValComparer`, and now uses `BytesRef` for its values.
  Likewise for `StringValComparator`, renamed to `TermValComparer`.
  This means when sorting by `SortField.STRING` or
  `SortField.STRING_VAL` (or directly invoking these comparers) the
  values returned in the `FieldDoc.Fields` array will be `BytesRef` not
  `String`.  You can call the `.Utf8ToString()` method on the `BytesRef`
  instances, if necessary.

## [LUCENE-2600](https://issues.apache.org/jira/browse/LUCENE-2600): `IndexReader`s are now read-only

Instead of `IndexReader.IsDeleted(int n)`, do this:

```cs
using Lucene.Net.Util;
using Lucene.Net.Index;

IBits liveDocs = MultiFields.GetLiveDocs(indexReader);
if (liveDocs != null && !liveDocs.Get(docID))
{
    // document is deleted...
}
```
    
## [LUCENE-2858](https://issues.apache.org/jira/browse/LUCENE-2858), [LUCENE-3733](https://issues.apache.org/jira/browse/LUCENE-3733): `IndexReader` --> `AtomicReader`/`CompositeReader`/`DirectoryReader` refactoring

The abstract class `IndexReader` has been 
refactored to expose only essential methods to access stored fields 
during display of search results. It is no longer possible to retrieve 
terms or postings data from the underlying index, not even deletions are 
visible anymore. You can still pass `IndexReader` as constructor parameter 
to `IndexSearcher` and execute your searches; Lucene will automatically 
delegate procedures like query rewriting and document collection atomic 
subreaders. 

If you want to dive deeper into the index and want to write own queries, 
take a closer look at the new abstract sub-classes `AtomicReader` and 
`CompositeReader`: 

`AtomicReader` instances are now the only source of `Terms`, `Postings`, 
`DocValues` and `FieldCache`. Queries are forced to execute on an `AtomicReader` on a per-segment basis and `FieldCache`s are keyed by 
`AtomicReader`s. 

Its counterpart `CompositeReader` exposes a utility method to retrieve 
its composites. But watch out, composites are not necessarily atomic. 
Next to the added type-safety we also removed the notion of 
index-commits and version numbers from the abstract `IndexReader`, the 
associations with `IndexWriter` were pulled into a specialized 
`DirectoryReader`. To open `Directory`-based indexes use 
`DirectoryReader.Open()`, the corresponding method in `IndexReader` is now 
deprecated for easier migration. Only `DirectoryReader` supports commits, 
versions, and reopening with `OpenIfChanged()`. Terms, postings, 
docvalues, and norms can from now on only be retrieved using 
`AtomicReader`; `DirectoryReader` and `MultiReader` extend `CompositeReader`, 
only offering stored fields and access to the sub-readers (which may be 
composite or atomic). 

If you have more advanced code dealing with custom `Filter`s, you might 
have noticed another new class hierarchy in Lucene (see [LUCENE-2831](https://issues.apache.org/jira/browse/LUCENE-2831)): 
`IndexReaderContext` with corresponding Atomic-/`CompositeReaderContext`. 

The move towards per-segment search Lucene 2.9 exposed lots of custom 
`Query`s and `Filter`s that couldn't handle it. For example, some `Filter` 
implementations expected the `IndexReader` passed in is identical to the 
`IndexReader` passed to `IndexSearcher` with all its advantages like 
absolute document IDs etc. Obviously this "paradigm-shift" broke lots of 
applications and especially those that utilized cross-segment data 
structures (like Apache Solr). 

In Lucene 4.0, we introduce `IndexReaderContext`s "searcher-private" 
reader hierarchy. During `Query` or `Filter` execution Lucene no longer 
passes raw readers down `Query`s, `Filter`s or `Collector`s; instead 
components are provided an `AtomicReaderContext` (essentially a hierarchy 
leaf) holding relative properties like the document-basis in relation to 
the top-level reader. This allows `Query`s and `Filter` to build up logic 
based on document IDs, albeit the per-segment orientation. 

There are still valid use-cases where top-level readers ie. "atomic 
views" on the index are desirable. Let say you want to iterate all terms 
of a complete index for auto-completion or faceting, Lucene provides
utility wrappers like `SlowCompositeReaderWrapper` ([LUCENE-2597](https://issues.apache.org/jira/browse/LUCENE-2597)) emulating 
an `AtomicReader`. Note: using "atomicity emulators" can cause serious 
slowdowns due to the need to merge terms, postings, `DocValues`, and 
`FieldCache`, use them with care! 

## [LUCENE-4306](https://issues.apache.org/jira/browse/LUCENE-4306): `GetSequentialSubReaders()`, `ReaderUtil.Gather()`

The method `IndexReader.GetSequentialSubReaders()` was moved to `CompositeReader`
(see [LUCENE-2858](https://issues.apache.org/jira/browse/LUCENE-2858), [LUCENE-3733](https://issues.apache.org/jira/browse/LUCENE-3733)) and made protected. It is solely used by `CompositeReader` itself to build its reader tree. To get all atomic leaves
of a reader, use `IndexReader.Leaves`, which also provides the doc base
of each leave. Readers that are already atomic return itself as leaf with
doc base 0. To emulate Lucene 3.x `GetSequentialSubReaders()`,
use `Context.Children`.

## [LUCENE-2413](https://issues.apache.org/jira/browse/LUCENE-2413),[LUCENE-3396](https://issues.apache.org/jira/browse/LUCENE-3396): Analyzer package changes Lucene's core and contrib analyzers, along with Solr's analyzers,
were consolidated into lucene/analysis. During the refactoring some
package names have changed, and `ReusableAnalyzerBase` was renamed to
`Analyzer`:

  - `Lucene.Net.Analysis.KeywordAnalyzer` -> `Lucene.Net.Analysis.Core.KeywordAnalyzer`
  - `Lucene.Net.Analysis.KeywordTokenizer` -> `Lucene.Net.Analysis.Core.KeywordTokenizer`
  - `Lucene.Net.Analysis.LetterTokenizer` -> `Lucene.Net.Analysis.Core.LetterTokenizer`
  - `Lucene.Net.Analysis.LowerCaseFilter` -> `Lucene.Net.Analysis.Core.LowerCaseFilter`
  - `Lucene.Net.Analysis.LowerCaseTokenizer` -> `Lucene.Net.Analysis.Core.LowerCaseTokenizer`
  - `Lucene.Net.Analysis.SimpleAnalyzer` -> `Lucene.Net.Analysis.Core.SimpleAnalyzer`
  - `Lucene.Net.Analysis.StopAnalyzer` -> `Lucene.Net.Analysis.Core.StopAnalyzer`
  - `Lucene.Net.Analysis.StopFilter` -> `Lucene.Net.Analysis.Core.StopFilter`
  - `Lucene.Net.Analysis.WhitespaceAnalyzer` -> `Lucene.Net.Analysis.Core.WhitespaceAnalyzer`
  - `Lucene.Net.Analysis.WhitespaceTokenizer` -> `Lucene.Net.Analysis.Core.WhitespaceTokenizer`
  - `Lucene.Net.Analysis.PorterStemFilter` -> `Lucene.Net.Analysis.En.PorterStemFilter`
  - `Lucene.Net.Analysis.ASCIIFoldingFilter` -> `Lucene.Net.Analysis.Miscellaneous.ASCIIFoldingFilter`
  - `Lucene.Net.Analysis.ISOLatin1AccentFilter` -> `Lucene.Net.Analysis.Miscellaneous.ISOLatin1AccentFilter`
  - `Lucene.Net.Analysis.KeywordMarkerFilter` -> `Lucene.Net.Analysis.Miscellaneous.KeywordMarkerFilter`
  - `Lucene.Net.Analysis.LengthFilter` -> `Lucene.Net.Analysis.Miscellaneous.LengthFilter`
  - `Lucene.Net.Analysis.PerFieldAnalyzerWrapper` -> `Lucene.Net.Analysis.Miscellaneous.PerFieldAnalyzerWrapper`
  - `Lucene.Net.Analysis.TeeSinkTokenFilter` -> `Lucene.Net.Analysis.Sinks.TeeSinkTokenFilter`
  - `Lucene.Net.Analysis.CharFilter` -> `Lucene.Net.Analysis.CharFilter.CharFilter`
  - `Lucene.Net.Analysis.BaseCharFilter` -> `Lucene.Net.Analysis.CharFilter.BaseCharFilter`
  - `Lucene.Net.Analysis.MappingCharFilter` -> `Lucene.Net.Analysis.CharFilter.MappingCharFilter`
  - `Lucene.Net.Analysis.NormalizeCharMap` -> `Lucene.Net.Analysis.CharFilter.NormalizeCharMap`
  - `Lucene.Net.Analysis.CharArraySet` -> `Lucene.Net.Analysis.Util.CharArraySet`
  - `Lucene.Net.Analysis.CharArrayMap` -> `Lucene.Net.Analysis.Util.CharArrayMap`
  - `Lucene.Net.Analysis.ReusableAnalyzerBase` -> `Lucene.Net.Analysis.Analyzer`
  - `Lucene.Net.Analysis.StopwordAnalyzerBase` -> `Lucene.Net.Analysis.Util.StopwordAnalyzerBase`
  - `Lucene.Net.Analysis.WordListLoader` -> `Lucene.Net.Analysis.Util.WordListLoader`
  - `Lucene.Net.Analysis.CharTokenizer` -> `Lucene.Net.Analysis.Util.CharTokenizer`
  - `Lucene.Net.Util.CharacterUtils` -> `Lucene.Net.Analysis.Util.CharacterUtils`

## [LUCENE-2514](https://issues.apache.org/jira/browse/LUCENE-2514): Collators

The option to use a Collator's order (instead of binary order) for
sorting and range queries has been moved to lucene/queries.
The Collated TermRangeQuery/Filter has been moved to SlowCollatedTermRangeQuery/Filter, 
and the collated sorting has been moved to `SlowCollatedStringComparer`.

Note: this functionality isn't very scalable and if you are using it, consider 
indexing collation keys with the collation support in the analysis module instead.

To perform collated range queries, use the collating analyzer: `ICUCollationKeyAnalyzer`, and set `qp.AnalyzeRangeTerms = true`.

`TermRangeQuery` and `TermRangeFilter` now work purely on bytes. Both have helper factory methods
(`NewStringRange`) similar to the `NumericRange` API, to easily perform range queries on `String`s.

## [LUCENE-2883](https://issues.apache.org/jira/browse/LUCENE-2883): `ValueSource` changes

Lucene's `Lucene.Net.Search.Function.ValueSource` based functionality, was consolidated
into `Lucene.Net`/`Lucene.Net.Queries` along with Solr's similar functionality.  The following classes were moved:

 - `Lucene.Net.Search.Function.CustomScoreQuery` -> `Lucene.Net.Queries.CustomScoreQuery`
 - `Lucene.Net.Search.Function.CustomScoreProvider` -> `Lucene.Net.Queries.CustomScoreProvider`
 - `Lucene.Net.Search.Function.NumericIndexDocValueSource` -> `Lucene.Net.Queries.Function.ValueSource.NumericIndexDocValueSource`

The following lists the replacement classes for those removed:

 - `Lucene.Net.Search.Function.DocValues` -> `Lucene.Net.Queries.Function.DocValues`
 - `Lucene.Net.Search.Function.FieldCacheSource` -> `Lucene.Net.Queries.Function.ValueSources.FieldCacheSource`
 - `Lucene.Net.Search.Function.FieldScoreQuery` ->`Lucene.Net.Queries.Function.FunctionQuery`
 - `Lucene.Net.Search.Function.FloatFieldSource` -> `Lucene.Net.Queries.Function.ValueSources.FloatFieldSource`
 - `Lucene.Net.Search.Function.IntFieldSource` -> `Lucene.Net.Queries.Function.ValueSources.IntFieldSource`
 - `Lucene.Net.Search.Function.OrdFieldSource` -> `Lucene.Net.Queries.Function.ValueSources.OrdFieldSource`
 - `Lucene.Net.Search.Function.ReverseOrdFieldSource` -> `Lucene.Net.Queries.Function.ValueSources.ReverseOrdFieldSource`
 - `Lucene.Net.Search.Function.ShortFieldSource` -> `Lucene.Net.Queries.Function.ValueSources.ShortFieldSource`
 - `Lucene.Net.Search.Function.ValueSource` -> `Lucene.Net.Queries.Function.ValueSource`
 - `Lucene.Net.Search.Function.ValueSourceQuery` -> `Lucene.Net.Queries.Function.FunctionQuery`

`DocValues` are now named `FunctionValues`, to not confuse with Lucene's per-document values.

## [LUCENE-2392](https://issues.apache.org/jira/browse/LUCENE-2392): Enable flexible scoring

The existing `Similarity` API is now `TFIDFSimilarity`, if you were extending
`Similarity` before, you should likely extend this instead.

`Weight.Normalize()` no longer takes a norm value that incorporates the top-level
boost from outer queries such as `BooleanQuery`, instead it takes 2 parameters,
the outer boost (`topLevelBoost`) and the norm. `Weight.SumOfSquaredWeights` has
been renamed to `Weight.GetValueForNormalization()`.

The `ScorePayload()` method now takes a `BytesRef`. It is never `null`.

## [LUCENE-3283](https://issues.apache.org/jira/browse/LUCENE-3283): Query parsers moved to separate module

Lucene's core `Lucene.Net.QueryParsers` query parsers have been consolidated into lucene/queryparser,
where other `QueryParser`s from the codebase will also be placed.  The following classes were moved:

  - `Lucene.Net.QueryParsers.CharStream` -> `Lucene.Net.QueryParsers.Classic.CharStream`
  - `Lucene.Net.QueryParsers.FastCharStream` -> `Lucene.Net.QueryParsers.Classic.FastCharStream`
  - `Lucene.Net.QueryParsers.MultiFieldQueryParser` -> `Lucene.Net.QueryParsers.Classic.MultiFieldQueryParser`
  - `Lucene.Net.QueryParsers.ParseException` -> `Lucene.Net.QueryParsers.Classic.ParseException`
  - `Lucene.Net.QueryParsers.QueryParser` -> `Lucene.Net.QueryParsers.Classic.QueryParser`
  - `Lucene.Net.QueryParsers.QueryParserBase` -> `Lucene.Net.QueryParsers.Classic.QueryParserBase`
  - `Lucene.Net.QueryParsers.QueryParserConstants` -> `Lucene.Net.QueryParsers.Classic.QueryParserConstants`
  - `Lucene.Net.QueryParsers.QueryParserTokenManager` -> `Lucene.Net.QueryParsers.Classic.QueryParserTokenManager`
  - `Lucene.Net.QueryParsers.QueryParserToken` -> `Lucene.Net.QueryParsers.Classic.Token`
  - `Lucene.Net.QueryParsers.QueryParserTokenMgrError` -> `Lucene.Net.QueryParsers.Classic.TokenMgrError`

## [LUCENE-2308](https://issues.apache.org/jira/browse/LUCENE-2308), [LUCENE-3453](https://issues.apache.org/jira/browse/LUCENE-3453): Separate `IndexableFieldType` from `Field` instances

With this change, the indexing details (indexed, tokenized, norms,
indexOptions, stored, etc.) are moved into a separate `FieldType`
instance (rather than being stored directly on the `Field`).

This means you can create the FieldType instance once, up front,
for a given field, and then re-use that instance whenever you instantiate
the Field.

Certain field types are pre-defined since they are common cases:

  * `StringField`: indexes a `String` value as a single token (ie, does
    not tokenize).  This field turns off norms and indexes only doc
    IDS (does not index term frequency nor positions).  This field
    does not store its value, but exposes `TYPE_STORED` as well.
  * `TextField`: indexes and tokenizes a `String`, `Reader` or `TokenStream`
    value, without term vectors.  This field does not store its value,
    but exposes `TYPE_STORED` as well.
  * `StoredField`: field that stores its value
  * `DocValuesField`: indexes the value as a `DocValues` field
  * `NumericField`: indexes the numeric value so that `NumericRangeQuery`
    can be used at search-time.

If your usage fits one of those common cases you can simply
instantiate the above class.  If you need to store the value, you can
add a separate `StoredField` to the document, or you can use
`TYPE_STORED` for the field:

```cs
Field f = new Field("field", "value", StringField.TYPE_STORED);
```

Alternatively, if an existing type is close to what you want but you
need to make a few changes, you can copy that type and make changes:

```cs
FieldType bodyType = new FieldType(TextField.TYPE_STORED)
{
    StoreTermVectors = true
};
```

You can of course also create your own `FieldType` from scratch:

```cs
FieldType t = new FieldType
{
    Indexed = true,
    Stored = true,
    OmitNorms = true,
    IndexOptions = IndexOptions.DOCS_AND_FREQS
};
t.Freeze();
```

`FieldType` has a `Freeze()` method to prevent further changes.

There is also a deprecated transition API, providing the same `Index`,
`Store`, `TermVector` enums from 3.x, and `Field` constructors taking these
enums.

When migrating from the 3.x API, if you did this before:

```cs
new Field("field", "value", Field.Store.NO, Field.Indexed.NOT_ANALYZED_NO_NORMS)
```

you can now do this:

```cs
new StringField("field", "value")
```

(though note that `StringField` indexes `DOCS_ONLY`).

If instead the value was stored:

```cs
new Field("field", "value", Field.Store.YES, Field.Indexed.NOT_ANALYZED_NO_NORMS)
```

you can now do this:

```cs
new Field("field", "value", TextField.TYPE_STORED)
```

If you didn't omit norms:

```cs
new Field("field", "value", Field.Store.YES, Field.Indexed.NOT_ANALYZED)
```

you can now do this:

```cs
FieldType ft = new FieldType(TextField.TYPE_STORED)
{
    OmitNorms = false
};
new Field("field", "value", ft)
```

If you did this before (value can be `String` or `TextReader`):

```cs
new Field("field", value, Field.Store.NO, Field.Indexed.ANALYZED)
```

you can now do this:

```cs
new TextField("field", value, Field.Store.NO)
```

If instead the value was stored:

```cs
new Field("field", value, Field.Store.YES, Field.Indexed.ANALYZED)
```

you can now do this:

```cs
new TextField("field", value, Field.Store.YES)
```

If in addition you omit norms:

```cs
new Field("field", value, Field.Store.YES, Field.Indexed.ANALYZED_NO_NORMS)
```

you can now do this:

```cs
FieldType ft = new FieldType(TextField.TYPE_STORED)
{
    OmitNorms = true
};
new Field("field", value, ft)
```

If you did this before (bytes is a `byte[]`):

```cs
new Field("field", bytes)
```

you can now do this:

```cs
new StoredField("field", bytes)
```

If you previously used the setter of `Document.Boost`, you must now pre-multiply
the document boost into each `Field.Boost`.  If you have a
multi-valued field, you should do this only for the first `Field`
instance (ie, subsequent Field instance sharing the same field name
should only include their per-field boost and not the document level
boost) as the boost for multi-valued field instances are multiplied
together by Lucene.

## Other changes

* [LUCENE-2674](https://issues.apache.org/jira/browse/LUCENE-2674):
  A new `IdfExplain` method was added to `Similarity` (which is now `TFIDFSimilarity`), that accepts an incoming docFreq.  If you subclass `TFIDFSimilarity`, make sure you also override this method on upgrade, otherwise your customizations won't run for certain `MultiTermQuery`s.

* [LUCENE-2691](https://issues.apache.org/jira/browse/LUCENE-2691): The near-real-time API has moved from `IndexWriter` to
  `DirectoryReader`.  Instead of `IndexWriter.GetReader()`, call
  `DirectoryReader.Open(IndexWriter)` or `DirectoryReader.OpenIfChanged(IndexWriter)`.

* [LUCENE-2690](https://issues.apache.org/jira/browse/LUCENE-2680): `MultiTermQuery` boolean rewrites per segment. Also `MultiTermQuery.GetTermsEnum()` now takes an `AttributeSource`. `FuzzyTermsEnum` is both consumer and producer of attributes: `MultiTermQuery.BoostAttribute` is added to the `FuzzyTermsEnum` and `MultiTermQuery`'s rewrite mode consumes it. The other way round `MultiTermQuery.TopTermsBooleanQueryRewrite` supplies a global `AttributeSource` to each segments `TermsEnum`. The `TermsEnum` is consumer and gets the current minimum competitive boosts (`MultiTermQuery.MaxNonCompetitiveBoostAttribute`).

* [LUCENE-2374](https://issues.apache.org/jira/browse/LUCENE-2374): The backwards layer in `Attribute` was removed. To support correct reflection of `Attribute` instances, where the reflection was done using deprecated `ToString()` parsing, you have to now override `ReflectWith()` to customize output. `ToString()` is no longer implemented by `Attribute`, so if you have overridden `ToString()`, port your customization over to `ReflectWith()`. `ReflectAsString()` would then return what `ToString()` did before.

* [LUCENE-2236](https://issues.apache.org/jira/browse/LUCENE-2236), [LUCENE-2912](https://issues.apache.org/jira/browse/LUCENE-2912): `DefaultSimilarity` can no longer be set statically 
  (and dangerously) for the entire `AppDomain`.
  `Similarity` can now be configured on a per-field basis (via `PerFieldSimilarityWrapper`)
  `Similarity` has a lower-level API, if you want the higher-level vector-space API
  like in previous Lucene releases, then look at `TFIDFSimilarity`.

* [LUCENE-1076](https://issues.apache.org/jira/browse/LUCENE-1076): `TieredMergePolicy` is now the default merge policy.
  It's able to merge non-contiguous segments; this may cause problems
  for applications that rely on Lucene's internal document ID
  assignment.  If so, you should instead use `LogByteSize`/`DocMergePolicy`
  during indexing.

* [LUCENE-3722](https://issues.apache.org/jira/browse/LUCENE-3722): `Similarity` methods and collection/term statistics now take
  `long` instead of `int` (to enable distributed scoring of > 2B docs). 
  For example, in `TFIDFSimilarity` `Idf(int, int)` is now `Idf(long, long)`. 

* [LUCENE-3559](https://issues.apache.org/jira/browse/LUCENE-3559): The members `DocFreq()` and `MaxDoc` on `IndexSearcher` were removed,
  as these are no longer used by the scoring system.
  If you were using these casually in your code for reasons unrelated to scoring,
  call them on the `IndexSearcher`'s reader instead: `IndexSearcher.IndexReader`.
  If you were subclassing `IndexSearcher` and overriding these members to alter
  scoring, override `IndexSearcher`'s `TermStatistics()` and `CollectionStatistics()`
  methods instead.

* [LUCENE-3396](https://issues.apache.org/jira/browse/LUCENE-3396): `Analyzer.TokenStream()` has been renamed `Analyzer.GetTokenStream()`. `Analyzer.TokenStream()` has been made sealed. `.ReusableTokenStream()` has been removed.
  It is now necessary to use `Analyzer.GetTokenStreamComponents()` to define an analysis process.
  `Analyzer` also has its own way of managing the reuse of `TokenStreamComponents` (either
  globally, or per-field).  To define another `Strategy`, implement `ReuseStrategy`.

* [LUCENE-3464](https://issues.apache.org/jira/browse/LUCENE-3464): `IndexReader.Reopen()` has been renamed to
  `DirectoryReader.OpenIfChanged()` (a static method), and now returns `null`
  (instead of the old reader) if there are no changes to the index, to
  prevent the common pitfall of accidentally closing the old reader.
  
* [LUCENE-3687](https://issues.apache.org/jira/browse/LUCENE-3687): `Similarity.ComputeNorm()` now expects a `Norm` object to set the computed 
  norm value instead of returning a fixed single byte value. Custom similarities can now
  set integer, float and byte values if a single byte is not sufficient.

* [LUCENE-2621](https://issues.apache.org/jira/browse/LUCENE-2621): Term vectors are now accessed via flexible indexing API.
  If you used `IndexReader.GetTermFreqVectors()` before, you should now
  use `IndexReader.GetTermVectors()`.  The new method returns a `Fields`
  instance exposing the inverted index of the one document.  From
  `Fields` you can enumerate all fields, terms, positions, offsets.

* [LUCENE-4227](https://issues.apache.org/jira/browse/LUCENE-4227): If you were previously using `Instantiated` index, you
  may want to use `DirectPostingsFormat` after upgrading: it stores all
  postings in simple arrays (`byte[]` for terms, `int[]` for docs, freqs,
  positions, offsets).  Note that this only covers postings, whereas
  `Instantiated` covered all other parts of the index as well.

* [LUCENE-3309](https://issues.apache.org/jira/browse/LUCENE-3309): The expert `FieldSelector` API has been replaced with
  `StoredFieldVisitor`.  The idea is the same (you have full control
  over which fields should be loaded).  Instead of a single accept
  method, `StoredFieldVisitor` has a `NeedsField()` method: if that method
  returns `true` then the field will be loaded and the appropriate
  type-specific method will be invoked with that fields's value.

* [LUCENE-4122](https://issues.apache.org/jira/browse/LUCENE-4122): Removed the `Payload` class and replaced with `BytesRef`.
  `PayloadAttribute`'s name is unchanged, it just uses the `BytesRef`
  class to refer to the payload bytes/start offset/end offset 
  (or `null` if there is no payload).
