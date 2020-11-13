---
uid: releasenotes/4.8.0-beta00013
version: 4.8.0-beta00013
---

# Lucene.NET 4.8.0-beta00013 Release Notes

---

> This release contains important bug fixes and performance enhancements.

## Benchmarks (from [#310](https://github.com/apache/lucenenet/pull/310))

#### Index Files
<details>
  <summary>Click to expand</summary>

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8850H CPU 2.60GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100
  [Host]          : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00005 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00006 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00007 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00008 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00009 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00010 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00011 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00012 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00013 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

InvocationCount=1  IterationCount=15  LaunchCount=2  
UnrollFactor=1  WarmupCount=10  

```
|     Method |             Job |     Mean |    Error |   StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|----------- |---------------- |---------:|---------:|---------:|-----------:|----------:|----------:|----------:|
| IndexFiles | 4.8.0-beta00005 | 628.1 ms |  8.41 ms | 12.05 ms | 43000.0000 | 8000.0000 | 7000.0000 | 220.82 MB |
| IndexFiles | 4.8.0-beta00006 | 628.3 ms | 13.19 ms | 19.33 ms | 44000.0000 | 8000.0000 | 7000.0000 | 220.67 MB |
| IndexFiles | 4.8.0-beta00007 | 617.2 ms |  8.44 ms | 11.83 ms | 44000.0000 | 8000.0000 | 7000.0000 | 220.73 MB |
| IndexFiles | 4.8.0-beta00008 | 620.6 ms |  5.62 ms |  8.41 ms | 44000.0000 | 8000.0000 | 7000.0000 | 221.06 MB |
| IndexFiles | 4.8.0-beta00009 | 632.8 ms | 12.57 ms | 18.43 ms | 44000.0000 | 8000.0000 | 7000.0000 | 220.95 MB |
| IndexFiles | 4.8.0-beta00010 | 862.3 ms | 51.13 ms | 74.95 ms | 44000.0000 | 8000.0000 | 7000.0000 | 221.22 MB |
| IndexFiles | 4.8.0-beta00011 | 636.5 ms | 11.06 ms | 15.87 ms | 44000.0000 | 8000.0000 | 7000.0000 | 221.09 MB |
| IndexFiles | 4.8.0-beta00012 | 668.8 ms | 14.78 ms | 21.66 ms | 56000.0000 | 7000.0000 | 6000.0000 | 286.63 MB |
| IndexFiles | 4.8.0-beta00013 | 626.7 ms |  7.78 ms | 10.91 ms | 43000.0000 | 8000.0000 | 7000.0000 |  219.8 MB |

</details>

#### Search Files
<details>
  <summary>Click to expand</summary>

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8850H CPU 2.60GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100
  [Host]          : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00005 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00006 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00007 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00008 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00009 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00010 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00011 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00012 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  4.8.0-beta00013 : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
|      Method |             Job |     Mean |   Error |   StdDev |      Gen 0 |     Gen 1 | Gen 2 | Allocated |
|------------ |---------------- |---------:|--------:|---------:|-----------:|----------:|------:|----------:|
| SearchFiles | 4.8.0-beta00005 | 274.8 ms | 7.01 ms | 10.28 ms | 18000.0000 | 1000.0000 |     - |  82.12 MB |
| SearchFiles | 4.8.0-beta00006 | 283.4 ms | 7.78 ms | 11.64 ms | 18000.0000 | 1000.0000 |     - |  82.13 MB |
| SearchFiles | 4.8.0-beta00007 | 291.5 ms | 8.91 ms | 13.33 ms | 18000.0000 | 1000.0000 |     - |   81.9 MB |
| SearchFiles | 4.8.0-beta00008 | 162.3 ms | 5.50 ms |  8.23 ms | 17000.0000 | 1000.0000 |     - |  80.13 MB |
| SearchFiles | 4.8.0-beta00009 | 165.6 ms | 2.61 ms |  3.90 ms | 17000.0000 |         - |     - |  80.13 MB |
| SearchFiles | 4.8.0-beta00010 | 159.4 ms | 2.84 ms |  4.17 ms | 17000.0000 | 1000.0000 |     - |  79.85 MB |
| SearchFiles | 4.8.0-beta00011 | 160.8 ms | 1.93 ms |  2.77 ms | 17000.0000 | 1000.0000 |     - |  79.85 MB |
| SearchFiles | 4.8.0-beta00012 | 169.2 ms | 6.48 ms |  9.49 ms | 18000.0000 | 1000.0000 |     - |  81.11 MB |
| SearchFiles | 4.8.0-beta00013 | 161.6 ms | 3.28 ms |  4.80 ms | 14000.0000 | 1000.0000 |     - |  65.78 MB |

</details>

## Change Log

### Breaking Changes
* `Lucene.Net.Search.FieldCache`: Added interface `ICreationPlaceholder` and changed `CreationPlaceholder` class to `CreationPlaceHolder<TValue>`.

### Bugs
* [#356](https://github.com/apache/lucenenet/pull/356) - `Lucene.Net.Store.NativeFSLockFactory`: Modified options to allow read access on non-Windows operating systems. This caused the copy constructor of `RAMDirectory` to throw "The process cannot access the file 'file path' because it is being used by another process" excpetions.
* [#296](https://github.com/apache/lucenenet/pull/296) - `Lucene.Net.Util.Automaton.State`: Removed `Equals()` implementation; it was intended to use reference equality as a unique key. This caused random `IndexOperationException`s to occur when using `FuzzyTermsEnum`/`FuzzyQuery`.
* [#387](https://github.com/apache/lucenenet/pull/387) - Fixed formatting in `ArgumentException` message for all analyzer factories so it will display the dictionary contents
* [#387](https://github.com/apache/lucenenet/pull/387) - Lucene.Net.Util.ExceptionExtensions.GetSuppressedAsList(): Use `J2N.Collections.Generic.List<T>` so the call to `ToString()` will automatically list the exception messages
* [#387](https://github.com/apache/lucenenet/pull/387) - `Lucene.Net.TestFramework.Analysis.MockTokenizer`: Pass the `AttributeFactory` argument that is provided as per the documentation comment. Note this bug exists in Lucene 4.8.0, also.
* [#387](https://github.com/apache/lucenenet/pull/387) - `Lucene.Net.Analysis.Common.Tartarus.Snowball.Among`: Fixed `MethodObject` property to return private field instead of itself
* [#387](https://github.com/apache/lucenenet/pull/387) - `Lucene.Net.Document.CompressionTools`: Pass the offset and length to the underlying `MemoryStream`
* [#388](https://github.com/apache/lucenenet/pull/388) - Downgraded minimum required `Microsoft.Extensions.Configuration` version to 2.0.0 on .NET Standard 2.0 and 2.1

### Improvements
* Updated code examples on website home page
  1. Show cross-OS examples of building `Directory` paths
  2. Demonstrate where to put `using` statements
  3. Removed LinqPad's `Dump()` method and replaced with `Console.WriteLine()` for clarity
  4. Fixed syntax error in initialization example of `MultiPhraseQuery`
* Upgraded NuGet dependency J2N to 2.0.0-beta-0010
* Upgraded NuGet dependency ICU4N to 60.1.0-alpha.353
* Upgraded NuGet dependency Morfologik.Stemming to 2.1.7-beta-0001
* [#344](https://github.com/apache/lucenenet/pull/344) - **PERFORMANCE**: `Lucene.Net.Search.FieldCacheImpl`: Removed unnecessary dictionary lookup
* [#352](https://github.com/apache/lucenenet/pull/352) - Added Azure DevOps tests for x86 on all platforms
* [#348](https://github.com/apache/lucenenet/pull/348) - **PERFORMANCE**: Reduced `FieldCacheImpl` casting/boxing
* [#355](https://github.com/apache/lucenenet/pull/355) - Setup nightly build (https://dev.azure.com/lucene-net/Lucene.NET/_build?definitionId=4)
* **PERFORMANCE**: `Lucene.Net.Util.Automaton.SortedInt32Set`: Removed unnecessary `IEquatable<T>` implementations and converted `FrozenInt32Set` into a struct.
* **PERFORMANCE**: `Lucene.Net.Util.Bits`: Removed unnecessary `GetHashCode()` method from `MatchAllBits` and `MatchNoBits` (didn't exist in Lucene)
*  `Lucene.Net.Util.Counter`: Changed Get() to Value property and added implicit operator.
* [#361](https://github.com/apache/lucenenet/pull/361) - Make `CreateDirectory()` method virtual so that derived classes can provide their own `Directory` implementation, allowing for benchmarking of custom `Directory` providers (e.q LiteDB)
* [#346](https://github.com/apache/lucenenet/pull/346), [#383](https://github.com/apache/lucenenet/pull/383) - **PERFORMANCE**: Change delegate overloads of `Debugging.Assert()` to use generic parameters and `string.Format()` to reduce allocations. Use `J2N.Text.StringFormatter` to automatically format arrays and collections so the processing of converting it to a string is deferred until an assert fails.
* [#296](https://github.com/apache/lucenenet/pull/296) - **PERFORMANCE**: `Lucene.Net..Index`: Calling `IndexOptions.CompareTo()` causes boxing. Added new `IndexOptionsComparer` class to be used in codecs instead.
* [#387](https://github.com/apache/lucenenet/pull/387) - Fixed or Suppressed Code Analysis Rules
  * [CA1012](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1012): Abstract types should not have constructors
  * [CA1052](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1052): Static holder types should be Static or NotInheritable
  * [CA1063](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1063): Implement IDisposable Properly (except for IndexWriter). Partially addresses [#265](https://github.com/apache/lucenenet/pull/265).
  * [CA1507](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1507): Use nameof instead of string ([#366](https://github.com/apache/lucenenet/pull/366))
  * [CA1802](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1802): Use Literals Where Appropriate
  * [CA1810](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1810): Initialize reference type static fields inline
  * [CA1815](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1815): Override equals and operator equals on value types
  * [CA1819](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1819): Properties should not return arrays
  * [CA1820](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1820): Test for empty strings using string length
  * [CA1822](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1822): Mark members as static
  * [CA1825](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1825): Avoid zero-length array allocations
  * [CA2213](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2213): Disposable fields should be disposed (except for `IndexWriter` and subclasses which need more work)
  * [IDE0016](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0016): use throw expression ([#368](https://github.com/apache/lucenenet/pull/368))
  * [IDE0018](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0018): Inline variable declaration
  * [IDE0019](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0019): Use pattern matching to avoid 'is' check followed by a cast
  * [IDE0020](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0020-ide0038): Use pattern matching to avoid 'is' check followed by a cast
  * [IDE0021](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0021): Use block body for constructors
  * [IDE0025](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0025): Use expression body for properties
  * [IDE0027](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0027): Use expression body for accessors
  * [IDE0028](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0028): Use collection initializers
  * [IDE0029](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0029-ide0030): Use coalesce expression
  * [IDE0030](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0029-ide0030): Use coalesce expression (nullable)
  * [IDE0031](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0031): Use null propagation
  * [IDE0034](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0034): Simplify 'default' expression
  * [IDE0038](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0020-ide0038): Use pattern matching to avoid 'is' check followed by a cast
  * [IDE0039](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0039): Use local function
  * [IDE0040](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0040): Add accessibility modifiers
  * [IDE0041](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0041): Use is null check
  * [IDE0049](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0049): Use language keywords instead of framework type names for type references
  * [IDE0051](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0051): Remove unused private member
  * [IDE0052](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0052): Remove unread private member
  * [IDE0059](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0059): Remove unnecessary value assignment
  * [IDE0060](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0060): Remove unused parameter
  * [IDE0063](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0063): Use simple 'using' statement
  * [IDE0071](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0071): Simplify interpolation
  * [IDE1005](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide1005): Use conditional delegate call
  * [IDE1006](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/naming-rules): Naming Styles
* [#387](https://github.com/apache/lucenenet/pull/387) - Removed dead code/commented code
* [#387](https://github.com/apache/lucenenet/pull/387) - **PERFORMANCE**: Added aggressive inlining in Codecs and Util namespaces
* [#387](https://github.com/apache/lucenenet/pull/387) - Simplified reuse logic of `TermsEnum` subclasses
* [#387](https://github.com/apache/lucenenet/pull/387) - **PERFORMANCE**: `Lucene.Net.Index.DocValuesProducer`: Optimized checks in `AddXXXField()` methods
* [#387](https://github.com/apache/lucenenet/pull/387) - **PERFORMANCE**: `Lucene.Net.Index`: Changed `FieldInfos`, `FreqProxTermsWriterPerField`, `IndexWriter`, `LogMergePolicy`, `SegmentCoreReaders`, and `SegmentReader` to take advantage of the fact that `TryGetValue()` returns a boolean
* [#370](https://github.com/apache/lucenenet/pull/370), [#389](https://github.com/apache/lucenenet/pull/389) - Reverted `FieldCacheImpl` delegate capture introduced in [#348](https://github.com/apache/lucenenet/pull/348)
* [#390](https://github.com/apache/lucenenet/pull/390) - Added tests for .NET 5
* [#390](https://github.com/apache/lucenenet/pull/390) - Upgraded to C# LangVersion 9.0

### New Features
* [#358](https://github.com/apache/lucenenet/pull/358) - Added Community Links page to website
* [#359](https://github.com/apache/lucenenet/pull/359) - Added builds mailing list to website
* [#365](https://github.com/apache/lucenenet/pull/365) - Added "Fork me on GitHub" to website and API docs
* `Lucene.Net.TestFramework`: Added `Assert.DoesNotThrow()` overloads