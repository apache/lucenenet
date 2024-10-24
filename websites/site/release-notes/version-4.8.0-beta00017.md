---
uid: releasenotes/4.8.0-beta00017
version: 4.8.0-beta00017
---

# Lucene.NET 4.8.0-beta00017 Release Notes

---

> This release contains many bug fixes, performance improvements, and other housekeeping/cleanup tasks in preparation of the production 4.8 release.

## Change Log

### Breaking Changes

- Lucene.Net.Util.OfflineSorter: Refactored to base file tracking on FileStream rather than FileInfo, which gives us better control over temp file deletion by specifying the `FileOptions.DeleteOnClose` option. We also use random access so we don't need to reopen streams over files except in the case of ExternalRefSorter.
- Refactored CharArraySet and CharArrayMap (now CharArrayDictionary) ([#762](https://github.com/apache/lucenenet/pull/762))
- Lucene.Net.Analysis.Kuromoji.Token: Changed these methods into properties: `IsKnown()` to `IsKnown`, `IsUnknown()` to `IsUnknown`, `IsUser()` to `IsUser`.
- Added guard clauses for all TokenAttribute members
- Renamed interface TokenAttribute type file names removing the prefix "I" so the file it was ported from is clear.
- Renamed concrete TokenAttribute type file names to be suffixed with "Impl" so the file it was ported from is clear.
- Lucene.Net.Index.IndexReader: De-nested IReaderClosedListener and renamed to IReaderDisposedListener.
- Lucene.Net.Index.IndexWriter: Fixed `Dispose()` overloads so there is no method signature conflict between the public `Dispose(waitForMerges)` method and the protected `Dispose(disposing)` method that can be overridden and called from a finalizer. See [#265](https://github.com/apache/lucenenet/pull/265).
- Lucene.Net.Search.FieldCacheRangeFilter&lt;T&gt;: Changed accessibility from protected internal to private protected. This class was not intended to be subclassed by users. (see [#677](https://github.com/apache/lucenenet/pull/677))
- Lucene.Net.Search.Suggest.Fst.ExternalRefSorter: Changed temp path generation to use `FileSupport.CreateTempFile()` with named prefix and extension because it more closely matches Lucene and makes the files more easily identifiable.
- Removed .NET Core 3.1 tests and lucene-cli support for it.  ([#735](https://github.com/apache/lucenenet/pull/735))
- Lucene.Net.Util.OfflineSorter: Changed `DefaultTempDir()` to `GetDefaultTempDir()`.
- Lucene.Net.Analysis.SmartCn.SmartChineseAnalyzer: Changed `GetDefaultStopSet()` to `DefaultStopSet`. Marked `GetDefaultStopSet()` obsolete.
- Lucene.Net.Analysis.Kuromoji.JapaneseAnalyzer: Changed `GetDefaultStopSet()` and `GetDefaultStopTags()` to `DefaultStopSet` and `DefaultStopTags`, respectively. Marked the old methods obsolete.
- Fixed ArgumentOutOfRange parameters so the message is passed into the 2nd parameter, not the first (which is for argumentName). Fixes [#665](https://github.com/apache/lucenenet/pull/665). Also addressed potential int overflow issues when checking for "index + length must be <= array length".
- Remove .NET 4.5, .NET 4.5.1, and .NET 4.5.2 support and update website framework versions ([#650](https://github.com/apache/lucenenet/pull/650))
- Lucene.Net.IndexWriter.IEvent: Marked internal (as it was in Java). This interface is only used in non-public contexts by Lucene.
- Remove virtual on methods that are being called from constructors
([#670](https://github.com/apache/lucenenet/issues/670) - see PRs linked to this issue)
- Lucene.Net.Util.PriorityQueue&lt;T&gt;: Replaced `(int, bool)` constructor and removed constructor call to virtual `GetSentinelObject()` method ([#820](https://github.com/apache/lucenenet/pull/820))
- Lucene.Net.Util: Added ValuePriorityQueue&lt;T&gt; to utilize stack allocations where possible ([#826](https://github.com/apache/lucenenet/pull/826))
- Use factory classes for DirectoryTaxonomyWriter and DirectoryTaxonomyReader to get configs ([#847](https://github.com/apache/lucenenet/pull/847))
- The Lucene CLI now runs on the .NET 8 runtime.
- Replicator no longer attempts to deserialize exception types ([#968](https://github.com/apache/lucenenet/pull/968))
- Rename `IndexWriter.NextMerge()` to `GetNextMerge()` ([#990](https://github.com/apache/lucenenet/pull/990))

### Bug Fixes

- Lucene.Net.Util.OfflineSorter: Added back original tests using FileInfo and fixed bugs that were preventing the original behavior
- Lucene.Net.Tests.Store.TestRAMDirectory: Fixed teardown to retry file deletion if they are locked by another process.
- Aligned disposable patterns ([#746](https://github.com/apache/lucenenet/pull/746))
- Changed TokenAttribute usage from concrete implementation type to interface type to align with Lucene 4.8.0. We were using the concrete type in several places where it shouldn't have been.
- Lucene.Net.Util.OfflineSorter: Fixed the `Sort()` and `SortPartition()` methods so they use the `tempDirectory` parameter that is passed through the constructor, as was the case in Lucene. Added a constructor overload to specify the directory as a string (a .NET convention).
- Lucene.Net.Analysis.Kuromoji.Util.CSVUtil: Applied SOLR-9413 patch to fix the `QuoteEscape()` method and add tests. Fixes [#660](https://github.com/apache/lucenenet/pull/660).
- Lucene.Net.Search.Similarities: Statically imported SimilarityBase where appropriate so the Log2 method doesn't have to be qualified (like in Lucene). Fixes [#694](https://github.com/apache/lucenenet/pull/694).
- Fixed a bug where the CharArraySet returned from DefaultStopSet in all analyzers was returning a static writable instance instead of a readonly instance as per the docs.
- Lucene.Net.Tests.Index.TestIndexWriter: Finished port of RandomFailingFieldEnumerable. Fixes [#695](https://github.com/apache/lucenenet/pull/695).
- Lucene.Net.Benchmark.Support.TagSoup.Parser::SetProperty(): Removed duplicate guard clause
- Lucene.Net.Analysis.Cjk.CJKBigramFilter: Changed the value of ALL to set all flags (was 0xff instead of 0xffff). Fixes [#657](https://github.com/apache/lucenenet/pull/657).
- Order of precedence for translation of `Remove()` method args in FrenchStemmer.cs  ([#654](https://github.com/apache/lucenenet/pull/654))
- Fixed Infinite loop in HttpClientBase
- Fixed throw statement in BinaryDictionary
- Fixed use of insecure `Path.GetTempFileName` in ExternalRefSorter ([#651](https://github.com/apache/lucenenet/pull/651))
- Lucene.Net.Search.package.md: Corrected link to TooManyClausesException
- Fix AssertionError in JapaneseTokenizer backtrace LUCENE-10059 ([#777](https://github.com/apache/lucenenet/pull/777))
- Lucene.Net.Util.RandomizedContext: Create a separate instance of `Randomizer()` for each thread initialized with the same seed. Fixes [#843](https://github.com/apache/lucenenet/issues/843).
- TestIndexWriterOnJRECrash: Removed using block to ensure that our original CheckIndex error bubbles up instead of any issue disposing (or double-disposing) the directory.
- Lucene.Net.Documents.DateTools: Convert `DateTimeKind.Unspecified` dates to UTC, otherwise they can produce ArgumentOutOfRangeException. Fixes [#772](https://github.com/apache/lucenenet/issues/772).
- Lucene.Net.Store: Fixed several `Dispose()` methods so they are safe to be called multiple times ([#854](https://github.com/apache/lucenenet/pull/854))
- Lucene.Net.QueryParsers.Classic.QueryParserTokenManager: Removed initialization code that caused writing a BOM to standard out upon creation. ([#902](https://github.com/apache/lucenenet/pull/902))
- Lucene.Net.Search.FieldComparer.TermValComparer: Fixed sorting ambiguity between empty fields and missing fields ([#912](https://github.com/apache/lucenenet/pull/912))
- Fix for DocumentsWriter concurrency (fixes #935, closes #886) ([#940](https://github.com/apache/lucenenet/pull/940))
- Finalizer fix in IndexReader ([#951](https://github.com/apache/lucenenet/pull/951))
- Fix for Lucene.Net.Util.SystemConsole throwing not supported exception in .NET MAUI app running on Android/iOS ([#952](https://github.com/apache/lucenenet/pull/952))
- Fix TermStats.TermText access, add CLI comments and 1 CLI bug fix ([#963](https://github.com/apache/lucenenet/pull/963))

### Performance

- Lucene.Net.Support.Arrays::CopyOfRange(): Use the `Copy()` method rather than a for loop for a ~10x improvement in performance.
- Lucene.Net.Support.Arrays::CopyOf(): Use the `Copy()` method rather than a for loop for a ~10x improvement in performance.
- Lucene.Net.Support.Arrays::Fill(): Replaced for loop implementation with `Array.Fill()` or `Span.Fill<T>()` depending on platform.
- Lucene.Net.Support.Arrays: Added `Copy()` overloads that use the most efficient (known) copy method for the platform and data type based on benchmarks. Replaced all occurrences of `Array.Copy()` and `Buffer.BlockCopy()` with `Arrays.Copy()`.
- Lucene.Net.Support.DictionaryExtensions: Reduced dependency on the `Put()` method and added documentation to indicate that it doesn't work with non-nullable value types. Also documented the `PutAll()` method and added guard clause.
- Lucene.Net.Analysis.Sinks.DateRecognizerSinkFilter: Prefer `ReadOnlySpan<char>` overloads of `DateTime.TryParse()` and `DateTime.TryParseExact()`, when available.
- Lucene.Net.Analysis.Util.HTMLStripCharFilter: Refactored to remove YyText property (method) which allocates a string every time it is called. Instead, we pass the underlying array to `J2N.Numerics.TryParse()` and `OpenStringBuilder.Append()` with the calculated startIndex and length to directly copy the characters without allocating substrings.
- Lucene.Net.Analysis.Util.OpenStringBuilder: Added overloads of `UnsafeWrite()` for string and ICharSequence. Optimized `Append()` methods to call `UnsafeWrite` with index and count to optimize the operation depending on the type of object passed.
- Lucene.Net.Analysis.Ga.IrishLowerCaseFilter: Use stack and spans to reduce allocations and improve throughput.
- Lucene.Net.Analysis.Th.ThaiWordBreaker: Removed unnecessary string allocations and concatenation. Use CharsRef to reuse the same memory. Removed Regex and replaced with UnicodeSet to detect Thai code points.
- Lucene.Net.Analysis.In.IndicNormalizer: Refactored ScriptData to change `Dictionary<Regex, ScriptData>` to `List<ScriptData>` and eliminated unnecessary hashtable lookup. Use static fields for unknownScript and `[ThreadStatic]` `previousScriptData` to optimize character script matching.
- Lucene.Net.Analysis.In.IndicNormalizer: Replaced static constructor with inline `LoadScripts()` method. Moved location of scripts field to ensure decompositions is initialized first.
- Lucene.Net.Analysis.Ja.GraphvizFormatter: Removed unnecessary surfaceForm string allocation.
- Lucene.Net.Analysis.Util.SegmentingTokenizerBase: Removed unnecessary string allocations that were added during the port due to missing APIs.
- Lucene.Net.Util.TestUnicodeUtil::TestUTF8toUTF32(): Added additional tests for `ICharSequence` and `char[]` overloads, changed the original test to test string.
- Lucene.Net.Analysis.Miscellaneous.StemmerOverrideFilter: Added overloads to Add for `ICharSequence` and `char[]` to reduce allocations. Added guard clauses.
- Lucene.Net.Analysis.Util.CharacterUtils: Use spans and stackalloc to reduce heap allocations when lowercasing. Added system property named "maxStackLimit" that defaults to 2048 bytes.
- Lucene.Net.Analysis.CharFilters.HTMLStripCharFilter: Removed allocation during parse of hexadecimal number by using J2N.Numerics.Int32 to specify index and length. Also added a CharArrayFormatter struct to defer the allocation of constructing a string until after an assertion failure.
- Lucene.Net.Codecs.SimpleText.SimpleTextUtil::Write(): Removed unnecessary `ToCharArray()` allocation
- Lucene.Net.Document.CompressionTools::CompressString(): Eliminated unnecessary `ToCharArray()` allocation
- Use `StringBuilder.Append(char)` instead of `StringBuilder.Append(string)` when the input is a constant unit string. ([#708](https://github.com/apache/lucenenet/pull/708))
- Lucene.Net.Util.MergedEnumerator: Changed from using inline string conversions when the generic type is string to J2N.Collections.Generic.Comparer<T>, which is statically initialized with the optimal comparer that matches Java behaviors.
- Lucene.Net.Index.BaseCompositeReader: Removed unnecessary list allocation
- Lucene.Net.Analysis.TokenAttributes.CharTermAttribute: Optimized char copying on `Append()` and `Subsequence()` ([#899](https://github.com/apache/lucenenet/pull/899))
- Lucene.Net.Facet.Taxonomy.WriterCache.CharBlockArray: Compare equality and calculate hash code without allocating ([#900](https://github.com/apache/lucenenet/pull/900))
- Restore fsync behavior in FSDirectory via P/Invoke, fixes significant performance regression when running on .NET 8 ([#938](https://github.com/apache/lucenenet/pull/938))
- Use concrete ConcurrentHashSet instead of abstraction for performance ([#960](https://github.com/apache/lucenenet/pull/960))

### Improvements

- .NET 8.0 target ([#982](https://github.com/apache/lucenenet/pull/982))
- Updated many dependencies, including J2N and ICU4N
- Lucene.Net.Support.IO (BinaryReaderDataInput + BinaryReaderDataOutput): Deleted, as they are no longer in use.
- Lucene.Net.Support.IO.FileSupport: Added `CreateTempFileAsStream()` method overloads to return an open stream for the file so we don't have to rely on the operating system to close the file handle before we attempt to reopen it.
- Lucene.Net.Support.IO: Added FileStreamOptions class similar to the one in .NET 6 to easily pass as a method parameter.
- Align InputStreamDataInput and OutputStreamDataOutput closer to the Java Source ([#769](https://github.com/apache/lucenenet/pull/769))
- Reviewed to ensure we are consistently using `Arrays.Fill()` instead of the (slower) `Array.Clear()` method or a for loop.
- Lucene.Net.Analysis.Util.BufferedCharFilter: Implemented `Peek()` method. Added overrides for `Span<T>` and `Memory<T>` based methods to throw NotSupportedException. Added doc comments.
- Lucene.Net.Analysis.Util.BufferedCharFilter: Check for CharFilter type inline
- Lucene.Net.Analysis.Util.BufferedCharFilter: Use UninterruptableMonitor for consistency (missed a couple)
- Lucene.Net.Queries.Mlt.MoreLikeThis: Use `StringBuilder.Append(char)` instead of `StringBuilder.Append(string)` when the input is a constant unit string (see [#674](https://github.com/apache/lucenenet/pull/674))
- Changed all internal constructors of abstract classes to private protected. (fixes [#677](https://github.com/apache/lucenenet/pull/677))
- Changed all protected internal constructors of abstract classes to protected.  (fixes [#677](https://github.com/apache/lucenenet/pull/677))
- Changed all public constructors of abstract classes to protected, except where it would be a problem for Reflection calls. (fixes [#677](https://github.com/apache/lucenenet/pull/677))
- ci: Added sonar workflow ([#709](https://github.com/apache/lucenenet/pull/709))
- Lucene.Net.Support.IO.FileSupport::GetFileIOExceptionHResult(): Avoid `Path.GetTempFileName()` because it is not secure. https://rules.sonarsource.com/csharp/RSPEC-5445
- Lucene.Net.Util.OfflineSorter (ByteSequencesReader + ByteSequencesWriter): Added constructor overloads to pass the file name as a string (.NET convention)
- Lucene.Net.Util.OfflineSorter: Added guard clauses (and removed asserts). Enabled nullable reference type support. Added disposed flag to ensure dispose only happens once.
- changes made for Redundant jump statements [#684](https://github.com/apache/lucenenet/pull/684) ([#724](https://github.com/apache/lucenenet/pull/724))
- Removed 2 private nested classes that were not in use ([#713](https://github.com/apache/lucenenet/pull/713))
- Migrated all callers of `CharArraySet.UnmodifiableSet()` and `CharArrayMap.UnmodifiableMap()` to the `AsReadOnly()` instance methods
- Lucene.Net.Analysis.Common.Analysis.Util (CharArrayMap + CharArraySet): Added `AsReadOnly()` instance methods to match .NET conventions and marked UnmodifiableMap/UnmodifiableSet obsolete.
- Removed call to `ToString()` for `StringBuilder.Append()` methods, so strongly typed StringBuilder overloads can be used on target frameworks that support it. Fixes [#668](https://github.com/apache/lucenenet/pull/668).
- Lucene.Net.Benchmark.Support.TagSoup.HTMLScanner: Reworked initialization to return `statetableIndexMaxChar` and set `statetableIndex` via out parameter.
- Marked all singleton "Holder" classes static. Fixes [#659](https://github.com/apache/lucenenet/pull/659).
- Renamed classes from using Iterable and Iterator to Enumerable and Enumerator, where appropriate - some were missed in [#698](https://github.com/apache/lucenenet/pull/698).
- Normalize anonymous class names/accessibility. Fixes [#666](https://github.com/apache/lucenenet/pull/666).
- Lucene.Net.Util.Fst.BytesStore: Suffix anonymous classes with "AnonymousClass". See [#666](https://github.com/apache/lucenenet/pull/666).
- Renamed classes from using Iterable and Iterator to Enumerable and Enumerator, where appropropriate. See [#279](https://github.com/apache/lucenenet/pull/279).
- Add a nested comment explaining why this method is empty. Fixes [#681](https://github.com/apache/lucenenet/pull/681).
- Lucene.Net.Benchmark.Support.TagSoup: Reviewed API for accessibility issues. Fixed error handling and guard clauses. Changed to generic collections. Renamed method arguments.
- Prefer 'AsSpan' over 'Substring' when span-based overloads are available. Fixes [#675](https://github.com/apache/lucenenet/pull/675).
- Lucene.Net.Analysis.Cn.Smart.Utility: Changed SPACES to use an encoded character for `'\u3000'` so it is visible in the designer. Fixes [#680](https://github.com/apache/lucenenet/pull/680).
- Lucene.Net.Tests.Analysis.SmartCn.TestHMMChineseTokenizerFactory: Added LuceneNetSpecificAttribute to `TestHHMMSegmenter()` and renamed `TestHHMMSegmenterInitialization()`, since this is a smoke test that was added to test initialization of HHMMSegmenter.
- Added PersianStemmer ([#571](https://github.com/apache/lucenenet/pull/571))
- Increased timeouts on tests to keep them from intermittently failing during nightly builds.
- Ported missing test: TestIndexWriterOnJRECrash ([#786](https://github.com/apache/lucenenet/pull/786))
- Fix slow test: use different sleep method if resolution is low ([#838](https://github.com/apache/lucenenet/pull/838))
- Lucene.Net.Documents.DateTools: Added exceptions to documentation and added nullable reference type support.
- Lucene.Net.Store (BaseDirectory + BaseDirectoryWrapper): Fixed XML comments so they don't produce warnings ([#855](https://github.com/apache/lucenenet/pull/855))
- Fix two typos in quick-start introduction.md ([#863](https://github.com/apache/lucenenet/pull/863))
- Remove a repetition from the quick-start index.md page ([#862](https://github.com/apache/lucenenet/pull/862))
- Update introduction.md and tutorial.md ([#870](https://github.com/apache/lucenenet/pull/870)) and ([#871](https://github.com/apache/lucenenet/pull/871))
- Removed dependency on Prism.Core (Fixes #872) ([#875](https://github.com/apache/lucenenet/pull/875))
- Lucene.Net.Util.PriorityQueue: Removed `[Serializable]` attribute. ([#876](https://github.com/apache/lucenenet/pull/876))
- Lucene.Net.Facet.Taxonomy.WriterCache.CharBlockArray: Implemented IAppendable to align with Lucene ([#901](https://github.com/apache/lucenenet/pull/901))
- Added the slack channel to the contributing page of the website (Fixes #893) ([#908](https://github.com/apache/lucenenet/pull/908))
- Added missing namespaces to tutorial ([#909](https://github.com/apache/lucenenet/pull/909))
- Added net6.0 target to Lucene.Net.Analysis.OpenNLP and changed to using MavenReference ([#892](https://github.com/apache/lucenenet/pull/892))
- Lucene.Net.Search.ReferenceContext&lt;T&gt;: Converted to ref struct and reworked `TestControlledRealTimeReopenThread.TestStraightForwardDemonstration()` to verify functionality ([#925](https://github.com/apache/lucenenet/pull/925))
- Reviewed usage of atomic numeric type methods ([#927](https://github.com/apache/lucenenet/pull/927))
- Upgrade tests and CLI to .NET 8, fix benchmark formatting issue ([#930](https://github.com/apache/lucenenet/pull/930))
- Conversion to native `Array.Empty<T>()` ([#953](https://github.com/apache/lucenenet/pull/953))
- TryDequeue and TryPeek Queue extension methods ([#957](https://github.com/apache/lucenenet/pull/957))
- Website support for `/latest/` and `/absolute-latest/` redirection ([#965](https://github.com/apache/lucenenet/pull/965))
- Docfx Documentation Fixes, Upgrade docfx, Cross-platform support ([#961](https://github.com/apache/lucenenet/pull/961))
-
## New Contributors
* @raminmjj made their first contribution in [#571](https://github.com/apache/lucenenet/pull/571)
* @BrandonStudio made their first contribution in [#634](https://github.com/apache/lucenenet/pull/634)
* @nikcio made their first contribution in [#647](https://github.com/apache/lucenenet/pull/647)
* @sachdevlaksh made their first contribution in [#724](https://github.com/apache/lucenenet/pull/724)
* @thbst16 made their first contribution in [#773](https://github.com/apache/lucenenet/pull/773)
* @vrdc90 made their first contribution in [#790](https://github.com/apache/lucenenet/pull/790)
* @chenhh021 made their first contribution in [#777](https://github.com/apache/lucenenet/pull/777)
* @Jeevananthan-23 made their first contribution in [#786](https://github.com/apache/lucenenet/pull/786)
* @cucoreanu made their first contribution in [#863](https://github.com/apache/lucenenet/pull/863)
* @tomwolfgang made their first contribution in [#870](https://github.com/apache/lucenenet/pull/870)
* @jaivardhan-bhola made their first contribution in [#908](https://github.com/apache/lucenenet/pull/908)
* @kant2002 made their first contribution in [#909](https://github.com/apache/lucenenet/pull/909)
* @J-Exodus made their first contribution in [#953](https://github.com/apache/lucenenet/pull/953)
* @stesee made their first contribution in [#951](https://github.com/apache/lucenenet/pull/951)
* @devklick made their first contribution in [#957](https://github.com/apache/lucenenet/pull/957)
* @JayOfemi made their first contribution in [#952](https://github.com/apache/lucenenet/pull/952)