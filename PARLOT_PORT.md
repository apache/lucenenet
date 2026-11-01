# Replace ANTLR with Parlot in Lucene.Net.Expressions

This branch replaces the ANTLR 4 parser of `Lucene.Net.Expressions` (`JavascriptCompiler`) with a parser
generated at build time by [Parlot](https://github.com/sebastienros/parlot)'s source generator.

## What changes for users

- **One dependency less.** `Lucene.Net.Expressions` no longer depends on `Antlr4.Runtime.Standard`, and it does not
  depend on Parlot either: `Parlot.SourceGenerator` is a build-only analyzer (`PrivateAssets="all"`) that emits the
  parser into the assembly. The only package added to the nuspec is `System.Memory` 4.6.3 on `netstandard2.0` and
  `net462`, which `Lucene.Net` already depends on.
- **No Java to build.** `Antlr4BuildTasks` ran the ANTLR tool, a Java program, on every build.
- **For now, a preview package to build.** The grammar uses `Operators()`, which is only in the Parlot previews
  (`Parlot.SourceGenerator` 2.0.0-preview-1043, from the Parlot preview feed added to `NuGet.config`). The PR should
  move to the next Parlot release before it merges; consumers of the Lucene.NET package are not affected either way.
- **No public ANTLR types.** The ANTLR 4 migration (#996, not released yet) made the generated `JavascriptLexer`,
  `JavascriptParser`, `JavascriptBaseListener` and `IJavascriptListener` public, exposing `Antlr4.Runtime` types. They
  are gone. The ANTLR 3 parser in the released 4.8.0-beta00017 had them internal.
- **The same compiled expressions.** The new parser builds a small syntax tree whose shape mirrors the grammar rules
  (an operator chain such as `a + b - c` is one node, a parenthesized expression is its own node), and the IL
  emission was retargeted from the parse-tree listener to that tree without changing what it emits. A differential
  test (in the history of this branch, removed with ANTLR, and run again against the benchmark's ANTLR copy after
  the move to `Operators()`) compiled 60,000 random expressions with both parsers and asserted byte-identical
  `Evaluate` IL and identical variables, and 150,000 mutated ones were accepted or rejected alike.
- **Syntax errors as in the released versions.** `ParseException.ErrorOffset` is the offset of the first token that
  cannot be parsed, with the messages of the ANTLR 3 parser of 4.8.0-beta00017 (`unexpected end of expression.`,
  `invalid sequence of tokens near '...' at position (n).`, `unexpected character '...' at position (n).`), which
  are also Java Lucene's. The ANTLR 4 port reported the start of the enclosing grammar rule instead (often 0, for
  example for `100 100`) with ANTLR's own messages such as `Exception of type 'Antlr4.Runtime.NoViableAltException'
  was thrown.`. The offset is absolute, where the ANTLR 3 parser used the column within the line.
- **Unknown characters are rejected.** The ANTLR 4 lexer dropped the characters it had no token for, with a warning
  on the console, so that `$a`, `a=b`, `a+b#` or `a<=b=` compiled. They are syntax errors again, as in the
  ANTLR 3 parser and Java Lucene. Form feed and vertical tab are white space.

## Design

| File | Role |
|---|---|
| `JS/JavascriptExpressionParser.parlot.cs` | The grammar, a port of `Javascript.g4`: one `Operators()` precedence table for the unary and the ten binary levels. Build-only: read by the generator, not compiled. |
| `JS/JavascriptExpressionParser.cs` | The generated `TryParse` declaration, the lexer rules (`NAMESPACE_ID` and the longest of `HEX`, `OCTAL`, `DECIMAL`, as `Terms.Scan` scanners), and error reporting. |
| `JS/JavascriptAst.cs` | The syntax tree. The binary operator factories extend a chain of the same level unless it is in parentheses, which keeps the shape of the ANTLR parse tree. |
| `JS/JavascriptCompiler.cs` | `JavascriptNodeCompiler` replaces `JavascriptListener`, emitting the same IL per node. |

**Why the source generator rather than the `Parlot` runtime package:** Lucene.NET is a port with few dependencies, and
the expressions module is compiled once per expression rather than parsed on a hot path, so a new runtime
dependency would be hard to justify. The generator leaves no runtime dependency at all. It requires a Roslyn 5
compiler (.NET 10 SDK), which the repository already needs for `LangVersion` 14. The grammar file is 118 lines, the
scanners, tree factories and error reporting 283, and the syntax tree 189, against the 249-line ANTLR grammar plus
its build tooling and about 2,200 lines of generated C#.

## Benchmarks

`src/Lucene.Net.Expressions.Benchmarks` (not in `Lucene.Net.sln`) compares the new parser with the ANTLR 4
compiler it replaces, kept as a copy in its `Antlr` folder (the grammar and the former `JavascriptCompiler`):

```bash
dotnet run -c Release --project src/Lucene.Net.Expressions.Benchmarks/Lucene.Net.Expressions.Benchmarks.csproj -- --filter "*"
```

The expressions (`Expressions.cs`): *Small* `_score * 2` (10 chars), *Medium* a 4-variable ranking formula with
`ln` and `haversin` (79 chars), *Large* three weighted copies of a 7-term formula with conditionals, comparisons,
logical, bitwise and shift operators, hex and octal literals and 10 function calls (1,113 chars).

Two machines, BenchmarkDotNet v0.15.8, `MinIterationCount=40`:

- **Arm64:** macOS Sequoia 15.8.2, Apple M4 Pro, .NET 10.0.11 Arm64 RyuJIT.
- **x64:** Windows 11 25H2 (Hyper-V), AMD EPYC 7763 2.44 GHz, 8 cores, .NET 10.0.12 X64 RyuJIT x86-64-v3.

Two Parlot versions: **2.1.0**, the release, with the operator levels written as nested `And(ZeroOrMany(...))`
sequences, and **preview**, 2.0.0-preview-1039/1043 (same generated code) with `Operators()`, the version on this
branch. The x64 preview column is the final commit (`LaunchCount=2`); the Arm64 one is the first `Operators()`
commit, which also had a lookahead after `&` and `|`; only *Large* has those operators. A later Arm64
run of the final commit was taken on a loaded machine (load average 21) and is not shown.

### Parsing only

The ANTLR parse tree versus the Parlot syntax tree, without emitting IL. Mean time and allocations per parse:

| Size   | ANTLR, Arm64 | Parlot 2.1.0, Arm64 | Parlot preview, Arm64 | ANTLR, x64 | Parlot 2.1.0, x64 | Parlot preview, x64 |
|--------|-------------:|--------------------:|----------------------:|-----------:|------------------:|--------------------:|
| Small  | 6,498 ns, 23,928 B | 227 ns, 544 B | 106 ns, 400 B | 13,961 ns, 23,928 B | 522 ns, 544 B | 218 ns, 400 B |
| Medium | 31,388 ns, 106,144 B | 1,334 ns, 2,272 B | 618 ns, 1,816 B | 67,602 ns, 106,144 B | 3,102 ns, 2,272 B | 1,301 ns, 1,816 B |
| Large  | 547,052 ns, 1,602,512 B | 22,038 ns, 29,520 B | 9,292 ns, 24,256 B | 1,087,578 ns, 1,602,512 B | 52,018 ns, 29,520 B | 20,317 ns, 24,256 B |

Parlot 2.1.0 parses 21 to 29 times faster than ANTLR, and the preview's `Operators()` 2.1 to 2.6 times faster
again: 51 to 61 times faster than ANTLR on Arm64, 52 to 63 times on x64. Allocations are 98% lower than ANTLR's (the syntax tree and the names), and 18% to 26%
lower than with 2.1.0, which built a list per operator level.

The full table for the preview on Arm64:

| Method | Size   | Mean         | Error       | StdDev      | Median       | Ratio | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------- |------- |-------------:|------------:|------------:|-------------:|------:|---------:|--------:|----------:|------------:|
| Antlr  | Small  |   6,497.6 ns |    36.28 ns |    64.48 ns |   6,495.6 ns |  1.00 |   2.8534 |  0.0839 |   23928 B |        1.00 |
| Parlot | Small  |     106.3 ns |     0.75 ns |     1.29 ns |     106.5 ns |  0.02 |   0.0478 |       - |     400 B |        0.02 |
| Antlr  | Medium |  31,387.6 ns |    98.59 ns |   167.42 ns |  31,380.1 ns |  1.00 |  12.6343 |  1.0986 |  106144 B |        1.00 |
| Parlot | Medium |     618.1 ns |     3.35 ns |     5.60 ns |     617.3 ns |  0.02 |   0.2165 |       - |    1816 B |        0.02 |
| Antlr  | Large  | 547,052.0 ns | 2,647.30 ns | 4,636.53 ns | 547,767.5 ns |  1.00 | 191.4063 | 83.9844 | 1602512 B |        1.00 |
| Parlot | Large  |   9,291.6 ns |    67.16 ns |   119.39 ns |   9,232.3 ns |  0.02 |   2.8992 |  0.0916 |   24256 B |        0.02 |

### `JavascriptCompiler.Compile`, end to end

Parsing, IL emission and the creation of a collectible dynamic assembly and type. Arm64, preview,
`LaunchCount=4`:

| Method | Size   | Mean     | Error   | StdDev   | Median   | Ratio | RatioSD | Gen0     | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|------- |------- |---------:|--------:|---------:|---------:|------:|--------:|---------:|--------:|-------:|-----------:|------------:|
| Antlr  | Small  | 173.3 μs | 2.25 μs | 13.40 μs | 171.9 μs |  1.01 |    0.11 |   6.3477 |  5.6152 | 5.6152 |   31.25 KB |        1.00 |
| Parlot | Small  | 214.6 μs | 6.03 μs | 36.36 μs | 211.0 μs |  1.25 |    0.23 |   1.4648 |  0.9766 | 0.9766 |    7.44 KB |        0.24 |
| Antlr  | Medium | 199.9 μs | 2.48 μs | 14.81 μs | 199.1 μs |  1.01 |    0.10 |  15.6250 |  5.3711 | 5.3711 |  115.24 KB |        1.00 |
| Parlot | Medium | 203.2 μs | 4.55 μs | 27.46 μs | 201.9 μs |  1.02 |    0.16 |   3.4180 |  3.4180 | 3.4180 |   10.29 KB |        0.09 |
| Antlr  | Large  | 845.3 μs | 4.82 μs | 18.14 μs | 848.2 μs |  1.00 |    0.03 | 199.2188 | 95.7031 | 3.9063 | 1631.97 KB |        1.00 |
| Parlot | Large  | 255.2 μs | 2.82 μs | 17.02 μs | 251.3 μs |  0.30 |    0.02 |   6.3477 |  2.4414 | 2.4414 |    47.2 KB |        0.03 |

x64, Parlot 2.1.0 (the compile and search benchmarks were not rerun with the preview there; on Arm64 the preview
changed them by less than their noise, as the parse is a small part of them):

| Method | Size   | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0     | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|------- |------- |-----------:|---------:|---------:|-----------:|------:|--------:|---------:|--------:|-------:|-----------:|------------:|
| Antlr  | Small  |   644.2 μs | 12.86 μs | 23.19 μs |   649.7 μs |  1.00 |    0.05 |   3.9063 |  3.9063 | 3.9063 |   30.56 KB |        1.00 |
| Parlot | Small  |   702.8 μs | 14.00 μs | 25.24 μs |   699.8 μs |  1.09 |    0.06 |   1.9531 |  1.9531 | 1.9531 |    6.84 KB |        0.22 |
| Antlr  | Medium |   922.6 μs | 24.19 μs | 68.24 μs |   917.4 μs |  1.01 |    0.10 |   9.7656 |  3.9063 | 3.9063 |   114.4 KB |        1.00 |
| Parlot | Medium |   722.1 μs | 16.43 μs | 46.60 μs |   715.4 μs |  0.79 |    0.08 |   3.9063 |  3.9063 | 3.9063 |    9.97 KB |        0.09 |
| Antlr  | Large  | 5,394.4 μs | 43.16 μs | 74.46 μs | 5,371.6 μs |  1.00 |    0.02 | 101.5625 | 31.2500 | 3.9063 | 1633.34 KB |        1.00 |
| Parlot | Large  | 1,022.2 μs | 20.42 μs | 54.86 μs | 1,046.2 μs |  0.19 |    0.01 |   3.9063 |  3.9063 | 3.9063 |    52.2 KB |        0.03 |

Creating the dynamic assembly and type costs about 170 μs on Arm64 and 630 μs on x64 whatever the expression, so
the parser shows on larger expressions: *Large* compiles 3.3 times faster on Arm64 and 5.3 times on x64, *Medium*
the same on Arm64 and 21% faster on x64. Allocations drop by 76% to 97%.

*Small* is 9% (x64) to 25% (Arm64) slower with Parlot, which is not the parser: it parses 6 μs (Arm64) faster and
emits the same IL. Every compilation creates a collectible assembly that only a garbage collection reclaims, and
with fewer allocations there are fewer gen0 collections, so every collection Parlot's loop gets is a gen2 one
(Gen0 = Gen2 above). Adding the allocations of the ANTLR parser back (100 arrays of 200 bytes per call) made the
Parlot 2.1.0 version 3% faster than ANTLR on Arm64 (170.7 μs median against 177.3 μs, `LaunchCount=2`). This is an
artifact of compiling thousands of expressions in a loop; an application compiling an expression now and then
doesn't see it.

### The layer above: searching sorted by an expression

A `TermQuery` matching every document of an in-memory index, sorted by an expression of six doc-values fields and
`_score`, top 10. `Precompiled` sorts with an expression compiled once, as an application caching its expressions
does: the parser plays no part. `CompileAntlr` and `CompileParlot` compile the expression for every search, as an
application taking it from each request would. Arm64, preview:

| Method        | Documents | Size   | Mean         | Error     | StdDev     | Ratio | RatioSD | Gen0     | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|-------------- |---------- |------- |-------------:|----------:|-----------:|------:|--------:|---------:|--------:|-------:|-----------:|------------:|
| Precompiled   | 1000      | Medium |     78.23 μs |  0.142 μs |   0.240 μs |  0.17 |    0.00 |   1.3428 |       - |      - |   11.73 KB |        0.09 |
| CompileAntlr  | 1000      | Medium |    449.40 μs |  5.581 μs |   9.477 μs |  1.00 |    0.03 |  18.5547 |  4.8828 | 4.8828 |  127.11 KB |        1.00 |
| CompileParlot | 1000      | Medium |    452.28 μs |  8.948 μs |  18.678 μs |  1.01 |    0.05 |   2.9297 |  1.4648 | 1.4648 |   21.46 KB |        0.17 |
| Precompiled   | 1000      | Large  |    453.42 μs |  2.783 μs |   4.874 μs |  0.20 |    0.01 |   1.4648 |       - |      - |   12.01 KB |       0.007 |
| CompileAntlr  | 1000      | Large  |  2,285.77 μs | 38.989 μs |  69.303 μs |  1.00 |    0.04 | 203.1250 | 74.2188 | 3.9063 | 1646.39 KB |       1.000 |
| CompileParlot | 1000      | Large  |  1,602.14 μs | 24.555 μs |  41.697 μs |  0.70 |    0.03 |  11.7188 |  7.8125 | 5.8594 |   59.51 KB |       0.036 |
| Precompiled   | 100000    | Medium |  7,444.47 μs |  6.747 μs |  11.993 μs |  0.93 |    0.00 |        - |       - |      - |   11.77 KB |        0.09 |
| CompileAntlr  | 100000    | Medium |  7,970.31 μs | 20.045 μs |  34.576 μs |  1.00 |    0.01 |        - |       - |      - |  125.86 KB |        1.00 |
| CompileParlot | 100000    | Medium |  7,841.53 μs | 22.005 μs |  38.539 μs |  0.98 |    0.01 |        - |       - |      - |   21.08 KB |        0.17 |
| Precompiled   | 100000    | Large  | 44,932.14 μs | 71.401 μs | 117.314 μs |  0.95 |    0.00 |        - |       - |      - |   12.05 KB |       0.007 |
| CompileAntlr  | 100000    | Large  | 47,352.18 μs | 58.404 μs |  99.174 μs |  1.00 |    0.00 | 181.8182 | 90.9091 |      - | 1645.07 KB |       1.000 |
| CompileParlot | 100000    | Large  | 46,925.25 μs | 75.243 μs | 125.713 μs |  0.99 |    0.00 |        - |       - |      - |   59.27 KB |       0.036 |

x64, Parlot 2.1.0:

| Method        | Documents | Size   | Mean         | Error     | StdDev      | Ratio | RatioSD | Gen0    | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|-------------- |---------- |------- |-------------:|----------:|------------:|------:|--------:|--------:|--------:|-------:|-----------:|------------:|
| Precompiled   | 1000      | Medium |     163.1 μs |   0.64 μs |     1.12 μs |  0.09 |    0.00 |  0.4883 |       - |      - |   11.73 KB |        0.09 |
| CompileAntlr  | 1000      | Medium |   1,809.4 μs |  14.70 μs |    25.75 μs |  1.00 |    0.02 |  7.8125 |  3.9063 | 3.9063 |  125.96 KB |        1.00 |
| CompileParlot | 1000      | Medium |   1,450.6 μs |  28.36 μs |    52.56 μs |  0.80 |    0.03 |  5.8594 |  5.8594 | 5.8594 |   21.87 KB |        0.17 |
| Precompiled   | 1000      | Large  |   1,083.5 μs |   2.88 μs |     5.05 μs |  0.12 |    0.00 |       - |       - |      - |   12.01 KB |       0.007 |
| CompileAntlr  | 1000      | Large  |   9,028.2 μs |  26.24 μs |    45.27 μs |  1.00 |    0.01 | 93.7500 | 62.5000 |      - | 1645.62 KB |       1.000 |
| CompileParlot | 1000      | Large  |   4,565.3 μs |  18.39 μs |    31.73 μs |  0.51 |    0.00 |       - |       - |      - |   64.43 KB |       0.039 |
| Precompiled   | 100000    | Medium |  15,851.9 μs |  54.68 μs |    95.77 μs |  0.90 |    0.01 |       - |       - |      - |   11.77 KB |        0.09 |
| CompileAntlr  | 100000    | Medium |  17,566.0 μs |  54.52 μs |    94.04 μs |  1.00 |    0.01 |       - |       - |      - |   126.1 KB |        1.00 |
| CompileParlot | 100000    | Medium |  17,039.8 μs |  48.47 μs |    79.63 μs |  0.97 |    0.01 |       - |       - |      - |  21.48 KB |        0.17 |
| Precompiled   | 100000    | Large  | 109,653.7 μs | 502.41 μs |   866.64 μs |  0.90 |    0.01 |       - |       - |      - |   12.05 KB |       0.007 |
| CompileAntlr  | 100000    | Large  | 121,845.6 μs | 649.45 μs | 1,120.28 μs |  1.00 |    0.01 |       - |       - |      - |  1643.9 KB |       1.000 |
| CompileParlot | 100000    | Large  | 120,896.2 μs | 435.11 μs |   738.85 μs |  0.99 |    0.01 |       - |       - |      - |   64.41 KB |       0.039 |

The honest summary: the effective gain depends on how often expressions are compiled. With a cached expression
there is none, as the parser is not involved. Compiling for each search on a small index or result set makes the
search 30% (Arm64) to 49% (x64) faster for the large expression, and for the medium one from no change (Arm64) to 20% faster (x64). On 100,000
documents, evaluating the expression per document dominates and the gain is 1% to 3%. In every case the
allocations per compiled search drop from 126 KB to 21 KB (medium) and from 1.6 MB to 59 KB (large), which
matters more for a server than the time.

## Tests

`dotnet test src/Lucene.Net.Tests.Expressions` passes on net10.0 and net8.0 (macOS and Windows), and on net48 and
net472 on Windows (`-p:TestFrameworks=true -p:IncludeNetFrameworkTargets=true`), which run the `net462` and the
`netstandard2.0` builds: 105 passed, 5 skipped (the existing ignored recursion tests) on each. New:
`TestJavascriptCompiler.TestParseErrorOffsets`.

## Known issues

- **Existing bug, unchanged:** a conditional whose true branch emits more than 127 bytes of IL fails with
  `NotSupportedException: Illegal one-byte branch`, because `CompileConditional` emits `Br_S`. Both parsers emit the
  same IL, so both fail; `OpCodes.Br` would fix it in a separate change.
- **net462 build workaround:** the Parlot generator refuses .NET Framework below 4.7.2 (`PARLOT026`), and its
  package adds a `netstandard` reference that only the 4.7.1+ reference assemblies have. The code it generates for
  .NET Standard 2.0 only needs `System.Memory` and `System.ValueTuple`, which net462 references, so three targets in
  `Lucene.Net.Expressions.csproj` report 4.7.2 to the generator and supply the facade. This should move into Parlot.
- **Preview dependency:** see above. Building needs the Parlot preview feed until the next Parlot release.

## Parlot gaps found

1. **CS1574 in consumers' builds (fixed).** In 2.1.0, `Parlot.StandaloneRuntime.Fluent.ParseContext.g.cs` has crefs
   such as `<see cref="WithWhiteSpaceSet{T}(Parser{T}, WhiteSpaceSet)"/>` that don't resolve in the embedded runtime,
   so every project with `GenerateDocumentationFile` gets warnings, and one with `TreatWarningsAsErrors` fails.
   [sebastienros/parlot#469](https://github.com/sebastienros/parlot/pull/469) strips the embedded runtime's doc
   comments; 2.0.0-preview-1043 builds this project without a warning.
2. **No .NET Framework 4.6.x.** `PARLOT026` refuses targets below 4.7.2, and `Parlot.SourceGenerator.targets` adds
   `<Reference Include="netstandard" />`, which the 4.6.x reference assemblies lack. `System.Memory` supports 4.6.1+,
   and the generated code builds for net462 once the version check passes and the facade from
   `Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll` is a compile reference. Lucene.NET (and other libraries
   still targeting net461/net462) need either a lower floor or an opt-out property.
3. **No error position from a generated entry point.** `TryParse` returns `bool` only, so reporting where a syntax
   error is takes a mutable configuration object and `Then((context, start, end, value) => ...)` callbacks on the
   tokens and at the start of each operand to record the furthest position. The diagnostics overload of the
   previews only reports `Error`/`ElseError` failures, which would need an `ElseError` at every place an operand or
   a token is expected. A built-in furthest-failure offset (an `out int` or a result type on the entry point) would
   remove eight callbacks from this grammar.
4. **Repetitive token callbacks.** Because a callback may not capture another method's parameters (`PARLOT015`),
   those callbacks can't come from one helper method taking the configuration object, and are written inline.
5. **`Eof()` doesn't skip trailing white space**, as documented, so `conditional.Eof()` rejected `" 1 "`; the grammar
   uses `AndSkip(SkipWhiteSpace(Eof()))`. Easy to miss with `Terms` parsers everywhere else.
6. **Preview versions sort below the release.** `main` still has `VersionPrefix` 2.0.0, so its Feedz previews are
   `2.0.0-preview-N`, older than 2.1.0 for NuGet: an update check never offers them, and they have to be pinned.

What worked well: `Operators()` replaced the ten nested operator levels with one table and made parsing 2.1 to 2.6
times faster again, `Terms.Scan` ported the ANTLR lexer rules (longest match of `HEX`/`OCTAL`/`DECIMAL`, dotted
names) one to one, `WithWhiteSpaceSet(WhiteSpaceSet.Ascii)` matched the grammar's white space, and the existing
tests pass unchanged (only a stale ANTLR exclusion was dropped from `TestApiConsistency`).
