# Replace ANTLR with Parlot in Lucene.Net.Expressions

This branch replaces the ANTLR 4 parser of `Lucene.Net.Expressions` (`JavascriptCompiler`) with a parser
generated at build time by [Parlot](https://github.com/sebastienros/parlot)'s source generator.

## What changes for users

- **One dependency less.** `Lucene.Net.Expressions` no longer depends on `Antlr4.Runtime.Standard`, and it does not
  depend on Parlot either: `Parlot.SourceGenerator` is a build-only analyzer (`PrivateAssets="all"`) that emits the
  parser into the assembly. The only package added to the nuspec is `System.Memory` 4.6.3 on `netstandard2.0` and
  `net462`, which `Lucene.Net` already depends on.
- **No Java to build.** `Antlr4BuildTasks` ran the ANTLR tool, a Java program, on every build.
- **For now, a preview package to build.** The grammar uses `Operators()` with a `Ternary` level and the furthest
  failure of the generated entry point, which are only in the Parlot previews (`Parlot.SourceGenerator`
  2.0.0-preview.1083, from the Parlot preview feed added to `NuGet.config`). The PR should
  move to the next Parlot release before it merges; consumers of the Lucene.NET package are not affected either way.
- **No public ANTLR types.** The ANTLR 4 migration (#996, not released yet) made the generated `JavascriptLexer`,
  `JavascriptParser`, `JavascriptBaseListener` and `IJavascriptListener` public, exposing `Antlr4.Runtime` types. They
  are gone. The ANTLR 3 parser in the released 4.8.0-beta00017 had them internal.
- **The same compiled expressions.** The new parser builds a small syntax tree whose shape mirrors the grammar rules
  (an operator chain such as `a + b - c` is one node, a parenthesized expression is its own node), and the IL
  emission was retargeted from the parse-tree listener to that tree without changing what it emits. A differential
  test (in the history of this branch, removed with ANTLR, and run again against the benchmark's ANTLR copy after
  the move to `Operators()` and again with preview-1059, preview.1077 and preview.1083) compiled 60,000 random expressions with both parsers and asserted byte-identical
  `Evaluate` IL and identical variables, and 150,000 mutated ones were accepted or rejected alike.
- **Syntax errors as in the released versions.** `ParseException.ErrorOffset` is the offset of the first token that
  cannot be parsed, with the messages of the ANTLR 3 parser of 4.8.0-beta00017 (`unexpected end of expression.`,
  `invalid sequence of tokens near '...' at position (n).`, `unexpected character '...' at position (n).`), which
  are also Java Lucene's. The ANTLR 4 port reported the start of the enclosing grammar rule instead (often 0, for
  example for `100 100`) with ANTLR's own messages such as `Exception of type 'Antlr4.Runtime.NoViableAltException'
  was thrown.`. The offset is absolute, where the ANTLR 3 parser used the column within the line. It is the
  furthest failure the generated `TryParse` returns: preview-1059, preview.1077 and preview.1083 give the same offsets and messages
  as the earlier callbacks recording each token's position, on 104,232 rejected mutated expressions.
- **Unknown characters are rejected.** The ANTLR 4 lexer dropped the characters it had no token for, with a warning
  on the console, so that `$a`, `a=b`, `a+b#` or `a<=b=` compiled. They are syntax errors again, as in the
  ANTLR 3 parser and Java Lucene. White space is space, tab, CR and LF, as in the grammar's `WS` rule, so a form
  feed or a vertical tab is an unexpected character too.
- **Deep nesting is a syntax error, not a crash (denial of service fixed).** The parser checks the stack in its
  recursive rules and throws a `ParseException` where it would overflow. It reads chains of prefix operators and of
  conditionals in their false branch (`a ? b : c ? d : e`) in a loop, and the compiler compiles these chains in a
  loop too, so they have no limit; it also checks the stack, in case a tree were deeper than it can recurse. The ANTLR 4 parser never checks the stack,
  so a deeply nested expression overflowed it, and .NET doesn't let code catch a stack overflow: the runtime
  terminates the whole process, whatever `catch` blocks surround `JavascriptCompiler.Compile`. An application
  that compiles expressions taken from requests could be stopped by a 2 KB expression of parentheses (CWE-674,
  uncontrolled recursion), and running the parse on a thread with a larger stack only raised the limit. The
  parenthesized depth ANTLR handled was also lower:

  | Maximum depth that compiles | ANTLR 4, 1 MB stack | Parlot, 1 MB stack | ANTLR 4, 8 MB stack | Parlot, 8 MB stack |
  |---|--:|--:|--:|--:|
  | `((((1))))` | 250, then crash | 1,167 | 2,007, then crash | 10,350 |
  | `-(-(-(1)))` | not measured | 1,007 | not measured | 11,759 |
  | `----1` | 8,120, then crash | no limit (200,000 tested) | over 40,000 | no limit |

  With preview-1059, which recursed on each prefix operator, `----1` stopped at 1,823 and 16,159 operators, and
  `((1))` at 927 and 8,281. Its `Ternary` level read the false branches in a loop but the compiler recursed on them,
  so 100,000 nested `0?1:` crashed the process when compiled; the compiler loops on them since preview.1077.

## Design

| File | Role |
|---|---|
| `JS/JavascriptExpressionParser.parlot.cs` | The grammar, a port of `Javascript.g4`: one `Operators()` precedence table for the unary, the ten binary and the conditional levels. Build-only: read by the generator, not compiled. |
| `JS/JavascriptExpressionParser.cs` | The generated `TryParse` declaration, which returns the offset of the furthest failure, the lexer rules (`NAMESPACE_ID` and the longest of `HEX`, `OCTAL`, `DECIMAL`, as `Terms.Scan` scanners), and the error messages. |
| `JS/JavascriptAst.cs` | The syntax tree. The binary operator factories extend a chain of the same level unless it is in parentheses, which keeps the shape of the ANTLR parse tree. |
| `JS/JavascriptCompiler.cs` | `JavascriptNodeCompiler` replaces `JavascriptListener`, emitting the same IL per node, with chains of unary operators and of conditionals compiled in a loop. |

**Why the source generator rather than the `Parlot` runtime package:** Lucene.NET is a port with few dependencies, and
the expressions module is compiled once per expression rather than parsed on a hot path, so a new runtime
dependency would be hard to justify. The generator leaves no runtime dependency at all. It requires a Roslyn 5
compiler (.NET 10 SDK), which the repository already needs for `LangVersion` 14. The grammar file is 103 lines, the
scanners, tree factories and error reporting 281, and the syntax tree 172, against the 249-line ANTLR grammar plus
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
sequences, and **preview**, 2.0.0-preview.1083 with `Operators()`, the version on this branch. The preview numbers are
one run of everything (`LaunchCount=1`) on each machine, with a load average of 4 on the 14 cores of the Arm64 one, and
so less settled than repeated runs; the 2.1.0 columns are from earlier runs of the same machines.

### Parsing only

The ANTLR parse tree versus the Parlot syntax tree, without emitting IL. Mean time and allocations per parse:

| Size   | ANTLR, Arm64 | Parlot 2.1.0, Arm64 | Parlot preview, Arm64 | ANTLR, x64 | Parlot 2.1.0, x64 | Parlot preview, x64 |
|--------|-------------:|--------------------:|----------------------:|-----------:|------------------:|--------------------:|
| Small  | 6,375 ns, 23,928 B | 227 ns, 544 B | 88 ns, 344 B | 15,478 ns, 23,928 B | 522 ns, 544 B | 192 ns, 344 B |
| Medium | 30,652 ns, 106,144 B | 1,334 ns, 2,272 B | 591 ns, 1,760 B | 71,247 ns, 106,144 B | 3,102 ns, 2,272 B | 1,294 ns, 1,760 B |
| Large  | 523,523 ns, 1,602,512 B | 22,038 ns, 29,520 B | 8,401 ns, 23,504 B | 1,100,719 ns, 1,602,512 B | 52,018 ns, 29,520 B | 18,217 ns, 23,504 B |

Parlot 2.1.0 parses 21 to 29 times faster than ANTLR, and the preview's `Operators()` 2.3 to 2.9 times faster
again: 52 to 73 times faster than ANTLR on Arm64, 55 to 81 times on x64. Allocations are 98% lower than ANTLR's (the
syntax tree and the names), and 20% to 37% lower than with 2.1.0, which built a list per operator level.

