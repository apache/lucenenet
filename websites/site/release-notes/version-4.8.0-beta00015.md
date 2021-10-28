---
uid: releasenotes/4.8.0-beta00015
version: 4.8.0-beta00015
---

# Lucene.NET 4.8.0-beta00015 Release Notes

---

> This release contains important bug fixes, performance enhancements, concurrency improvements, and improved debugging support (full stack traces, consistent exception types, attributes for debug view, and structurally formattable lists).

> Much of the exception handling has been changed so it is recommended to test thoroughly, especially if your application relies on catching exceptions from Lucene.NET for control flow. The full extent of the exception handling changes are not documented here, but can be viewed at https://github.com/apache/lucenenet/pull/476/files.

## Known Issues

* `Lucene.Net.Index.IndexWriter::Dispose()`: Using `Thread.Interrupt()` to shutdown background threads in .NET is problematic because `System.Threading.ThreadInterruptedException` could be thrown on any `lock` statement with contention on it. This includes `lock` statements on code that we depend on or custom components that are engaged during a `Commit()` (such as a custom `Directory` implementation). These exceptions may cause `Commit()` to fail in unexpected ways during `IndexWriter.Dispose()`. While this affected all prior releases to a much larger degree, this release provides a partial solution using [`UninterruptableMonitor.Enter()`](https://github.com/apache/lucenenet/blob/f68cbb3ef79e0635397fcc367267ee252e2264c1/src/Lucene.Net/Support/Threading/UninterruptableMonitor.cs#L61-L90) to ensure these exceptions are ignored and the `Thread.Interrupt()` state restored, which greatly reduces the chance a `Commit()` could be broken or a deadlock can occur. This problem will not affect applications that do not call `Thread.Interrupt()` to shut down a thread. It is recommended never to use `Thread.Interrupt()` in conjunction with `IndexWriter`, `ConcurrentMergeScheduler`, or `ControlledRealTimeReopenThread`. 

## Change Log

### Breaking Changes
* [#455](https://github.com/apache/lucenenet/pull/455) - `lucene-cli`: Changed exit codes to well-defined constants to make testing simpler
* [#407](https://github.com/apache/lucenenet/pull/407) - Moved all Document extensions to the `Lucene.Net.Documents.Extensions` namespace and added tests for `DocumentExtensions` in `Lucene.Net.Tests._J-S`, `Lucene.Net.Tests.ICU` and `Lucene.Net.Tests.Facet`. Added guard clauses and updated documentation of Document extension methods and some related fields.
* [#474](https://github.com/apache/lucenenet/pull/474) - `Lucene.Net.TestFramework.Util.TestUtil`: Renamed method parameters from abbreviations to whole words to follow .NET API conventions and improved documentation.
* [#475](https://github.com/apache/lucenenet/pull/475) - `Lucene.Net.Grouping`: Refactored and improved `GroupingSearch` Search API and added `GroupByField()` and `GroupByFunction()` methods.
* [#479](https://github.com/apache/lucenenet/pull/479) - Moved `Lucene.Net.Join` types to `Lucene.Net.Search.Join` namespace
* Marked public exception constructors that were meant only for testing internal (affects only .NET Framework)
* [#446](https://github.com/apache/lucenenet/pull/446), [#476](https://github.com/apache/lucenenet/pull/476) - Redesigned exception handling to ensure that exception behavior is the same as in Lucene and so we consistently throw the closest .NET equivalent exception across all of the projects.
* [#480](https://github.com/apache/lucenenet/pull/480) - Changed `Cardinality()` methods to `Cardinality` property. Added obsolete `Cardinality()` extension methods to the namespace of each of the pertinent types for backward compatibility.
  - `Lucene.Net.Index.RandomAccessOrds`
  - `Lucene.Net.Util.FixedBitSet`
  - `Lucene.Net.Util.Int64BitSet`
  - `Lucene.Net.Util.OpenBitSet`
  - `Lucene.Net.Util.PForDeltaDocIdSet`
  - `Lucene.Net.Util.WAH8DocIdSet`
* [#481](https://github.com/apache/lucenenet/pull/481) - `Lucene.Net.Index.Term`: Changed `Text()` method into `Text` property. Added an obsolete `Text()` extension method to `Lucene.Net.Index` namespace for backward compatibility.
* [#482](https://github.com/apache/lucenenet/pull/482) - `Lucene.Net.BinaryDocValuesField`: Changed `fType` static field to `TYPE` (as it was in Lucene) and added obsolete `fType` field for backward compatibility.
* [#483](https://github.com/apache/lucenenet/pull/483) - Changed all `GetFilePointer()` methods into properties named `Position` to match `FileStream`. Types affected: `Lucene.Net.Store.IndexInput` (and subclasses), `Lucene.Net.Store.IndexOutput` (and subclasses). Added obsolete extension methods for each type in `Lucene.Net.Store` namespace for backward compatibility.
* [#484](https://github.com/apache/lucenenet/pull/484) - `Lucene.Net.QueryParser`: Factored out `NLS`/`IMessage`/`Message` support and changed exceptions to use string messages so end users can elect whether or not to use .NET localization, as is possible with any other .NET exception type.
* [#484](https://github.com/apache/lucenenet/pull/484) - `Lucene.Net.QueryParsers.Flexible.Messages`: Removed entire namespace, as we have refactored to use .NET localization rather than NLS
* [#484](https://github.com/apache/lucenenet/pull/484) - `Lucene.Net.Util`: Removed `BundleResourceManagerFactory` and `IResourceManagerFactory`, as these were only to support NLS. The new approach to localizing messages can be achieved by registering `QueryParserMessages.SetResourceProvider(SomeResource.ResourceManager, SomeOtherResource.ResourceManager)` at application startup using any `ResourceManager` instance or designer-generated resource's `ResourceManager` property.
* [#497](https://github.com/apache/lucenenet/pull/497), [#507](https://github.com/apache/lucenenet/pull/507) - Factored out `Lucene.Net.Support.Time` in favor of `J2N.Time`. Replaced all calls (except `Lucene.Net.Tests.Search.TestDateFilter`) that were `Environment.TickCount` and `Time.CurrentTimeMilliseconds()` to use `Time.NanoTime() / Time.MillisecondsPerNanosecond` for more accurate results. This may break some concurrent applications that are synchronizing with Lucene.NET components using `Environment.TickCount`.
* [#504](https://github.com/apache/lucenenet/pull/504) - `Lucene.Net.Highlighter.VectorHiglight.ScoreOrderFragmentsBuilder.ScoreComparer`: Implemented singleton pattern so the class can only be used via the `Default` property.
* [#502](https://github.com/apache/lucenenet/pull/502) - `Lucene.Net.QueryParser.Flexible.Core.Nodes.IQueryNode`: Added `RemoveChildren()` method from Lucene 8.8.1 to fix broken `RemoveFromParent()` method behavior (applies patch [LUCENE-5805](https://issues.apache.org/jira/browse/LUCENE-5805)). This requires existing `IQueryNode` implementations to implement `RemoveChildren()` and `TryGetTag()`.
* [#502](https://github.com/apache/lucenenet/pull/502) - `Lucene.Net.QueryParser.Flexible.Core.Nodes.IQueryNode`: Added `TryGetTag()` method to simplify looking up a tag by name.
* [#528](https://github.com/apache/lucenenet/pull/528) - `Lucene.Net.Analysis.Stempel.Egothor.Stemmer.MultiTrie`: Changed protected `m_tries` field from `List<Trie>` to `IList<Trie>`
* [#528](https://github.com/apache/lucenenet/pull/528) - `Lucene.Net.Search.BooleanQuery`: Changed protected `m_weights` field from `List<Weight>` to `IList<Weight>`
* [#528](https://github.com/apache/lucenenet/pull/528) - `Lucene.Net.Search.DisjunctionMaxQuery`: Changed protected `m_weights` field from `List<Weight>` to `IList<Weight>`

### Bugs
* [#461](https://github.com/apache/lucenenet/pull/461) - `Lucene.Net.Grouping.GroupingSearch::GroupByFieldOrFunction<TGroupValue>()`: Fixed casting bug of `allGroupsCollector.Groups` by changing the cast to `ICollection` instead of `IList`.
* [#453](https://github.com/apache/lucenenet/pull/453), [#455](https://github.com/apache/lucenenet/pull/455) - lucene-cli: Made `appsettings.json` file optional. This was causing a fatal `FileNotFoundException` after installing lucene-cli without adding an `appsettings.json` file.
* [#464](https://github.com/apache/lucenenet/pull/464) - `Lucene.Net.Codecs.SimpleText.SimpleTextStoredFieldsWriter` + `Lucene.Net.Codecs.SimpleText.SimpleTextTermVectorsWriter`: Fixed `Abort()` methods to correctly swallow any exceptions thrown by `Dispose()` to match the behavior of Lucene 4.8.0.
* [#394](https://github.com/apache/lucenenet/pull/394), [#467](https://github.com/apache/lucenenet/pull/467) - `Lucene.Net` NuGet does not compile under Visual Studio 2017. Downgraded `Lucene.Net.CodeAnalysis.CSharp` and `Lucene.Net.CodeAnalysis.VisualBasic` from .NET Standard 2.0 to .NET Standard 1.3 to fix.
* [#471](https://github.com/apache/lucenenet/pull/471) - Lucene.Net.Documents.FieldType: Corrected documentation to reflect the actual default of `IsTokenaized` as `true` and `NumericType` as `NumericType.NONE`, and to set to `NumericType.NONE` (rather than `null`) if the field has no numeric type.
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Analysis.Common.Util.CharArraySet`: Throw `NotSupportedException` when the set is readonly, not `InvalidOperationException` to match .NET collection behavior
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Codecs.Bloom.BloomFilteringPostingsFormat::FieldsConsumer()`: Throw `NotSupportedException` rather than `InvalidOperationException`
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer::LoadNumeric()`: Throw `AssertionError` rather than `InvalidOperationException`
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Store.CompoundFileDirectory::ReadEntries()`: throw `AssertionError` rather than `InvalidOperationException`
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Util.Packed.DirectPackedReader::Get()`: Throw `AssertionError` rather than `InvalidOperationException`
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Facet`: Throw `InvalidOperationException` rather than `ThreadStateException`
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Grouping.BlockGroupingCollector`: Throw `NotSupportedException` rather than `InvalidOperationException`
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Tests.Index.TestUniqueTermCount`: Throw `NotSupportedException` rather than `InvalidOperationException`
* [#486](https://github.com/apache/lucenenet/pull/486) - Changed all references that were `float.MinValue` and `double.MinValue` to `float.Epsilon` and `double.Epsilon` because those are the .NET equivalent constants to `Float.MIN_VALUE` and `Double.MIN_VALUE` in Java
* [#492](https://github.com/apache/lucenenet/pull/492), [#497](https://github.com/apache/lucenenet/pull/497) - `Lucene.Net.Search.ControlledRealTimeReopenThread` - Fixed time calculation issue that was causing wait to happen for unusually long time periods.
* `Lucene.Net.Tests.Search.TestMultiThreadTermVectors`: Removed stray `[Test]` attribute that was causing extra overhead with no benefit
* [#509](https://github.com/apache/lucenenet/pull/509) - `Lucene.Net.Support.WeakDictionary`: Changed `WeakKey` to use `WeakReference<T>` instead of `WeakReference` to avoid problems with garbage collection
* [#504](https://github.com/apache/lucenenet/pull/504) - `Lucene.Net.Highlighter.VectorHiglight.ScoreOrderFragmentsBuilder.ScoreComparer`: Implemented singleton pattern so the class can only be used via the `Default` property.
* [#506](https://github.com/apache/lucenenet/pull/506), [#509](https://github.com/apache/lucenenet/pull/509) - `Lucene.Net.Index.IndexReader`: Use `ConditionalWeakTable<TKey, TValue>`/`WeakDictionary<TKey, TValue>` to ensure dead elements are pruned and garbage collected
* [#525](https://github.com/apache/lucenenet/pull/525) - Fixed `Lucene.Net.Index.TestIndexWriter::TestThreadInterruptDeadlock()` and `Lucene.Net.Index.TestIndexWriter::TestTwoThreadsInterruptDeadlock()` that were failing due to a difference in .NET `Thread.Interrupt()` behavior. In Java, an `InterruptedException` is never thown from `synchronized (this)` (the equivalent of `lock (this)`), but .NET may throw `ThreadInterruptedException` in cases where there is contention on the lock. The patch fixes our immediate problem of these 2 tests failing and deadlocks occurring, but is only a partial fix. See [#526](https://github.com/apache/lucenenet/pull/526) for an explanation.
* `Lucene.Net.Tests.Suggest.Suggest.Analyzing.TestFreeTextSuggester::TestRandom()`: `LookupResult` calculation in the test was using different order of parentheses than the production code. This bug existed in Java, but apparently the order makes no difference on that platform. This test was getting a false positive because it was using `List<T>.ToString()` to make the result comparison, which J2N's `List<T>` corrects.
* [#529](https://github.com/apache/lucenenet/pull/529) - Fix for .NET Framework x86 Support. The following tests were fixed by using the [`Lucene.Net.Util.NumericUtils::SingleToSortableInt32()`](https://github.com/apache/lucenenet/blob/dd7ed62e9bfc455c9b39ea5d33a783a93280b739/src/Lucene.Net/Util/NumericUtils.cs#L336-L356) method to compare the raw bits for equality. This change doesn't impact performance or behavior of the application as using an approximate float comparison would.
  * `Lucene.Net.Expressions.TestExpressionSorts::TestQueries()`
  * `Lucene.Net.Sandbox.TestSlowFuzzyQuery::TestTieBreaker()`
  * `Lucene.Net.Sandbox.TestSlowFuzzyQuery::TestTokenLengthOpt()`
  * `Lucene.Net.Search.TestBooleanQuery::TestBS2DisjunctionNextVsAdvance()`
  * `Lucene.Net.Search.TestFuzzyQuery::TestTieBreaker()`
  * `Lucene.Net.Search.TestSearchAfter::TestQueries()`
  * `Lucene.Net.Search.TestTopDocsMerge::TestSort_1()`
  * `Lucene.Net.Search.TestTopDocsMerge::TestSort_2()`

### Improvements
* [#284](https://github.com/apache/lucenenet/pull/284) - website: Converted code examples in documentation from Java to C#
* [#300](https://github.com/apache/lucenenet/pull/300) - website: Fixed formatting and many broken links on the website
* **PERFORMANCE:** `Lucene.Net.Tartarus.Snowball`: Refactored to use `Func<bool>` instead of a Reflection call to execute stemmer code as in the original C# port: https://github.com/snowballstem/snowball
* [#461](https://github.com/apache/lucenenet/pull/461), [#475](https://github.com/apache/lucenenet/pull/475) - Added `GroupingSearch` tests to demonstrate usage
* [#453](https://github.com/apache/lucenenet/pull/453), [#455](https://github.com/apache/lucenenet/pull/455) - lucene-cli: Added `appsettings.json` file with the default settings
* [#455](https://github.com/apache/lucenenet/pull/455) - `Lucene.Net.Tests.Cli`: Added InstallationTest to install lucene-cli and run it to ensure it can be installed and has basic functionality.
* [#463](https://github.com/apache/lucenenet/pull/463) - `Lucene.Net.Analysis.OpenNLP`: Updated to OpenNLP 1.9.1.1 and added strong naming support.
* [#465](https://github.com/apache/lucenenet/pull/465) - **PERFORMANCE:** - `Lucene.Net.IndexWriter.ReaderPool`: Swapped in `ConcurrentDictionary<TKey, TValue>` instead of `Dictionary<TKey, TValue>` to take advantage of the fact `ConcurrentDictionary<TKey, TValue>` supports deleting while iterating.
* [#466](https://github.com/apache/lucenenet/pull/466) - **PERFORMANCE:** `Lucene.Net.Queries.Mlt.MoreLikeThis`: Fixed boxing issues with `RetrieveTerms()` and `RetrieveInterestingTerms()` methods by changing `object[]` to a class named `ScoreTerm` (same refactoring as Lucene 8.2.0).
* [#467](https://github.com/apache/lucenenet/pull/467) - `Lucene.Net.CodeAnalysis`: Added `Version.props` file to make it possible to manually bump the assembly number by one revision on any code change (VS requires this, see: dotnet/roslyn[#4381](https://github.com/apache/lucenenet/pull/4381) (comment)).
* website - Updated release documentation.
* [#473](https://github.com/apache/lucenenet/pull/473), [#349](https://github.com/apache/lucenenet/pull/349) - Moved "benchmark" tests that cannot fail to the nightly build to reduce testing time in the normal workflow.
* [#257](https://github.com/apache/lucenenet/pull/257), [#474](https://github.com/apache/lucenenet/pull/474) - Moved the [RandomizedTesting generators](https://github.com/NightOwl888/RandomizedTesting/) to a separate library so they can be reused across projects.
* [#474](https://github.com/apache/lucenenet/pull/474) - `Lucene.Net.TestFramework`: Removed FEATURE_RANDOMIZEDCONTEXT and deleted all files related to [Java randomizedtesting](https://github.com/randomizedtesting/randomizedtesting) that were partially ported bits of its test runner.
* [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.TestFramework`, `Lucene.Net.Support`: Added `[DebuggerStepThrough]` attribute to all assertion methods so the debugger stops in the code that fails the assert not inside of the assert method (affects only internal Lucene.NET development).
* [#446](https://github.com/apache/lucenenet/pull/446), [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Support.ExceptionHandling`: Added `ExceptionExtensions` class with methods named after the Java exception types so future porting efforts can use similar catch blocks with the same behavior as in Java (i.e. `catch (Exception e) when (e.IsIllegalStateException())`.
* [#446](https://github.com/apache/lucenenet/pull/446), [#476](https://github.com/apache/lucenenet/pull/476) - `Lucene.Net.Support.ExceptionHandling`: Added exception classes with the same names as Java exception types so future porting efforts can use similar catch blocks with the same behavior as in Java (i.e `throw IllegalStateException.Create("This is the message")`).
* [#446](https://github.com/apache/lucenenet/pull/446), [#476](https://github.com/apache/lucenenet/pull/476) - Added `Lucene.Net.Tests.AllProjects` project containing tests to confirm that all exceptions thrown by .NET and NUnit are correctly identified by `ExceptionExtensions` methods.
* [#482](https://github.com/apache/lucenenet/pull/482) - `Lucene.Net.Documents.FieldType::Freeze()`: Changed from void return to return this `FieldType` to allow direct chaining of the method in field initializers. Chained the `Freeze()` method in all static field initializers of `Field` subclasses to eliminate extra helper load methods. Marked `BinaryDocValuesField.fType` static field obsolete and added `TYPE` static field (as it was in Lucene).
* [#484](https://github.com/apache/lucenenet/pull/484) - `Lucene.Net.QueryParsers.Flexible.Core.Messages`: Redesigned `QueryParserMessages.cs` so that it is just a facade around a `IResourceProvider` implementation that provides the actual fallback logic. Added a `QueryParserResourceProvider` implementation that can be passed zero to many `ResourceProvider` instances to override and optionally localize the default resource messages.
* [#490](https://github.com/apache/lucenenet/pull/490) - Improved debugger experience for `BytesRef`. In addition to the decimal bytes values it now shows the `BytesRef` as a UTF8 string. If the `BytesRef` is not a UTF8 string that representation will be the string's fingerprint signature.
* [#488](https://github.com/apache/lucenenet/pull/488) - `Lucene.Net.Grouping`: Fix SonarQube's "Any() should be used to test for emptiness" / Code Smell
* [#504](https://github.com/apache/lucenenet/pull/504) - `Lucene.Net.Support`: Factored out `Number` class in favor of using J2N's parsers and formatters
* [#504](https://github.com/apache/lucenenet/pull/504) - `Lucene.Net.Highlighter`: Implemented `IFormattable` and added culture-aware `ToString()` overload to `WeightedPhraseInfo` and `WeightedFragInfo`
* [#504](https://github.com/apache/lucenenet/pull/504) - **PERFORMANCE:** `Lucene.Net.Highlighter`: Use `RemoveAll()` extension method rather than allocating separate collections to track which enumerated items to remove.
* [#499](https://github.com/apache/lucenenet/pull/499) - **PERFORMANCE:** Use overloads of J2N `Parse`/`TryParse` that accept offsets rather than allocating substrings
* [#500](https://github.com/apache/lucenenet/pull/500) - **PERFORMANCE:** Updated collections to use optimized removal methods
* [#501](https://github.com/apache/lucenenet/pull/501) - **PERFORMANCE:** `Lucene.Net.Support.ListExtensions::SubList()`: Factored out in favor of J2N's `List<T>.GetView()` method. Many calls to `List<T>.GetRange()` were updated to `J2N.Collections.Generic.List<T>.GetView()`, which reduces unnecessary allocations.
* [#503](https://github.com/apache/lucenenet/pull/503) - **PERFORMANCE:** `Lucene.Net.Util.UnicodeUtil::ToString()`: Updated to cascade the call to `J2N.Character.ToString()` which has been optimized to use the stack for small strings.
* [#512](https://github.com/apache/lucenenet/pull/512) - Removed `FEATURE_THREAD_YIELD` and `FEATURE_THREAD_PRIORITY`, changed all applicable calls from `Thread.Sleep(0)` back to `Thread.Yield()` as they were in Lucene.
* [#523](https://github.com/apache/lucenenet/pull/523) - Removed several .NET Standard 1.x Features
  - NETSTANDARD1_X
  - FEATURE_CULTUREINFO_GETCULTURES
  - FEATURE_DTD_PROCESSING
  - FEATURE_XSLT
  - FEATURE_STACKTRACE
  - FEATURE_APPDOMAIN_ISFULLYTRUSTED
  - FEATURE_APPDOMAIN_BASEDIRECTORY
  - FEATURE_APPDOMAIN_GETASSEMBLIES
  - FEATURE_METHODBASE_GETMETHODBODY
* [#528](https://github.com/apache/lucenenet/pull/528) - Changed all instances of `System.Collections.Generic.List<T>` to `J2N.Collections.Generic.List<T>`, which is structurally equatable and structurally formattable.
* [#528](https://github.com/apache/lucenenet/pull/528) - **PERFORMANCE**: `Lucene.Net.Util.ListExtensions`: Added optimized path for `J2N.Collections.Generic.List<T>` in `AddRange()` and `Sort()` extension methods
* [#530](https://github.com/apache/lucenenet/pull/530) - Upgraded J2N NuGet package dependency to 2.0.0-beta-0017
* [#530](https://github.com/apache/lucenenet/pull/530) - Upgraded ICU4N NuGet package dependency to 60.1.0-alpha.355
* [#530](https://github.com/apache/lucenenet/pull/530) - Upgraded Morfologik.Stemming package dependency to 2.1.7-beta-0004

### New Features
* [#521](https://github.com/apache/lucenenet/pull/521) - Added target and tests for `net6.0`