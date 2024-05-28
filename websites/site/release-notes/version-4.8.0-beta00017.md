---
uid: releasenotes/4.8.0-beta00017
version: 4.8.0-beta00017
---

# Lucene.NET 4.8.0-beta00017 Release Notes

---

> This release contains many bug fixes, performance improvements, and other housekeeing/cleanup tasks in prepartion of the production 4.8 release.

## Change Log

### Breaking Changes

- BREAKING: Lucene.Net.Util.OfflineSorter: Refactored to base file tracking on FileStream rather than FileInfo, which gives us better control over temp file deletion by specifying the FileOptions.DeleteOnClose option. We also use random access so we don't need to reopen streams over files except in the case of ExternalRefSorter.
- BREAKING: Refactored CharArraySet and CharArrayMap (now CharArrayDictionary) ([#762]\(https://github.com/apache/lucenenet/pull/762\))
- BREAKING: Lucene.Net.Analysis.Kuromoji.Token: Renamed IsKnown() > IsKnown, IsUnknown() > IsUnknown, IsUser() > IsUser.
- SWEEP: Added guard clauses for all TokenAttribute members
- SWEEP: Renamed interface TokenAttribute type file names removing the prefix "I" so the file it was ported from is clear.
- SWEEP: Renamed concrete TokenAttribute type file names to be suffixed with "Impl" so the file it was ported from is clear.
- BREAKING: Lucene.Net.Index.IndexReader: De-nested IReaderClosedListener and renamed to IReaderDisposedListener.
- BREAKING: Lucene.Net.Index.IndexWriter: Fixed Dispose() overloads so there is no method signature conflict between the public Dispose(waitForMerges) method and the protected Dispose(disposing) method that can be overridden and called from a finalizer. See [#265]\(https://github.com/apache/lucenenet/pull/265\).
- BREAKING: Lucene.Net.Search.FieldCacheRangeFilter<T>: Changed accessibility from protected internal to private protected. This class was not intended to be subclassed by users. (see [#677]\(https://github.com/apache/lucenenet/pull/677\))
- Lucene.Net.Search.Suggest.Fst.ExternalRefSorter: Changed temp path generation to use FileSupport.CreateTempFile() with named prefix and extension because it more closely matches Lucene and makes the files more easily identifiable.
- Removed .NET Core 3.1 tests and lucene-cli support for it.  ([#735]\(https://github.com/apache/lucenenet/pull/735\))
- BREAKING: Lucene.Net.Util.OfflineSorter: Changed DefaultTempDir() > GetDefaultTempDir().
- Disabled Json Source Generator.  ([#727]\(https://github.com/apache/lucenenet/pull/727\))
- Lucene.Net.Analysis.SmartCn.SmartChineseAnalyzer: Changed GetDefaultStopSet() to DefaultStopSet. Marked GetDefaultStopSet() obsolete.
- Lucene.Net.Analysis.Kuromoji.JapaneseAnalyzer: Changed GetDefaultStopSet() and GetDefaultStopTags() to DefaultStopSet and DefaultStopTags, respectively. Marked the old methods obsolete.
- SWEEP: Fixed ArgumentOutOfRange parameters so the message is passed into the 2nd parameter, not the first (which is for argumentName). Fixes [#665]\(https://github.com/apache/lucenenet/pull/665\). Also addressed potential int overflow issues when checking for "index + length must be <= array length".
- Remove NET45, NET451, NET452 Support & update website framework ver ([#650]\(https://github.com/apache/lucenenet/pull/650\))
- BREAKING: Lucene.Net.IndexWriter.IEvent: Marked internal (as it was in Java). This interface is only used in non-public contexts by Lucene.


### Bug Fixes

- Lucene.Net.Util.OfflineSorter: Added back original tests using FileInfo and fixed bugs that were preventing the original behavior
- Lucene.Net.Tests.Store.TestRAMDirectory: Fixed teardown to retry file deletion if they are locked by another process.
- fix: Aligned disposable patterns ([#746]\(https://github.com/apache/lucenenet/pull/746\))
- BUG: Changed TokenAttribute usage from concrete implementation type to interface type to align with Lucene 4.8.0. We were using the concrete type in several places where it shouldn't have been.
- BUG: Lucene.Net.Util.OfflineSorter: Fixed the Sort() and SortPartition() methods so they use the tempDirectory parameter that is passed through the constructor, as was the case in Lucene. Added a constructor overload to specify the directory as a string (a .NET convention).
- Update SlowSynonymFilter.cs
- Lucene.Net.Analysis.Kuromoji.Util.CSVUtil: Applied SOLR-9413 patch to fix the QuoteEscape() method and add tests. Fixes [#660]\(https://github.com/apache/lucenenet/pull/660\).
- Lucene.Net.Search.Similarities: Statically imported SimilarityBase where appropriate so the Log2 method doesn't have to be qualified (like in Lucene). Fixes [#694]\(https://github.com/apache/lucenenet/pull/694\).
- SWEEP: Fixed a bug where the CharArraySet returned from DefaultStopSet in all analyzers was returning a static writable instance instead of a readonly instance as per the docs.
- BUG: Lucene.Net.Tests.Index.TestIndexWriter: Finished port of RandomFailingFieldEnumerable. Fixes [#695]\(https://github.com/apache/lucenenet/pull/695\).
- BUG: Lucene.Net.Benchmark.Support.TagSoup.Parser::SetProperty(): Removed duplicate guard clause
- BUG: Lucene.Net.Analysis.Cjk.CJKBigramFilter: Changed the value of ALL to set all flags (was 0xff instead of 0xffff). Fixes [#657]\(https://github.com/apache/lucenenet/pull/657\).
- fix: Order of precedence for translation of Remove() method args in FrenchStemmer.cs  ([#654]\(https://github.com/apache/lucenenet/pull/654\))
- fix: Fixed Infinite loop in HttpClientBase
- fix: Fixed throw statement in BinaryDictionary
- fix: Fixed use of insecure 'Path.GetTempFileName' in ExternalRefSorter.cs ([#651]\(https://github.com/apache/lucenenet/pull/651\))
- BUG: Lucene.Net.Search.package.md: Corrected link to TooManyClausesException

### Performance

- PERFORMANCE: Lucene.Net.Support.Arrays::CopyOfRange(): Use the Copy() method rather than a for loop for a ~10x improvement in performance.
- PERFORMANCE: Lucene.Net.Support.Arrays::CopyOf(): Use the Copy() method rather than a for loop for a ~10x improvement in performance.
- PERFORMANCE: Lucene.Net.Support.Arrays::Fill(): Replaced for loop implementation with Array.Fill() or Span.Fill<T>() depending on platform.
- PERFORMANCE: Lucene.Net.Support.Arrays: Added Copy() overloads that use the most efficient (known) copy method for the platform and data type based on benchmarks. Replaced all occurrences of Array.Copy() and Buffer.BlockCopy() with Arrays.Copy().
- Lucene.Net.Support.DictionaryExtensions: Reduced dependency on the Put() method and added documentation to indicate that it doesn't work with non-nullable value types. Also documented the PutAll() method and added guard clause.
- PERFORMANCE: Lucene.Net.Analysis.Sinks.DateRecognizerSinkFilter: Prefer ReadOnlySpan<char> overloads of DateTime.TryParse() and DateTime.TryParseExact(), when available.
- PERFORMANCE: Lucene.Net.Analsis.Util.HTMLStripCharFilter: Refactored to remove YyText property (method) which allocates a string every time it is called. Instead, we pass the underlying array to J2N.Numerics.TryParse() and OpenStringBuilder.Append() with the calculated startIndex and length to directly copy the characters without allocating substrings.
- PERFORMANCE: Lucene.Net.Analysis.Util.OpenStringBuilder: Added overloads of UnsafeWrite() for string an ICharSequence. Optimized Append() methods to call UnsafeWrite with index and count to optimize the operation depending on the type of object passed.
- PERFORAMANCE: Lucene.Net.Analysis.Ga.IrishLowerCaseFilter: Use stack and spans to reduce allocations and improve throughput.
- PERFORMANCE: Lucene.Net.Analysis.Th.ThaiWordBreaker: Removed unnecessary string allocations and concatenation. Use CharsRef to reuse the same memory. Removed Regex and replaced with UnicodeSet to detect Thai code points.
- PERFORMANCE: Lucene.Net.Analysis.In.IndicNormalizer: Refactored ScriptData to change Dictionary<Regex, ScriptData> to List<ScriptData> and eliminated unnecessary hashtable lookup. Use static fields for unknownScript and [ThreadStatic] previousScriptData to optimize character script matching.
- PERFORMANCE: Lucene.Net.Analysis.In.IndicNormalizer: Replaced static constructor with inline LoadScripts() method. Moved location of scripts field to ensure decompositions is initialized first.
- PERFORMANCE: Lucene.Net.Analysis.Ja.GraphvizFormatter: Removed unnecessary surfaceForm string allocation.
- PERFORMANCE: Lucene.Net.Analysis.Util.SegmentingTokenizerBase: Removed unnecessary string allocations that were added during the port due to missing APIs.
- Lucene.Net.Util.TestUnicodeUtil::TestUTF8toUTF32(): Added additional tests for ICharSequence and char[] overloads, changed the original test to test string.
- PERFORMANCE: Lucene.Net.Analysis.Miscellaneous.StemmerOverrideFilter: Added overloads to Add for ICharSequence and char[] to reduce allocations. Added guard clauses.
- PERFORMANCE: Lucene.Net.Analysis.Util.CharacterUtils: Use spans and stackalloc to reduce heap allocations when lowercasing. Added system property named "maxStackLimit" that defaults to 2048 bytes.
- PERFORMANCE: Lucene.Net.Analysis.CharFilters.HTMLStripCharFilter: Removed allocation during parse of hexadecimal number by using J2N.Numerics.Int32 to specify index and length. Also added a CharArrayFormatter struct to defer the allocation of constructing a string until after an assertion failure.
- PERFORMANCE: Lucene.Net.Codecs.SimpleText.SimpleTextUtil::Write(): Removed unnecessary ToCharArray() allocation
- PERFORMANCE: Lucene.Net.Document.CompressionTools::CompressString(): Eliminated unnecessary ToCharArray() allocation
- PERFORMANCE: Use 'StringBuilder.Append(char)' instead of 'StringBuilder.Append(string)' when the input is a constant unit string. ([#708]\(https://github.com/apache/lucenenet/pull/708\))
- PERFORMANCE: Lucene.Net.Util.MergedEnumerator: Changed from using inline string conversions when the generic type is string to J2N.Collections.Generic.Comparer<T>, which is statically initialized with the optimal comparer that matches Java behaviors.
- PERFORMANCE: Lucene.Net.Index.BaseCompositeReader: Removed unnecessary list allocation

### Improvements

- net7.0 support
- Lucene.Net.Support.IO (BinaryReaderDataInput + BinaryReaderDataOutput): Deleted, as they are no longer in use.
- Lucene.Net.Support.IO.FileSupport: Added CreateTempFileAsStream() method overloads to return an open stream for the file so we don't have to rely on the operating system to close the file handle before we attempt to reopen it.
- Lucene.Net.Support.IO: Added FileStreamOptions class similar to the one in .NET 6 to easily pass as a method parameter.
- Align InputStreamDataInput and OutputStreamDataOutput closer to the Java Source ([#769]\(https://github.com/apache/lucenenet/pull/769\))
- SWEEP: Reviewed to ensure we are consistently using Arrays.Fill() instead of the (slower) Array.Clear() method or a for loop (ouch).
- Lucene.Net.Analysis.Util.BufferedCharFilter: Implemented Peek() method. Added overrides for Span<T> and Memory<T> based methods to throw NotSupportedException. Added doc comments.
- Lucene.Net.Analysis.Util.BufferedCharFilter: Check for CharFilter type inline
- Lucene.Net.Analysis.Util.BufferedCharFilter: Use UninterruptableMonitor for consistency (missed a couple)
- Lucene.Net.Queries.Mlt.MoreLikeThis: Use 'StringBuilder.Append(char)' instead of 'StringBuilder.Append(string)' when the input is a constant unit string (see [#674]\(https://github.com/apache/lucenenet/pull/674\))
- SWEEP: Changed all internal constructors of abstract classes to private protected. (fixes [#677]\(https://github.com/apache/lucenenet/pull/677\))
- SWEEP: Changed all protected internal constructors of abstract classes to protected.  (fixes [#677]\(https://github.com/apache/lucenenet/pull/677\))
- SWEEP: Changed all public constructors of abstract classes to protected, except where it would be a problem for Reflection calls. (fixes [#677]\(https://github.com/apache/lucenenet/pull/677\))
- ci: Added sonar workflow ([#709]\(https://github.com/apache/lucenenet/pull/709\))
- Lucene.Net.Support.IO.FileSupport::GetFileIOExceptionHResult(): Avoid Path.GetTempFileName() because it is not secure. https://rules.sonarsource.com/csharp/RSPEC-5445
- Lucene.Net.Util.OfflineSorter (ByteSequencesReader + ByteSequencesWriter): Added constructor overloads to pass the file name as a string (.NET convention)
- Lucene.Net.Util.OfflineSorter: Added guard clauses (and removed asserts). Enabled nullable reference type support. Added disposed flag to ensure dispose only happens once.
- Updated Azure DevOps Mac OS used for Net3.1 Testing ([#728]\(https://github.com/apache/lucenenet/pull/728\))
- changes made for Redundant jump statements [#684]\(https://github.com/apache/lucenenet/pull/684\) ([#724]\(https://github.com/apache/lucenenet/pull/724\))
- Removed 2 private nested classes that were not in use ([#713]\(https://github.com/apache/lucenenet/pull/713\))
- SWEEP: Migrated all callers of CharArraySet.UnmodifiableSet() and CharArrayMap.UnmodifiableMap() to the AsReadOnly() instance methods
- Lucene.Net.Analysis.Common.Analysis.Util (CharArrayMap + CharArraySet): Added AsReadOnly() instance methods to match .NET conventions and marked UnmodifiableMap/UnmodifiableSet obsolete.
- SWEEP: Removed call to ToString() for StringBuilder.Append() methods, so strongly typed StringBuilder overloads can be used on target frameworks that support it. Fixes [#668]\(https://github.com/apache/lucenenet/pull/668\).
- Lucene.Net.Benchmark.Support.TagSoup.HTMLScanner: Reworked initialization to return statetableIndexMaxChar and set statetableIndex via out parameter.
- SWEEP: Marked all singleton "Holder" classes static. Fixes [#659]\(https://github.com/apache/lucenenet/pull/659\).
- SWEEP: Renamed classes from using Iterable and Iterator to Enumerable and Enumerator, where appropriate - some were missed in [#698]\(https://github.com/apache/lucenenet/pull/698\).
- SWEEP: Normalize anonymous class names/accessibility. Fixes [#666]\(https://github.com/apache/lucenenet/pull/666\).
- Lucene.Net.Util.Fst.BytesStore: Suffix anonymous classes with "AnonymousClass". See [#666]\(https://github.com/apache/lucenenet/pull/666\).
- SWEEP: Renamed classes from using Iterable and Iterator to Enumerable and Enumerator, where appropropriate. See [#279]\(https://github.com/apache/lucenenet/pull/279\).
- SWEEP: Add a nested comment explaining why this method is empty. Fixes [#681]\(https://github.com/apache/lucenenet/pull/681\).
- SWEEP: Lucene.Net.Benchmark.Support.TagSoup: Reviewed API for accessibility issues. Fixed error handling and guard clauses. Changed to generic collections. Renamed method arguments.
- SWEEP: Prefer 'AsSpan' over 'Substring' when span-based overloads are available. Fixes [#675]\(https://github.com/apache/lucenenet/pull/675\).
- Lucene.Net.Analysis.Cn.Smart.Utility: Changed SPACES to use an encoded character for '\u3000' so it is visible in the designer. Fixes [#680]\(https://github.com/apache/lucenenet/pull/680\).
- Lucene.Net.Tests.Analysis.SmartCn.TestHMMChineseTokenizerFactory: Added LuceneNetSpecificAttribute to TestHHMMSegmenter() and renamed TestHHMMSegmenterInitialization(), since this is a smoke test that was added to test initialization of HHMMSegmenter.
- chore(deps): Update Newtonsoft.Json to 13.0.1 to avoid vulnerability ([#647]\(https://github.com/apache/lucenenet/pull/647\))
- Added PersianStemmer ([#571]\(https://github.com/apache/lucenenet/pull/571\))
- SWEEP: Increased timeouts on tests to keep them from intermittently failing during nightly builds.
