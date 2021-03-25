---
uid: releasenotes/4.8.0-beta00014
version: 4.8.0-beta00014
---

# Lucene.NET 4.8.0-beta00014 Release Notes

---

> This release contains bug fixes and minor performance improvements

## Change Log

### Breaking Changes
* [#424](https://github.com/apache/lucenenet/pull/424) - Deprecated `TaskMergeScheduler`, a merge scheduler that was added to support .NET Standard 1.x
* [#424](https://github.com/apache/lucenenet/pull/424) - `Lucene.Net.TestFramework`: Removed the public `LuceneTestCase.ConcurrentMergeSchedulerFactories` class

### Bugs
* [#405](https://github.com/apache/lucenenet/pull/405), [#415](https://github.com/apache/lucenenet/pull/415) - `Lucene.Net.Index.DocTermOrds`: Fixed issue with enumerator (`OrdWrappedTermsEnum`) incorrectly returning `true` when the value is `null`.
* [#427](https://github.com/apache/lucenenet/pull/427) - `Lucene.Net.Analysis.Common`: Fixed `TestRollingCharBuffer::Test()` to prevent out of memory exceptions when running with `Verbose` enabled
* [#434](https://github.com/apache/lucenenet/pull/434), [#418](https://github.com/apache/lucenenet/pull/418) - Hunspell affixes' file parsing corrupts some affixes' conditions
* [#434](https://github.com/apache/lucenenet/pull/434), [#419](https://github.com/apache/lucenenet/pull/419) - `HunspellStemFilter` does not work with zero affix
* [#439](https://github.com/apache/lucenenet/pull/439) - `Lucene.Net.Facet.Taxonomy.CachedOrdinalsReader`: Fixed synchronization issue between adding new items to the cache and reading `RamBytesUsed` method
* [#439](https://github.com/apache/lucenenet/pull/439), [#417](https://github.com/apache/lucenenet/pull/417), [#319](https://github.com/apache/lucenenet/pull/319) -  `Lucene.Net.Spatial.Util.ShapeFieldCacheProvider`: Fixed atomicity issue with loading the cache by using `Lazy<T>`.
* [#441](https://github.com/apache/lucenenet/pull/441) - ` Lucene.Net.TestFramework.Support.Confguration.TestConfigurationFactory`: Use `Lazy<T>` to ensure the `configurationCache.GetOrAdd()` factory is atomic.
* [#441](https://github.com/apache/lucenenet/pull/441) - `Lucene.Net.TestFramework.Search.ShardSearchingTestBase: Fixed possible `KeyNotFoundException` when getting the value from `collectionStatisticsCache`
* [#441](https://github.com/apache/lucenenet/pull/441), [#417](https://github.com/apache/lucenenet/pull/417), [#319](https://github.com/apache/lucenenet/pull/319) - `Lucene.Net.Spatial.Prefix.PrefixTreeFactory`: Use `Lazy<T>` in `ConcurrentDictionary` to make the `valueFactory` atomic.
* [#443](https://github.com/apache/lucenenet/pull/443) - `Lucene.Net.Benchmark.ByTask.Feeds.SpatialDocMaker: Since `Dictionary<TKey, TValue>.this[key]` is not marked virtual in .NET, subclassing `Dictionary<string, string>` is not a valid approach. So we implement `IDictionary<string, string>` instead.
* [#416](https://github.com/apache/lucenenet/pull/416) - CLI Documentation issue - environment variable token not replaced.
* [#450](https://github.com/apache/lucenenet/pull/450) - `Lucene.Net.Facet` - Reverted locking in to the state it was in Lucene 4.8.1, however we are still making use of `ReaderWriterLockSlim` to improve read performance of caches. Also, removed the 1 second lock timeout from `Cl2oTaxonomyWriterCache`.

### Improvements
* [#269](https://github.com/apache/lucenenet/pull/269) - Added `[AwaitsFix]` attribute to known failing tests
* [#391](https://github.com/apache/lucenenet/pull/391) - Improved plugins in DocFx when generating API docs
* [#392](https://github.com/apache/lucenenet/pull/392) - Enabled GitHub Actions to Run Tests on Pull Request
* [#395](https://github.com/apache/lucenenet/pull/395) - Improved performance of build pipeline by publishing the whole solution in one step instead of one project at a time
* [#395](https://github.com/apache/lucenenet/pull/395) - Fixed dependency NuGet package version conflicts
* [#395](https://github.com/apache/lucenenet/pull/395) - Added crash and hang detection to the test runs
* [#395](https://github.com/apache/lucenenet/pull/395) - Upgraded to the latest `dotnet` CLI commands `dotnet build` and `dotnet test` rather than `dotnet msbuild` and `dotnet vstest`
* [#411](https://github.com/apache/lucenenet/pull/411), [#259](https://github.com/apache/lucenenet/pull/259) - Reviewed tests for `Lucene.Net.Tests.Facet`
* [#412](https://github.com/apache/lucenenet/pull/412), [#406](https://github.com/apache/lucenenet/pull/406) - Upgraded NUnit to 3.13.1 and NUnit3TestAdapter to 3.17.0 to make `Console.WriteLine()` work in unit tests.
* [#414](https://github.com/apache/lucenenet/pull/414), [#259](https://github.com/apache/lucenenet/pull/259) - Review of tests for `Lucene.Net.Tests.Join`
* [#420](https://github.com/apache/lucenenet/pull/420), [#259](https://github.com/apache/lucenenet/pull/259) - Review of tests for `Lucene.Net.Tests.Classification`
* [#422](https://github.com/apache/lucenenet/pull/422) - `Lucene.Net.Classification`: Removed leading underscore from private/internal member variables
* [#423](https://github.com/apache/lucenenet/pull/423) - Reduced casting
* [#423](https://github.com/apache/lucenenet/pull/423) - `azure-pipelines.yml`: Added `RunX86Tests` option to explicitly enable x86 tests without having to run a full nightly build
* [#425](https://github.com/apache/lucenenet/pull/425), [#259](https://github.com/apache/lucenenet/pull/259) - Review of tests for `Lucene.Net.Tests.Codecs`
* [#426](https://github.com/apache/lucenenet/pull/426) - Changed multiple naming conventions of anonymous classes to just use the suffix `AnonymousClass`
* [#426](https://github.com/apache/lucenenet/pull/426) - Changed accessibility of anonymous classes to `private`
* [#427](https://github.com/apache/lucenenet/pull/427), [#259](https://github.com/apache/lucenenet/pull/259) - Review of tests for `Lucene.Net.Tests.Queries`
* [#433](https://github.com/apache/lucenenet/pull/433), [#430](https://github.com/apache/lucenenet/pull/430) - Removed `FEATURE_CLONEABLE` and the MSBuild property `IncludeICloneable`
* [#435](https://github.com/apache/lucenenet/pull/435), [#259](https://github.com/apache/lucenenet/pull/259) - Review of tests for `Lucene.Net.Tests.Expressions`
* [#438](https://github.com/apache/lucenenet/pull/438) - Don't insert extra newline in TFIDFSim's score explanation (this minor change had already been done to Lucene 5.0, so we are back-porting it to 4.8.0)
* [#439](https://github.com/apache/lucenenet/pull/439) -  `Lucene.Net.Util.VirtualMethod`: Removed unnecessary call to `Convert.ToInt32()`
* [#439](https://github.com/apache/lucenenet/pull/439) - `Lucene.Net.Util.AttributeSource`: Restored comment from Lucene indicating it doesn't matter if multiple threads compete to populate the `ConditionalWeakTable`.
* [#440](https://github.com/apache/lucenenet/pull/440) - **SWEEP**: Reviewed catch blocks and made improvements to preserve stack details.
* [#441](https://github.com/apache/lucenenet/pull/441), [#417](https://github.com/apache/lucenenet/pull/417) -  `Lucene.Net.Analysis.OpenNLP.Tools.OpenNLPOpsFactory`: Simplified logic by using `GetOrAdd()` instead of `TryGetValue`.
* [#441](https://github.com/apache/lucenenet/pull/441) - ` Lucene.Net.TestFramework.Util` (`LuceneTestCase` + `TestUtil`): Refactored the `CleanupTemporaryFiles()` method to be more in line with the original Java implementation, including not allowing new files/directories to be added to the queue concurrently with the deletion process.
* [#441](https://github.com/apache/lucenenet/pull/441) - **PERFORMANCE:** ` Lucene.Net.Join.ToParentBlockJoinCollector`: Changed from `ConcurrentQueue<T>` to `Queue<T>` because we are dealing with a collection declared within the same method so there is no reason for the extra overhead.
* [#441](https://github.com/apache/lucenenet/pull/441) - **PERFORMANCE:** ` Lucene.Net.Tests.Suggest.Spell.TestSpellChecker`: Replaced `ConcurrentBag<T>` with ConcurrentQueue<T> because we need to be sure the underlying implementation guarantees order and the extra call to `Reverse()` was just slowing things down.
* [#441](https://github.com/apache/lucenenet/pull/441) - ` Lucene.Net.TestFramework.Search.ShardSearchingTestBase`: Display the contents of the collection to the console using `Collections.ToString()`.
* [#441](https://github.com/apache/lucenenet/pull/441) - ` Lucene.Net.Search.SearcherLifetimeManager: Added comment to indicate the reason we use `Lazy<T>` is to make the create operation atomic.
* [#441](https://github.com/apache/lucenenet/pull/441) - ` Directory.Build.Targets`: Added `FEATURE_DICTIONARY_REMOVE_CONTINUEENUMERATION` so we can support this feature in .NET 5.x + when we add a target.
* [#442](https://github.com/apache/lucenenet/pull/442) - **PERFORMANCE:** `Lucene.Net.Search.Suggest.Fst.FSTCompletion`: Use `Stack<T>` rather than `List<T>.Reverse()`. Also, removed unnecessary lock in `CheckExistingAndReorder()`, as it is only used in a single thread at a time.
* [#442](https://github.com/apache/lucenenet/pull/442) - **PERFORMANCE:** `Lucene.Net.Search.Suggest.SortedInputEnumerator`: Removed unnecessary call to `Reverse()` and allocation of `HashSet<T>`
* [#444](https://github.com/apache/lucenenet/pull/444), [#272](https://github.com/apache/lucenenet/pull/272) - **PERFORMANCE:** `Lucene.Net.Search.FieldCacheImpl`: Reverted locking back to the state of Lucene 4.8.0.
* [#445](https://github.com/apache/lucenenet/pull/445) - Removed `FEATURE_THREAD_INTERRUPT` since all supported targets now support thread interrupts. Note also that Lucene *depends* on thread interrupts to function properly, so disabling this feature would be invalid.
* [#448](https://github.com/apache/lucenenet/pull/448) - **DOCS:** Added migration guide for users migrating from Lucene.NET 3.0.3 to Lucene.NET 4.8.0.
* [#396](https://github.com/apache/lucenenet/pull/396) - **DOCS:** Create branching scheme to track changes in docuentation between different Lucene versions and removed the `JavaDocToMarkdownConverter` tool from the normal build workflow of the API docs. This frees us up to update the "namespace" documentation with .NET-specific information and code examples.
* Upgraded J2N NuGet package dependency to 2.0.0-beta-0012
* Upgraded ICU4N NuGet package dependency to 60.1.0-alpha.254
* Upgraded Morfologik.Stemming package dependency to 2.1.7-beta-0002

### New Features
* [#385](https://github.com/apache/lucenenet/pull/385), [#362](https://github.com/apache/lucenenet/pull/362) - `Lucene.Net.Documents.Document`: Added culture-sensitive overloads of `GetValues()`, `Get()` and `GetStringValue()` that accept  `format` and `IFormatProvider` and implemented `IFormattable` on `Document` and `LazyDocument`.
* [#404](https://github.com/apache/lucenenet/pull/404) - Added `Commit()` method to `AnalyzingInfixSuggester` (from [LUCENE-5889](https://issues.apache.org/jira/browse/LUCENE-5889))