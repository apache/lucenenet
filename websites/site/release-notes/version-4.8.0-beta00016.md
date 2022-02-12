---
uid: releasenotes/4.8.0-beta00016
version: 4.8.0-beta00016
---

# Lucene.NET 4.8.0-beta00016 Release Notes

---

> This release contains several important bug fixes and performance enhancements.

## Breaking Index Changes

There are 2 breaking changes that may affect some users when reading indexes that were created from version 4.8.0-beta00015 and all prior 4.8.0 beta versions (not including 3.0.3).
1. A bug was fixed in the generation of segment file names. This only affects users with more than 10 segments in their index.
2. `Lucene.Net.Documents.DateTools` has been modified to return _milliseconds since Unix epoch_ (that is, since Jan 1, 1970 at 00:00:00 UTC) by *default* to match Java Lucene. This only affects users who explicitly use `Lucene.Net.Documents.DateTools` in their application and store the result (in .NET ticks) in their index.

If you are affected by either of the above issues, it is recommended to regenerate your indexes during upgrading. However, if that is not feasible, we have provided the following workarounds.

1. If you have a large index with more than 10 segments, see [#576](https://github.com/apache/lucenenet/pull/576) for details on how to enable legacy segment name support.
2. If you are storing the result of `Lucene.Net.Documents.DateTools.StringToTime(string)` or `Lucene.Net.Documents.DateTools.Round(long)` (a `long`) in your index, you are storing .NET ticks. There are now optional parameters `inputRepresentation` and `outputRepresentation` on these methods to specify whether the `long` value represents .NET ticks, .NET ticks as milliseconds, or millisenonds since the Unix epoch. To exactly match version 4.8.0-beta00015 and prior (including prior major versions):
    - `Lucene.Net.Documents.DateTools.StringToTime(string, NumericRepresentation)` should specify `NumericRepresentation.TICKS` for `outputRepresentation`.
    - `Lucene.Net.Documents.DateTools.Round(long, NumericRepresentation, NumericRepresentation)` should specify `NumericRepresentation.TICKS_AS_MILLISECONDS` for `inputRepresentation` and `NumericRepresentation.TICKS` for `outputRepresentation`.

## .NET Framework Recommendations

It is recommended that all .NET Framework users migrate as soon as possible.

1. In cases where `Lucene.Net.Support.WeakDictionary<TKey, TValue>` was used in .NET Framework and .NET Standard 2.0 due to missing APIs, but there is now a better solution using `Prism.Core`'s weak events in combination with `ConditionalWeakTable<TKey, TValue>`, which means memory management is handled entirely by the GC in `Lucene.Net.Index.IndexReader`, `Lucene.Net.Search.FieldCacheImpl`, `Lucene.Net.Search.CachingWrappingFilter` and `Lucene.Net.Facet.Taxonomy.CachedOrdinalsReader`. See [#613](https://github.com/apache/lucenenet/pull/613).
2. All known issues with loss of floating-point precision on .NET Framework x86 have been fixed.

## Change Log

### Breaking Changes
* [#547](https://github.com/apache/lucenenet/pull/547) - `Lucene.Net.Util.StringHelper.GOOD_FAST_HASH_SEED`: converted from a static field to a property and marked obsolete. Added a new property `GoodFastHashSeed`. Removed `SystemProperties` call to populate the value of the field, since NUnit only allows us to generate a seed per test, and we need a way to inject the seed value for repeatability.
* [#547](https://github.com/apache/lucenenet/pull/547) - `Lucene.Net.TestFramework`: Added `LuceneSetUpFixtureBuilder` class to load either a subclass or our default instance of `LuceneTestFrameworkInitializer`. Also added `LuceneTestCase.SetUpFixture` to control initialization of `LuceneTestFrameworkInitializer` so it is only called on setup and teardown for the assembly. Added `Initialize()` method to `LuceneTestFrameworkInitializer` that *must be used* when setting factories during testing.
* [#547](https://github.com/apache/lucenenet/pull/547) - `Lucene.Net.TestFramework.Util.LuceneTestCase`: Deprecated `GetClassType()` method and added `TestType` property
* [#547](https://github.com/apache/lucenenet/pull/547) - `Lucene.Net.TestFramework.Util.AbstractBeforeAfterRule``: Removed `LuceneTestCase` parameter from `Before()` and `After()` methods.
* [#551](https://github.com/apache/lucenenet/pull/551) - Changed constructors of `Lucene.Net.Util.NumberFormat` and `Lucene.Net.QueryParsers.Flexible.Standard.Config.NumberDateFormat` to accept `IFormatProvider` rather than `CultureInfo` and changed `Lucene.Net.Util.NumberFormat.Culture` property to `Lucene.Net.Util.NumberFormat.FormatProvider`.
* [#554](https://github.com/apache/lucenenet/pull/554) - `Lucene.Net.Misc`: Made `DocFreqComparer` and `TotalTermFreqComparer` into static singletons, only accessible by the `Default` property.
* [#428](https://github.com/apache/lucenenet/pull/428), [#429](https://github.com/apache/lucenenet/pull/429), [#570](https://github.com/apache/lucenenet/pull/570) - `Lucene.Net.Search.FieldComparer`: Redesigned implementation to use reference types for numerics (from J2N) to avoid boxing.
* [#570](https://github.com/apache/lucenenet/pull/570) - `Lucene.Net.Search.FieldCache.IParser`: Renamed method from `TermsEnum()` to `GetTermsEnum()` to match other APIs
* [#570](https://github.com/apache/lucenenet/pull/570) - `Lucene.Net.Queries`: `ObjectVal()` returns a `J2N.Numerics.Number`-derived type rather than a value type cast to object. Direct casts to `int`, `long`, `double`, `single`, etc. will no longer work without first casting to the `J2N.Numerics.Number`-derived type. Alternatively, use the corresponding `Convert.ToXXX()` method for the type you wish to retrieve from the object.
* [#574](https://github.com/apache/lucenenet/pull/574) - `Lucene.Net.Suggest.Fst.FSTCompletionLookup/WFSTCompletionLookup`: Changed `Get()` to return `long?` instead of `object` to eliminate boxing/unboxing
* [#574](https://github.com/apache/lucenenet/pull/574) - `Lucene.Net.Index.MergePolicy::FindForcedMerges()`: Removed unnecessary nullable from `FindForcedMerges()` and all `MergePolicy` subclasses
* [#574](https://github.com/apache/lucenenet/pull/574) - `Lucene.Net.Replicator`: Changed callback signature from `Func<bool?>` to `Action`, since the return value had no semantic meaning
* [#575](https://github.com/apache/lucenenet/pull/575) - `Lucene.Net.Index.DocValuesFieldUpdates`: Refactored so the subclasses will handle getting the values from `DocValuesFieldUpdatesIterator` or `DocValuesUpdate` via a cast rather than boxing the value. Also marked internal (as well as all members of `BufferedUpdates`), since this was not supposed to be part of the public API.
* [#573](https://github.com/apache/lucenenet/pull/573), [#576](https://github.com/apache/lucenenet/pull/576) - Changed segment file names to match Lucene 4.8.0 and Lucene.NET 3.x
* [#577](https://github.com/apache/lucenenet/pull/577) - `Lucene.Net.Index.SegmentInfos`: Changed `Info()` method to an indexer (.NET Convention)
* [#580](https://github.com/apache/lucenenet/pull/580) - `Lucene.Net.Documents.DateTools` - Added `NumericRepresentation` enum to allow converting to/from long in the following formats:
  - Unix Epoch (default): Milliseconds since Jan 1, 1970 12:00:00 AM UTC.
  - Ticks: The raw ticks from `DateTime` or `DateTimeOffset`.
  - Ticks as Milliseconds: This is for compatibility with prior versions of Lucene.NET (3.0.3 and 4.8.0-beta00001 - 4.8.0-beta00015). The conversion done on input values is `time * TimeSpan.TicksPerMillisecond` and the conversion to output values is `time / TimeSpan.TicksPerMillisecond`.

    **The `long` return value from `Lucene.Net.Documents.DateTools.StringToTime(string, NumericRepresentation)` has been changed from `NumericRepresentation.TICKS` to `NumericRepresentation.UNIX_TIME_MILLISECONDS` by default.**

    **The `long` input parameter provided to `Lucene.Net.Documents.DateTools.Round(long, NumericRepresentation, NumericRepresentation)` has been changed from `NumericRepresentation.TICKS_AS_MILLISECONDS` to `NumericRepresentation.UNIX_TIME_MILLISECONDS` by default.**

    **The `long` return value from `Lucene.Net.Documents.DateTools.Round(long, NumericRepresentation, NumericRepresentation)` has changed from `NumericRepresentation.TICKS` to `NumericRepresentation.UNIX_TIME_MILLISECONDS` by default.**
* [#580](https://github.com/apache/lucenenet/pull/580) - `Lucene.Net.Documents.DateTools` - De-nested `Resolution` enum and renamed `DateResolution`.
* [#580](https://github.com/apache/lucenenet/pull/580) - `Lucene.Net.QueryParsers.Flexible.Standard`: Changed numeric nodes to accept and return `J2N.Numerics.Number`-derived types instead of `object`.
* [#581](https://github.com/apache/lucenenet/pull/581) - **SWEEP:** `Lucene.Net.Util.Fst`: Changed API to use `J2N.Numerics.Int64` instead of `long?` for generic closing type as it was designed to use reference equality comparison.
* [#581](https://github.com/apache/lucenenet/pull/581) - **SWEEP:** `Lucene.Net.Util.Fst`: Added class constraints to each generic FST type and reverted to reference equality comparisons.
* [#581](https://github.com/apache/lucenenet/pull/581), [#279](https://github.com/apache/lucenenet/pull/279) - `Lucene.Net.Util.Fst.Int32sRefFSTEnum`: Added `MoveNext()` method and marked `Next()` method obsolete. This change had already been done to BytesRefFSTEnum, which made them inconsistent.
* [#583](https://github.com/apache/lucenenet/pull/583) - `Lucene.Net.QueryParsers.Flexible`: Removed unnecessary nullable value types from `ConfigurationKeys` and configuration setters/getters in `StandardQueryParser`. Added `AbstractQueryConfig.TryGetValue()` method to allow retrieving value types so they can be defaulted properly.
* [#583](https://github.com/apache/lucenenet/pull/583) - `Lucene.Net.Queries.Function.ValueSources.EnumFieldSource::ctor()` - changed `enumIntToStringMap` to accept `IDictionary<int, string>` instead of `IDictionary<int?, string>` (removed unnecessary nullable)
* [#587](https://github.com/apache/lucenenet/pull/587) - `Lucene.Net.TestFramework.Store.MockDirectoryWrapper`: Renamed `AssertNoUnreferencedFilesOnClose` to `AssertNoUnreferencedFilesOnDispose`
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial`: Upgraded to new Spatial4n NuGet package that unifies the types from Spatial4n.Core and Spatial4n.Core.NTS
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial.Prefix.Tree.Cell`: Renamed `m_outerInstance` > `m_spatialPrefixTree` and constructor parameter `outerInstance` > `spatialPrefixTree`
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial.Prefix.AbstractPrefixTreeFilter.BaseTermsEnumTransverser`: renamed `m_outerInstance` > `m_filter`, constructor parameter `outerInstance` > `filter`
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial.Prefix.AbstractPrefixTreeFilter`: De-nested `BaseTermsEnumTraverser`class
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial.Prefix.Tree.GeohashPrefixTree.Factory`: de-nested and renamed `GeohashPrefixTreeFactory`
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial.Prefix.Tree.QuadPrefixTree.Factory`: de-nested and renamed `QuadPrefixTreeFactory`
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial.Prefix.AbstractVisitingPrefixTreeFilter`: De-nested `VisitorTemplate` class and changed protected field `m_prefixGridScanLevel` to a public property named `PrefixGridScanLevel`.
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial.Query`: Renamed `UnsupportedSpatialOperation` > `UnsupportedSpatialOperationException` to match .NET conventions

### Bugs
* [#363](https://github.com/apache/lucenenet/pull/363), [#534](https://github.com/apache/lucenenet/pull/534) - `Lucene.Net.Replicator.Http.HttpReplicatorTest::TestBasic()`. This was failing intermittently due to the Timeout value being set to 1 second instead of the 60 second value that was used in Java. It has been increased to the .NET default of 100 seconds.
* [#363](https://github.com/apache/lucenenet/pull/363), [#534](https://github.com/apache/lucenenet/pull/534) - `Lucene.Net.Replicator.IndexAndTaxonomyReplicationClientTest::TestConsistencyOnExceptions()` and `Lucene.Net.Replicator.IndexReplicationClientTest::TestConsistencyOnExceptions()` were failing due to to exceptions being raised on the worker thread and missing locks, which have both been addressed.
* [#535](https://github.com/apache/lucenenet/pull/535) - Added `[SuppressCodecs]` attribute where required for custom Lucene.NET tests
* [#536](https://github.com/apache/lucenenet/pull/536) - Modified all `TermsEnum.MoveNext()` methods to return a check for `null` rather than returning `true`
* [#537](https://github.com/apache/lucenenet/pull/537) - `Lucene.Net.TestFramework.Index.BasePostingsFormatTestCase`: Removed `IndexOptions.NONE` from the list of available options, since it is not a valid test option
* [#539](https://github.com/apache/lucenenet/pull/539) - `Lucene.Net.Grouping.Term.TermAllGroupHeadsCollector`: Use `NumericUtils.SingleToSortableInt32()` to compare floating point numbers (Fixes `AllGroupHeadCollectorTest.TestRandom()` on .NET Framework x86).
* [#540](https://github.com/apache/lucenenet/pull/540) - `Lucene.Net.Tests.Util.TestPriorityQueue`: Fixed issues with comparers after introducing `J2N.Randomizer`, which produces negative random numbers.
* [#541](https://github.com/apache/lucenenet/pull/541) - `Lucene.Net.Codecs.SimpleText.SimpleTextFieldsReader::NextDoc()`: Fixed assert that was throwing on `BytesRef.Utf8ToString()`
* [#542](https://github.com/apache/lucenenet/pull/542) - `Lucene.Net.Util.Automation.MinimizationOperations::MinimizeHopcroft()`: Fixed range in `OpenBitSet.Clear()`
* [#543](https://github.com/apache/lucenenet/pull/543) - `Lucene.Net.Tests.QueryParser.Flexible.Standard.TestQPHelper`: Use `ParseExact()` method to specify the date format, so it works across cultures.
* [#527](https://github.com/apache/lucenenet/pull/527), [#548](https://github.com/apache/lucenenet/pull/548) - `Lucene.Net.Search.Suggest.Analyzing.BlendedInfixSuggester`: Apply patch from https://issues.apache.org/jira/browse/LUCENE-6093 to fix `ArgumentNullException` if there were discarded trailing characters in the query (Thanks @Maxwellwr)
* [#550](https://github.com/apache/lucenenet/pull/550) - **SWEEP:** `Use `StringComparer.Ordinal` for `Sort()` methods, where appropriate
* [#551](https://github.com/apache/lucenenet/pull/551) - `Lucene.Net.QueryParser.Flexible.Standard`: Fixed calendar handling on .NET Core
* [#552](https://github.com/apache/lucenenet/pull/552) - `Lucene.Net.Suggest.Jaspell.JaspellTernarySearchTree`: Fixed random number generator so it produces random numbers
* [#553](https://github.com/apache/lucenenet/pull/553), [#609](https://github.com/apache/lucenenet/pull/609) - `Lucene.Net.TestFramework.Util.TestUtil::RandomAnalysisString()`: Fixed `ArgumentOutOfRangeException` when passed a `maxLength` of 0.
* [#546](https://github.com/apache/lucenenet/pull/546), [#557](https://github.com/apache/lucenenet/pull/557) - `Lucene.Net.Search.DisjunctionMaxScorer`: Fixed x86 floating point precision issue on .NET Framework
* [#558](https://github.com/apache/lucenenet/pull/558) - `Lucene.Net.Expressions.ScoreFunctionValues`: Fixed x86 floating point precision issue on .NET Framework
* [#559](https://github.com/apache/lucenenet/pull/559) - `Lucene.Net.Spatial.Prefix.SpatialOpRecursivePrefixTreeTest`: Ported over patch from https://github.com/apache/lucene/commit/e9906a334b8e123e93b917c3feb6e55fed0a8c57 (from 4.9.0).
* [#545](https://github.com/apache/lucenenet/pull/545), [#565](https://github.com/apache/lucenenet/pull/565) - `Lucene.Net.Index.TestDuelingCodecs::TestEquals()`: There was a missing ! in `Lucene.Net.Codecs.BlockTreeTermsReader.IntersectEnum.Frame::Load()` that was inverting the logic, causing this test to fail intermittently.
* [#549](https://github.com/apache/lucenenet/pull/549), [#566](https://github.com/apache/lucenenet/pull/566) - `Lucene.Net.Search.TestJoinUtil::TestMultiValueRandomJoin()`: Fixed x86 floating point precision issue on .NET Framework
* [#568](https://github.com/apache/lucenenet/pull/568) - `Lucene.Net.Search.Spell.TestSpellChecker::TestConcurrentAccess()`: Fixed issues that were causing the test to hang due to concurrency problems.
* [#513](https://github.com/apache/lucenenet/pull/513), [#572](https://github.com/apache/lucenenet/pull/572) - Updated `ControlledRealTimeReopenThread` to correctly handle timing (thanks @rclabo)
* `Lucene.Net.Support.Collections.ReverseComparer<T>`: Replaced `CaseInsensitiveComparer` with `J2N.Collections.Generic.Comparer<T>`. This only affects tests.
* [#597](https://github.com/apache/lucenenet/pull/597) - `.github/workflows`: Updated website/documentation configs to use subdirectory glob patterns for paths.
* [#598](https://github.com/apache/lucenenet/pull/598) - Website: Fixed codeclimber article broken links
* [#600](https://github.com/apache/lucenenet/pull/600) - Fixed broken book link for Instant Lucene.NET (Thanks @rclabo)
* [#606](https://github.com/apache/lucenenet/pull/606) - `Lucene.Net.Search.FieldCacheImpl.Cache<TKey, TValue>::Put()`: Logic was inverted on `innerCache` field so the value was being updated if exists, when it should not be updated in this case
* [#606](https://github.com/apache/lucenenet/pull/606) - `Lucene.Net.Search.FieldCacheImpl::Cache<TKey, TValue> (Put + Get)`: Fixed issue with `InitReader()` being called prior to adding the item to the cache when it should be called after
* [#619](https://github.com/apache/lucenenet/pull/619) - `Lucene.Net.Spatial.Query.SpatialArgs::ctor()`: Set `operation` and `shape` fields rather than calling the virtual properties to set them (which can cause initialization issues for subclasses)

### Improvements
* [#538](https://github.com/apache/lucenenet/pull/538) - `Lucene.Net.TestFramework.Search.CheckHits::CheckHitCollector()`: Removed unnecessary call to `Convert.ToInt32()` and simplified collection initialization.
* [#554](https://github.com/apache/lucenenet/pull/554) - **SWEEP:** Made stateless private sealed comparers into singletons to reduce allocations (unless they already have a static property)
* [#555](https://github.com/apache/lucenenet/pull/555), [#526](https://github.com/apache/lucenenet/pull/526) - Deprecated support for `System.Threading.Thread.Interrupt()` when writing indexes due to the high possibility in .NET that it could break a `Commit()` or cause a deadlock.
* [#567](https://github.com/apache/lucenenet/pull/567) - Enabled `[Serializable]` exceptions on all target platforms (previously, exceptions were not serializable in .NET Core)
* [#274](https://github.com/apache/lucenenet/pull/274), [LUCENENET-574](https://issues.apache.org/jira/browse/LUCENENET-574), [#567](https://github.com/apache/lucenenet/pull/567) - Removed `[Serializable]` support for all classes except for the following (See [#567](https://github.com/apache/lucenenet/pull/567) for a complete list)
   * Exceptions
   * Collections
   * Low-level holder types (such as BytesRef, CharsRef, etc.)
   * Stateless `IComparer<T>` implementations that are publicly exposed directly or through collections
* [#568](https://github.com/apache/lucenenet/pull/568) - `Lucene.Net.TestFramework.Util.LuceneTestCase::NewSearcher()`: Added missing event handler to shut down `LimitedConcurrencyLevelTaskScheduler` to prevent it from accepting new work when we are attempting to end the background process.
* [#568](https://github.com/apache/lucenenet/pull/568) - `Lucene.Net.Support`: Factored out `ICallable<V>` and `ICompletionService<V>` interfaces, as they are not needed
* [#570](https://github.com/apache/lucenenet/pull/570) - **PERFORMANCE:** `Lucene.Net.Search.NumericRangeQuery`: Eliminated boxing when converting from T to the numeric type and when comparing equality
* [#570](https://github.com/apache/lucenenet/pull/570) - **PERFORMANCE:** `Lucene.Net.Suggest.Jaspell`: Use J2N numeric types to eliminate boxing
* [#570](https://github.com/apache/lucenenet/pull/570) - **PERFORMANCE:** `Lucene.Net.Search.FieldCache`: Use J2N parsers and formatters
* [#570](https://github.com/apache/lucenenet/pull/570) - **PERFORMANCE:** `Lucene.Net.Classification.Utils.DatasetSplitter`: Removed duplicate calls to field methods and stored values in local variables. Use default round-trip format from J2N.
* [#570](https://github.com/apache/lucenenet/pull/570) - **PERFORMANCE:** `Lucene.Net.Search.FieldCacheRangeFilter`: Use `HasValue` and `Value` for nullable value types rather casting and comparing to null
* [#574](https://github.com/apache/lucenenet/pull/574), [#583](https://github.com/apache/lucenenet/pull/583) - **SWEEP:** - Removed unnecessary nullable value types
* [#578](https://github.com/apache/lucenenet/pull/578) - `Lucene.Net.Facet`: Added culture-sensitve `ToString()` overload on `FacetResult` and `LabelAndValue`
* [#578](https://github.com/apache/lucenenet/pull/578) - `Lucene.Net.Facet.FacetResult`: Added nullable reference type support
* [#579](https://github.com/apache/lucenenet/pull/579) - `Lucene.Net.Facet.DrillDownQuery`: Added collection initializer support
* [#580](https://github.com/apache/lucenenet/pull/580) - `Lucene.Net.Documents.DateTools` - Added support for `TimeZoneInfo` when converting to/from string
* [#580](https://github.com/apache/lucenenet/pull/580) - `Lucene.Net.QueryParsers.Flexible.Standard.Config.NumberDateFormat`: Added constructor overload to format a date without a time.
* [#580](https://github.com/apache/lucenenet/pull/580) - `Lucene.Net.QueryParsers.Flexible.Standard.Config.NumberDateFormat`: Added `NumericRepresentation` property to set the representation to use for both `Format()` and `Parse()`.
* [#580](https://github.com/apache/lucenenet/pull/580) - `Lucene.Net.QueryParsers` - Added support for `TimeZoneInfo` when converting to/from string (Classic and Flexible query parsers)
* [#580](https://github.com/apache/lucenenet/pull/580) - `Lucene.Net.QueryParsers.Classic.QueryParserBase`: Use `TryParse()` instead of `Parse()` to parse numeric values. Use the current culture, but fall back to invariant culture.
* [#582](https://github.com/apache/lucenenet/pull/582) - **PERFORAMANCE:** `Lucene.Net.Search.FieldCacheRangeFilter`: Eliminated boxing in `Equals()` check
* [#584](https://github.com/apache/lucenenet/pull/584) - `Lucene.Net.Expressions.SimpleBindings`: Added collection initializer support. Updated `DistanceFacetsExample` and `ExpressionAggregationFacetsExample` to demonstrate usage.
* [#586](https://github.com/apache/lucenenet/pull/586) - **SWEEP:** Removed conditional compilation for MSTest/xUnit and the following features:
   - TESTFRAMEWORK_MSTEST
   - TESTFRAMEWORK_NUNIT
   - TESTFRAMEWORK_XUNIT
   - FEATURE_INSTANCE_TESTDATA_INITIALIZATION
   - FEATURE_INSTANCE_CODEC_IMPERSONATION
* [#587](https://github.com/apache/lucenenet/pull/587) - Fixed the documentation comments for `LuceneTestCase`
* [#587](https://github.com/apache/lucenenet/pull/587) - Added some documentation for random seed configuration
* [#587](https://github.com/apache/lucenenet/pull/587) - Implemented some missing console logging
* [#588](https://github.com/apache/lucenenet/pull/588) - `lucene-cli`: Added embedded readme to NuGet package and updated build to update docs with release version number
* [#590](https://github.com/apache/lucenenet/pull/590) - **SWEEP:** Added links to release notes and documentation in each NuGet package, and corrected package descriptions.
* [#594](https://github.com/apache/lucenenet/pull/594) - Website: Improved content of contributing/source code page to show current information about the Apache's two-master setup and provided additional information about contributing source code with many links to external references. (Thanks @rclabo)
* [#595](https://github.com/apache/lucenenet/pull/595) - Website: Added "How to Setup Java Debugging" page. (Thanks @rclabo)
* [#602](https://github.com/apache/lucenenet/pull/602) - Shifted most of the `IndexWriter` tests to `Lucene.Net.Tests._I-J` to make both `Lucene.Net.Tests._E-I` and `Lucene.Net.Tests._I-J` run less than 2 minutes. This cuts the total time on Azure DevOps by around 5 minutes.
* [#603](https://github.com/apache/lucenenet/pull/603), [#601](https://github.com/apache/lucenenet/pull/601) - Upgraded build tools for `LuceneDocsPlugins` project
* Upgraded J2N NuGet package dependency to 2.0.0
* Upgraded ICU4N NuGet package dependency to 60.1.0-alpha.356
* Upgraded RandomizedTesting.Generators NuGet package dependency to 2.7.8
* Upgraded Morfologik.Stemming NuGet package dependency to 2.1.7
* [#611](https://github.com/apache/lucenenet/pull/611) - **PERFORMANCE:** Fixed `NIOFSDirectory` bottleneck on multiple instances by switching from a static shared lock to a lock per `FileStream` instance.
* [#611](https://github.com/apache/lucenenet/pull/611) - `Lucene.Net.Store`: Updated the `FSDirectory` documentation to remove irrelevant Java info and replace it with performance characteristics of the .NET implementation.
* [#613](https://github.com/apache/lucenenet/pull/613), [#256](https://github.com/apache/lucenenet/pull/256), [#604](https://github.com/apache/lucenenet/pull/604), [#605](https://github.com/apache/lucenenet/pull/605) - **PERFORMANCE:** Factored out `WeakDictionary<TKey, TValue>` in favor of weak events using [Prism.Core](https://github.com/PrismLibrary/Prism)
* [#617](https://github.com/apache/lucenenet/pull/617) - **SWEEP:** Changed "== null" to "is null"
* [#619](https://github.com/apache/lucenenet/pull/619) - **SWEEP:** `Lucene.Net.Spatial`: Enabled nullable reference type support
* [#619](https://github.com/apache/lucenenet/pull/619) - **SWEEP:** `Lucene.Net.Spatial`: Added guard clauses, where appropriate

### New Features
* [#288](https://github.com/apache/lucenenet/pull/288), [#547](https://github.com/apache/lucenenet/pull/547) - `Lucene.Net.TestFramework`: Fixed random seed functionality so it is repeatable, so random tests can be more easily debugged. The random seed and how to configure a test assembly to repeat the same result is appended to the output message of the test (which becomes visible upon failure). The `J2N.Randomizer` class was used to provide random numbers, which uses the same implementation on every OS, so the random seeds are portable across operating systems.
* [#588](https://github.com/apache/lucenenet/pull/588), [#612](https://github.com/apache/lucenenet/pull/612) - `lucene-cli`: Added multitarget support for .NET Core 3.1, .NET 5.0, and .NET 6.0
* [#592](https://github.com/apache/lucenenet/pull/592) - Added [Source Link](https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) support and added documentation page to the API docs.
* [#593](https://github.com/apache/lucenenet/pull/593), [#596](https://github.com/apache/lucenenet/pull/596), [#364](https://github.com/apache/lucenenet/pull/364) - Added Cross-Platform Build Script