How the previews compare on x64, with the time of each run. 1043 used position callbacks; 1059 replaced them with the
furthest failure and the conditional with a `Ternary` level, and allocates 32 bytes less per parse (the parse state
object). 1077 parses 4% to 13% faster than 1059, which keeps exception handling out of the climb of its `Ternary` level
([sebastienros/parlot#485](https://github.com/sebastienros/parlot/pull/485), the likely cause; this wasn't isolated).
1083 is the same as 1077 within the noise of its single run. Pooling the names with `ToPooledString()` on 1077 was tried
and dropped: it allocated 12% to 13% less, the strings of the names, but gave back 1% to 11% of the time, as a lookup
costs about as much as creating a short string, and compiling an expression showed neither.

| Size   | 1043 | 1059 | 1077 | 1077, pooled names | 1083 |
|--------|-----:|-----:|-----:|-------------------:|-----:|
| Small  | 211 ns, 208 ns, 400 B | 222 ns, 228 ns, 368 B | 192 ns, 199 ns, 344 B | 217 ns, 216 ns, 304 B | 192 ns, 344 B |
| Medium | 1,314 ns, 1,311 ns, 1,816 B | 1,421 ns, 1,379 ns, 1,784 B | 1,324 ns, 1,353 ns, 1,760 B | 1,350 ns, 1,363 ns, 1,536 B | 1,294 ns, 1,760 B |
| Large  | 20,509 ns, 20,633 ns, 24,256 B | 20,129 ns, 20,601 ns, 24,224 B | 18,433 ns, 18,979 ns, 23,504 B | 19,244 ns, 19,816 ns, 20,360 B | 18,217 ns, 23,504 B |

The 1043 and 1083 columns are from other sessions of the same machine than the three in the middle, so compare them
loosely.

The full tables for the preview, Arm64 and x64:

| Method | Size   | Mean          | Error        | StdDev       | Ratio | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------- |------- |--------------:|-------------:|-------------:|------:|---------:|--------:|----------:|------------:|
| Antlr  | Small  |   6,375.48 ns |    26.847 ns |    25.112 ns |  1.00 |   2.8534 |  0.0839 |   23928 B |        1.00 |
| Parlot | Small  |      87.84 ns |     0.684 ns |     0.640 ns |  0.01 |   0.0411 |       - |     344 B |        0.01 |
| Antlr  | Medium |  30,652.40 ns |   162.209 ns |   151.730 ns |  1.00 |  12.6343 |  1.0986 |  106144 B |        1.00 |
| Parlot | Medium |     591.00 ns |     5.245 ns |     4.906 ns |  0.02 |   0.2098 |       - |    1760 B |        0.02 |
| Antlr  | Large  | 523,523.18 ns | 3,269.301 ns | 3,058.106 ns |  1.00 | 191.4063 | 83.9844 | 1602512 B |        1.00 |
| Parlot | Large  |   8,401.05 ns |    51.907 ns |    48.554 ns |  0.02 |   2.8076 |  0.0916 |   23504 B |        0.01 |

| Method | Size   | Mean           | Error        | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------- |------- |---------------:|-------------:|-------------:|------:|--------:|--------:|--------:|----------:|------------:|
| Antlr  | Small  |    15,478.3 ns |    306.60 ns |    612.31 ns |  1.00 |    0.06 |  1.4191 |  0.0305 |   23928 B |        1.00 |
| Parlot | Small  |       192.1 ns |      3.82 ns |      3.76 ns |  0.01 |    0.00 |  0.0205 |       - |     344 B |        0.01 |
| Antlr  | Medium |    71,246.9 ns |  1,383.90 ns |  1,699.56 ns |  1.00 |    0.03 |  6.2256 |  0.4883 |  106144 B |        1.00 |
| Parlot | Medium |     1,293.6 ns |     22.20 ns |     19.68 ns |  0.02 |    0.00 |  0.1049 |       - |    1760 B |        0.02 |
| Antlr  | Large  | 1,100,719.2 ns | 19,817.41 ns | 17,567.62 ns |  1.00 |    0.02 | 95.7031 | 50.7813 | 1602512 B |        1.00 |
| Parlot | Large  |    18,216.5 ns |    102.46 ns |     79.99 ns |  0.02 |    0.00 |  1.4038 |  0.0305 |   23504 B |        0.01 |

### `JavascriptCompiler.Compile`, end to end

Parsing, IL emission and the creation of a collectible dynamic assembly and type. Arm64, preview:

| Method | Size   | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0     | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|------- |------- |---------:|---------:|---------:|---------:|------:|--------:|---------:|--------:|-------:|-----------:|------------:|
| Antlr  | Small  | 159.3 μs |  4.09 μs | 12.07 μs | 161.4 μs |  1.01 |    0.11 |   4.8828 |  2.4414 | 2.4414 |   31.19 KB |        1.00 |
| Parlot | Small  | 211.3 μs | 11.63 μs | 34.30 μs | 220.5 μs |  1.33 |    0.24 |   4.8828 |  4.8828 | 4.8828 |    7.55 KB |        0.24 |
| Antlr  | Medium | 179.1 μs |  4.09 μs | 12.07 μs | 180.6 μs |  1.00 |    0.09 |  15.6250 |  4.3945 | 4.3945 |  114.92 KB |        1.00 |
| Parlot | Medium | 190.0 μs |  9.10 μs | 26.82 μs | 181.4 μs |  1.07 |    0.17 |   4.6387 |  4.6387 | 4.6387 |   10.15 KB |        0.09 |
| Antlr  | Large  | 758.8 μs |  5.76 μs |  5.10 μs | 758.1 μs |  1.00 |    0.01 | 199.2188 | 93.7500 | 3.9063 | 1632.83 KB |        1.00 |
| Parlot | Large  | 237.9 μs |  5.02 μs | 14.81 μs | 231.8 μs |  0.31 |    0.02 |   7.8125 |  4.3945 | 4.3945 |   47.06 KB |        0.03 |

x64, preview:

| Method | Size   | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|------- |------- |-----------:|---------:|---------:|------:|--------:|--------:|--------:|-------:|-----------:|------------:|
| Antlr  | Small  |   643.6 μs | 12.78 μs | 12.55 μs |  1.00 |    0.03 |  3.9063 |  3.9063 | 3.9063 |   30.52 KB |        1.00 |
| Parlot | Small  |   598.0 μs | 11.92 μs | 18.56 μs |  0.93 |    0.03 |  1.9531 |  1.9531 | 1.9531 |    6.57 KB |        0.22 |
| Antlr  | Medium |   694.7 μs | 13.73 μs | 32.62 μs |  1.00 |    0.07 |  9.7656 |  3.9063 | 3.9063 |  115.43 KB |        1.00 |
| Parlot | Medium |   615.4 μs | 11.92 μs | 17.83 μs |  0.89 |    0.05 |  3.9063 |  3.9063 | 3.9063 |    9.48 KB |        0.08 |
| Antlr  | Large  | 5,189.2 μs | 69.63 μs | 58.15 μs |  1.00 |    0.02 | 93.7500 | 39.0625 |      - | 1632.26 KB |        1.00 |
| Parlot | Large  |   832.5 μs | 15.86 μs | 20.05 μs |  0.16 |    0.00 |  3.9063 |  3.9063 | 3.9063 |   46.91 KB |        0.03 |

Creating the dynamic assembly and type costs about 160 μs on Arm64 and 600 μs on x64 whatever the expression, so
the parser shows on larger expressions: *Large* compiles 3.2 times faster on Arm64 and 6.2 times on x64, and *Medium*
takes the same time on Arm64 (the medians are 181 μs for both) and is 11% faster on x64. Allocations drop by 76% to
97%.

*Small* is 33% slower on Arm64 with Parlot in this run and 7% faster on x64, which is not the parser: it parses 6 μs
(Arm64) faster and emits the same IL. Every compilation creates a collectible assembly that only a garbage collection
reclaims, and with fewer allocations there are fewer gen0 collections, so every collection Parlot's loop gets is a gen2
one (Gen0 = Gen2 above). Adding the allocations of the ANTLR parser back (100 arrays of 200 bytes per call) made the
Parlot 2.1.0 version 3% faster than ANTLR on Arm64 (170.7 μs median against 177.3 μs, `LaunchCount=2`). This is an
artifact of compiling thousands of expressions in a loop; an application compiling an expression now and then
doesn't see it.

### The layer above: searching sorted by an expression

A `TermQuery` matching every document of an in-memory index, sorted by an expression of six doc-values fields and
`_score`, top 10. `Precompiled` sorts with an expression compiled once, as an application caching its expressions
does: the parser plays no part. `CompileAntlr` and `CompileParlot` compile the expression for every search, as an
application taking it from each request would. Arm64, preview:

| Method        | Documents | Size   | Mean         | Error      | StdDev     | Ratio | RatioSD | Gen0     | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|-------------- |---------- |------- |-------------:|-----------:|-----------:|------:|--------:|---------:|--------:|-------:|-----------:|------------:|
| Precompiled   | 1000      | Medium |     76.78 μs |   0.384 μs |   0.359 μs |  0.18 |    0.00 |   1.3428 |       - |      - |   11.73 KB |        0.09 |
| CompileAntlr  | 1000      | Medium |    419.70 μs |   6.448 μs |   6.032 μs |  1.00 |    0.02 |  17.5781 |  4.8828 | 4.8828 |  127.18 KB |        1.00 |
| CompileParlot | 1000      | Medium |    414.86 μs |   8.199 μs |  13.472 μs |  0.99 |    0.03 |   2.9297 |  1.4648 | 1.4648 |   21.46 KB |        0.17 |
| Precompiled   | 1000      | Large  |    440.80 μs |   1.312 μs |   1.227 μs |  0.21 |    0.00 |   1.4648 |       - |      - |   12.01 KB |       0.007 |
| CompileAntlr  | 1000      | Large  |  2,087.88 μs |  16.169 μs |  15.125 μs |  1.00 |    0.01 | 195.3125 | 70.3125 |      - | 1644.99 KB |       1.000 |
| CompileParlot | 1000      | Large  |  1,504.97 μs |  10.788 μs |   9.563 μs |  0.72 |    0.01 |   7.8125 |  3.9063 | 3.9063 |   59.32 KB |       0.036 |
| Precompiled   | 100000    | Medium |  7,110.32 μs |  50.140 μs |  46.901 μs |  0.94 |    0.01 |        - |       - |      - |   11.77 KB |        0.09 |
| CompileAntlr  | 100000    | Medium |  7,556.37 μs |  40.126 μs |  37.534 μs |  1.00 |    0.01 |   7.8125 |       - |      - |  125.91 KB |        1.00 |
| CompileParlot | 100000    | Medium |  7,426.26 μs |  67.508 μs |  63.147 μs |  0.98 |    0.01 |        - |       - |      - |   21.03 KB |        0.17 |
| Precompiled   | 100000    | Large  | 44,091.11 μs | 204.657 μs | 191.436 μs |  0.96 |    0.01 |        - |       - |      - |   12.05 KB |       0.007 |
| CompileAntlr  | 100000    | Large  | 46,158.01 μs | 229.397 μs | 214.578 μs |  1.00 |    0.01 | 181.8182 | 90.9091 |      - | 1646.87 KB |       1.000 |
| CompileParlot | 100000    | Large  | 45,234.87 μs | 270.948 μs | 253.445 μs |  0.98 |    0.01 |        - |       - |      - |   59.17 KB |       0.036 |

x64, preview:

| Method        | Documents | Size   | Mean         | Error       | StdDev      | Median       | Ratio | RatioSD | Gen0    | Gen1    | Gen2   | Allocated  | Alloc Ratio |
|-------------- |---------- |------- |-------------:|------------:|------------:|-------------:|------:|--------:|--------:|--------:|-------:|-----------:|------------:|
| Precompiled   | 1000      | Medium |     161.5 μs |     0.99 μs |     0.88 μs |     161.5 μs |  0.09 |    0.00 |  0.4883 |       - |      - |   11.73 KB |        0.09 |
| CompileAntlr  | 1000      | Medium |   1,851.8 μs |    36.53 μs |    42.07 μs |   1,848.5 μs |  1.00 |    0.03 |  7.8125 |  3.9063 | 3.9063 |   125.9 KB |        1.00 |
| CompileParlot | 1000      | Medium |   1,587.4 μs |    31.66 μs |    66.79 μs |   1,603.4 μs |  0.86 |    0.04 |  5.8594 |  5.8594 | 5.8594 |   21.01 KB |        0.17 |
| Precompiled   | 1000      | Large  |   1,219.9 μs |    53.42 μs |   157.52 μs |   1,135.8 μs |  0.13 |    0.02 |       - |       - |      - |   12.01 KB |       0.007 |
| CompileAntlr  | 1000      | Large  |   9,094.4 μs |   180.39 μs |   200.51 μs |   9,046.9 μs |  1.00 |    0.03 | 93.7500 | 54.6875 |      - | 1645.49 KB |       1.000 |
| CompileParlot | 1000      | Large  |   4,537.9 μs |    72.20 μs |    64.00 μs |   4,519.3 μs |  0.50 |    0.01 |       - |       - |      - |   59.17 KB |       0.036 |
| Precompiled   | 100000    | Medium |  16,353.5 μs |   262.70 μs |   269.77 μs |  16,276.5 μs |  0.87 |    0.04 |       - |       - |      - |   11.77 KB |        0.09 |
| CompileAntlr  | 100000    | Medium |  18,796.5 μs |   370.55 μs |   901.96 μs |  18,475.4 μs |  1.00 |    0.06 |       - |       - |      - |  125.91 KB |        1.00 |
| CompileParlot | 100000    | Medium |  17,458.2 μs |   288.74 μs |   255.96 μs |  17,419.4 μs |  0.93 |    0.04 |       - |       - |      - |   20.98 KB |        0.17 |
| Precompiled   | 100000    | Large  | 107,221.1 μs |   669.58 μs |   593.56 μs | 107,268.2 μs |  0.95 |    0.01 |       - |       - |      - |   12.05 KB |       0.007 |
| CompileAntlr  | 100000    | Large  | 112,530.1 μs |   485.90 μs |   454.51 μs | 112,400.3 μs |  1.00 |    0.01 |       - |       - |      - |  1643.9 KB |       1.000 |
| CompileParlot | 100000    | Large  | 119,140.1 μs | 1,704.79 μs | 1,594.66 μs | 118,803.0 μs |  1.06 |    0.01 |       - |       - |      - |   59.17 KB |       0.036 |

The honest summary: the effective gain depends on how often expressions are compiled. With a cached expression
there is none, as the parser is not involved. Compiling for each search on a small index or result set makes the
search 28% (Arm64) to 50% (x64) faster for the large expression, and for the medium one from no change (Arm64) to 14%
faster (x64). On 100,000 documents, evaluating the expression per document dominates, and the difference is within
what one run can tell: 2% faster on Arm64, and on x64 7% faster for the medium expression and 6% slower for the large
one, which can't come from the parser since the IL is the same and compiling is about 4 ms faster. In every
case the allocations per compiled search drop from 126 KB to 21 KB (medium) and from 1.6 MB to 59 KB (large), which
matters more for a server than the time.

## Tests

`dotnet test src/Lucene.Net.Tests.Expressions` passes on net10.0 and net8.0 (macOS and Windows), and on net48 and
net472 on Windows (`-p:TestFrameworks=true -p:IncludeNetFrameworkTargets=true`), which run the `net462` and the
`netstandard2.0` builds: 107 passed, 5 skipped (the existing ignored recursion tests) on each. New:
`TestJavascriptCompiler.TestParseErrorOffsets`, `TestJavascriptCompiler.TestDeepNesting` (100,000 nested parentheses
are a `ParseException`) and `TestJavascriptCompiler.TestLongChains` (100,000 prefix operators compile, and 100,000
nested conditionals don't overflow the stack).

## Known issues

- **Existing bug, unchanged:** a conditional whose false branch emits more than 127 bytes of IL fails with
  `NotSupportedException: Illegal one-byte branch`, because `CompileConditional` jumps from the end of the true
  branch over the false one with `Br_S`. Twenty nested `0?1:` are enough. Both parsers emit the same IL, so both fail;
  `OpCodes.Br` would fix it in a separate change.
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
3. **No error position from a generated entry point (fixed).** In 2.1.0 and 1043, `TryParse` returns `bool` only, so
   reporting where a syntax error is took a mutable configuration object and eight `Then((context, start, end,
   value) => ...)` callbacks on the tokens and at the start of each operand, written inline since a callback may
   not capture another method's parameters (`PARLOT015`).
   [sebastienros/parlot#474](https://github.com/sebastienros/parlot/pull/474) adds an overload with a last
   `out int` returning the furthest failure, which gives the same offsets here; the grammar has neither callbacks nor
   configuration anymore.
4. **Prefix operator chains recursed (fixed).** `Operators()` in 1059 recursed once per prefix operator, which bounded
   `----1` lower than ANTLR did, with a clean `ParseException`
   ([sebastienros/parlot#475](https://github.com/sebastienros/parlot/pull/475)).
   [sebastienros/parlot#483](https://github.com/sebastienros/parlot/pull/483) reads them in a loop. A grammar whose
   parser loops can still build a tree deeper than the code walking it can recurse, which is what happened to the
   compiler here; Parlot can't see that, but the docs of `Unary`, `Operators()` and right-associative levels could say
   it.
5. **`Eof()` doesn't skip trailing white space**, as documented, so `conditional.Eof()` rejected `" 1 "`; the grammar
   uses `AndSkip(SkipWhiteSpace(Eof()))`. Easy to miss with `Terms` parsers everywhere else.
6. **Preview versions sort below the release.** `main` still has `VersionPrefix` 2.0.0, so its Feedz previews are
   `2.0.0-preview.N` (`2.0.0-preview-N` before #482), older than 2.1.0 for NuGet: an update check never offers them,
   and they have to be pinned.

What worked well: `Operators()` replaced the ten nested operator levels and the conditional with one table and made
parsing 2.3 to 2.9 times faster again, the furthest failure of the generated entry point located syntax errors
without any grammar change, `Terms.Scan` ported the ANTLR lexer rules (longest match of `HEX`/`OCTAL`/`DECIMAL`,
dotted names) one to one, `WithWhiteSpaceSet(WhiteSpaceSet.SpaceTabCrLf)` is exactly the grammar's white space, and
the existing tests pass unchanged (only a stale ANTLR exclusion was dropped from `TestApiConsistency`).
