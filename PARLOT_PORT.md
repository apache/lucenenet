# Replace ANTLR with Parlot in Lucene.Net.Expressions

This branch replaces the ANTLR 4 parser of `Lucene.Net.Expressions` (`JavascriptCompiler`) with a parser
generated at build time by [Parlot](https://github.com/sebastienros/parlot)'s source generator.

## What changes for users

- **One dependency less.** `Lucene.Net.Expressions` no longer depends on `Antlr4.Runtime.Standard`, and it does not
  depend on Parlot either: `Parlot.SourceGenerator` is a build-only analyzer (`PrivateAssets="all"`) that emits the
  parser into the assembly. The only package added to the nuspec is `System.Memory` 4.6.3 on `netstandard2.0` and
  `net462`, which `Lucene.Net` already depends on.
- **No Java to build.** `Antlr4BuildTasks` ran the ANTLR tool, a Java program, on every build.
- **No public ANTLR types.** The ANTLR 4 migration (#996, not released yet) made the generated `JavascriptLexer`,
  `JavascriptParser`, `JavascriptBaseListener` and `IJavascriptListener` public, exposing `Antlr4.Runtime` types. They
  are gone. The ANTLR 3 parser in the released 4.8.0-beta00017 had them internal.
- **The same compiled expressions.** The new parser builds a small syntax tree whose shape mirrors the grammar rules
  (an operator chain such as `a + b - c` is one node, a parenthesized expression is its own node), and the IL
  emission was retargeted from the parse-tree listener to that tree without changing what it emits. A differential
  test (in the history of this branch, removed with ANTLR) compiled thousands of random expressions with both
  parsers and asserted byte-identical `Evaluate` IL and identical variables.
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
| `JS/JavascriptExpressionParser.parlot.cs` | The grammar, a port of `Javascript.g4`. Build-only: read by the generator, not compiled. |
| `JS/JavascriptExpressionParser.cs` | The generated `TryParse` declaration, the lexer rules (`NAMESPACE_ID` and the longest of `HEX`, `OCTAL`, `DECIMAL`, as `Terms.Scan` scanners), and error reporting. |
| `JS/JavascriptAst.cs` | The syntax tree. |
| `JS/JavascriptCompiler.cs` | `JavascriptNodeCompiler` replaces `JavascriptListener`, emitting the same IL per node. |

**Why the source generator rather than the `Parlot` runtime package:** Lucene.NET is a port with few dependencies, and
the expressions module is compiled once per expression rather than parsed on a hot path, so a new runtime
dependency would be hard to justify. The generator leaves no runtime dependency at all. It requires a Roslyn 5
compiler (.NET 10 SDK), which the repository already needs for `LangVersion` 14. The grammar file is 161 lines, the
scanners and error reporting 271, and the syntax tree 186, against the 249-line ANTLR grammar plus its build
tooling and about 2,200 lines of generated C#.

## Benchmarks

`src/Lucene.Net.Expressions.Benchmarks` (not in `Lucene.Net.sln`) compares the new parser with the ANTLR 4
compiler it replaces, kept as a copy in its `Antlr` folder (the grammar and the former `JavascriptCompiler`):

```bash
dotnet run -c Release --project src/Lucene.Net.Expressions.Benchmarks/Lucene.Net.Expressions.Benchmarks.csproj -- --filter "*"
```

The expressions (`Expressions.cs`): *Small* `_score * 2` (10 chars), *Medium* a 4-variable ranking formula with
`ln` and `haversin` (79 chars), *Large* three weighted copies of a 7-term formula with conditionals, comparisons,
logical, bitwise and shift operators, hex and octal literals and 10 function calls (1,113 chars).

BenchmarkDotNet v0.15.8, macOS Sequoia 15.8.2, Apple M4 Pro, .NET 10.0.11 Arm64 RyuJIT, `MinIterationCount=40`.

### Parsing only

The ANTLR parse tree versus the Parlot syntax tree, without emitting IL.

| Method | Size   | Mean         | Error       | StdDev      | Ratio | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------- |------- |-------------:|------------:|------------:|------:|---------:|--------:|----------:|------------:|
| Antlr  | Small  |   6,594.1 ns |     8.62 ns |    15.10 ns |  1.00 |   2.8534 |  0.0839 |   23928 B |        1.00 |
| Parlot | Small  |     226.9 ns |     0.29 ns |     0.50 ns |  0.03 |   0.0648 |       - |     544 B |        0.02 |
| Antlr  | Medium |  33,690.9 ns |    44.76 ns |    77.20 ns |  1.00 |  12.6343 |  1.0986 |  106144 B |        1.00 |
| Parlot | Medium |   1,334.2 ns |     2.50 ns |     4.45 ns |  0.04 |   0.2708 |       - |    2272 B |        0.02 |
| Antlr  | Large  | 552,291.4 ns | 1,276.22 ns | 2,268.48 ns |  1.00 | 191.4063 | 83.9844 | 1602512 B |        1.00 |
| Parlot | Large  |  22,038.3 ns |    18.60 ns |    30.03 ns |  0.04 |   3.5095 |  0.1526 |   29520 B |        0.02 |

Parsing is 25 to 29 times faster and allocates 98% less.

### `JavascriptCompiler.Compile`, end to end

Parsing, IL emission and the creation of a collectible dynamic assembly and type.

| Method | Size   | Mean     | Error   | StdDev   | Median   | Ratio | RatioSD | Gen0     | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|------- |------- |---------:|--------:|---------:|---------:|------:|--------:|---------:|--------:|-------:|-----------:|------------:|
| Antlr  | Small  | 179.5 μs | 1.85 μs | 10.99 μs | 177.8 μs |  1.00 |    0.09 |   5.3711 |  2.9297 | 2.9297 |    31.3 KB |        1.00 |
| Parlot | Small  | 221.2 μs | 6.02 μs | 36.33 μs | 217.0 μs |  1.24 |    0.22 |   5.1270 |  5.1270 | 5.1270 |    8.14 KB |        0.26 |
| Antlr  | Medium | 206.4 μs | 2.40 μs | 14.42 μs | 204.3 μs |  1.00 |    0.10 |  15.6250 |  4.3945 | 4.3945 |   115.4 KB |        1.00 |
| Parlot | Medium | 205.6 μs | 4.83 μs | 29.12 μs | 200.8 μs |  1.00 |    0.16 |   4.8828 |  4.8828 | 4.8828 |   10.63 KB |        0.09 |
| Antlr  | Large  | 840.9 μs | 3.01 μs | 11.24 μs | 839.7 μs |  1.00 |    0.02 | 199.2188 | 89.8438 | 3.9063 | 1632.86 KB |        1.00 |
| Parlot | Large  | 263.2 μs | 2.45 μs |  9.28 μs | 261.4 μs |  0.31 |    0.01 |   6.8359 |  2.9297 | 2.9297 |   52.41 KB |        0.03 |

`LaunchCount=4`, `MinIterationCount=40`.

Creating the dynamic assembly and type costs about 180 μs whatever the expression, so the parser shows on large
expressions: 3.2 times faster for *Large*, the same for *Medium*. Allocations drop by 74% to 97%.

*Small* is 20% slower with Parlot, which is not the parser: it parses 6 μs faster and emits the same IL. Every
compilation creates a collectible assembly that only a garbage collection reclaims, and with fewer allocations
there are fewer gen0 collections, so every collection Parlot's loop gets is a gen2 one (Gen0 = Gen2 above). Adding
the allocations of the ANTLR parser back (100 arrays of 200 bytes per call) makes the Parlot version 3% faster
than ANTLR (170.7 μs median against 177.3 μs, `LaunchCount=2`). This is an artifact of compiling thousands of
expressions in a loop; an application compiling an expression now and then doesn't see it.

### The layer above: searching sorted by an expression

A `TermQuery` matching every document of an in-memory index, sorted by an expression of six doc-values fields and
`_score`, top 10. `Precompiled` sorts with an expression compiled once, as an application caching its expressions
does: the parser plays no part. `CompileAntlr` and `CompileParlot` compile the expression for every search, as an
application taking it from each request would.

| Method        | Documents | Size   | Mean         | Error     | StdDev     | Ratio | RatioSD | Gen0     | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|-------------- |---------- |------- |-------------:|----------:|-----------:|------:|--------:|---------:|--------:|-------:|-----------:|------------:|
| Precompiled   | 1000      | Medium |     79.79 μs |  0.060 μs |   0.101 μs |  0.17 |    0.00 |   1.3428 |       - |      - |   11.73 KB |        0.09 |
| CompileAntlr  | 1000      | Medium |    458.88 μs |  3.186 μs |   5.662 μs |  1.00 |    0.02 |  19.5313 |  5.8594 | 5.8594 |  127.27 KB |        1.00 |
| CompileParlot | 1000      | Medium |    449.40 μs |  8.532 μs |  15.165 μs |  0.98 |    0.03 |   4.3945 |  3.4180 | 3.4180 |   22.01 KB |        0.17 |
| Precompiled   | 1000      | Large  |    448.64 μs |  0.197 μs |   0.335 μs |  0.20 |    0.00 |   1.4648 |       - |      - |   12.01 KB |       0.007 |
| CompileAntlr  | 1000      | Large  |  2,290.49 μs |  5.120 μs |   8.966 μs |  1.00 |    0.01 | 203.1250 | 74.2188 | 3.9063 | 1646.31 KB |       1.000 |
| CompileParlot | 1000      | Large  |  1,615.19 μs |  3.696 μs |   6.473 μs |  0.71 |    0.00 |   7.8125 |  3.9063 | 3.9063 |   64.64 KB |       0.039 |
| Precompiled   | 100000    | Medium |  7,472.72 μs |  8.614 μs |  15.086 μs |  0.92 |    0.00 |        - |       - |      - |   11.77 KB |        0.09 |
| CompileAntlr  | 100000    | Medium |  8,085.30 μs | 17.512 μs |  31.127 μs |  1.00 |    0.01 |        - |       - |      - |  125.86 KB |        1.00 |
| CompileParlot | 100000    | Medium |  7,966.82 μs | 12.907 μs |  22.264 μs |  0.99 |    0.00 |        - |       - |      - |   21.53 KB |        0.17 |
| Precompiled   | 100000    | Large  | 44,705.44 μs | 45.600 μs |  74.922 μs |  0.94 |    0.00 |        - |       - |      - |   12.05 KB |       0.007 |
| CompileAntlr  | 100000    | Large  | 47,698.66 μs | 61.108 μs | 107.026 μs |  1.00 |    0.00 | 181.8182 | 90.9091 |      - | 1646.66 KB |       1.000 |
| CompileParlot | 100000    | Large  | 46,487.60 μs | 66.602 μs | 114.885 μs |  0.97 |    0.00 |        - |       - |      - |   64.41 KB |       0.039 |

The honest summary: the effective gain depends on how often expressions are compiled. With a cached expression
there is none, as the parser is not involved. Compiling for each search on a small index or result set makes it
29% faster for the large expression, 2% for the medium one. On 100,000 documents, evaluating the expression per
document dominates and the gain is 1% to 3%. In every case the allocations per compiled search drop from 126 KB to
22 KB (medium) and from 1.6 MB to 64 KB (large), which matters more for a server than the time.

## Tests

`dotnet test src/Lucene.Net.Tests.Expressions` passes on net10.0 and net8.0 (`-p:TestFrameworks=true`). New:
`TestJavascriptCompiler.TestParseErrorOffsets`. The `net462` and `netstandard2.0` builds are compile-verified only
on macOS; the .NET Framework test legs (net48/net472, which consume `netstandard2.0` and `net462`) have not been run.

## Known issues

- **Existing bug, unchanged:** a conditional whose true branch emits more than 127 bytes of IL fails with
  `NotSupportedException: Illegal one-byte branch`, because `CompileConditional` emits `Br_S`. Both parsers emit the
  same IL, so both fail; `OpCodes.Br` would fix it in a separate change.
- **net462 build workaround:** the Parlot generator refuses .NET Framework below 4.7.2 (`PARLOT026`), and its
  package adds a `netstandard` reference that only the 4.7.1+ reference assemblies have. The code it generates for
  .NET Standard 2.0 only needs `System.Memory` and `System.ValueTuple`, which net462 references, so three targets in
  `Lucene.Net.Expressions.csproj` report 4.7.2 to the generator and supply the facade. This should move into Parlot.
- **Parlot warning CS1574:** the generator's embedded runtime has an XML doc `cref` that does not resolve in projects
  generating documentation, one warning per target framework. Fixed in Parlot by
  [sebastienros/parlot#469](https://github.com/sebastienros/parlot/pull/469), not released yet.

## Parlot gaps found (Parlot.SourceGenerator 2.1.0)

1. **CS1574 in consumers' builds.** `Parlot.StandaloneRuntime.Fluent.ParseContext.g.cs(224)` has
   `<see cref="WithWhiteSpaceSet{T}(Parser{T}, WhiteSpaceSet)"/>`, which does not resolve in the embedded runtime.
   Every project with `GenerateDocumentationFile` gets the warning, and one with `TreatWarningsAsErrors` fails.
2. **No .NET Framework 4.6.x.** `PARLOT026` refuses targets below 4.7.2, and `Parlot.SourceGenerator.targets` adds
   `<Reference Include="netstandard" />`, which the 4.6.x reference assemblies lack. `System.Memory` supports 4.6.1+,
   and the generated code builds for net462 once the version check passes and the facade from
   `Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll` is a compile reference. Lucene.NET (and other libraries
   still targeting net461/net462) need either a lower floor or an opt-out property.
3. **No error position from a generated entry point.** `TryParse` returns `bool` only, so reporting where a syntax
   error is takes a mutable configuration object and a `Then((context, start, end, value) => ...)` callback on every
   token to record the furthest token end. A built-in furthest-failure offset (an `out int` or a result type on the
   entry point) would remove ten callbacks from this grammar.
4. **Repetitive token callbacks.** Because a callback may not capture another method's parameters (`PARLOT015`),
   those ten tracking callbacks can't come from one helper method taking the configuration object, and are written
   inline.
5. **`Eof()` doesn't skip trailing white space**, as documented, so `conditional.Eof()` rejected `" 1 "`; the grammar
   uses `AndSkip(SkipWhiteSpace(Eof()))`. Easy to miss with `Terms` parsers everywhere else.
6. **Preview versions sort below the release.** `main` still has `VersionPrefix` 2.0.0, so its Feedz previews are
   `2.0.0-preview-N`, older than 2.1.0 for NuGet. The newest (`2.0.0-preview-1039`, commit `c6ebdd0`) has
   `Operators()`, which could replace the ten operator levels of this grammar with one precedence parser, and
   diagnostics returned by generated entry points, which could replace the error tracking callbacks (3 and 4).
   This branch stays on the 2.1.0 release.

What worked well: `Terms.Scan` ported the ANTLR lexer rules (longest match of `HEX`/`OCTAL`/`DECIMAL`, dotted
names) one to one, `WithWhiteSpaceSet(WhiteSpaceSet.Ascii)` matched the grammar's white space, and the existing
tests pass unchanged (only a stale ANTLR exclusion was dropped from `TestApiConsistency`).
