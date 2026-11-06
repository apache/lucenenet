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

# Maintaining the expression parser

Upstream Lucene parses expressions with an ANTLR grammar, `Javascript.g` in Lucene 4.8.0 and `Javascript.g4` in later
versions. Lucene.NET ports that grammar to [Parlot](https://github.com/sebastienros/parlot) instead: its source
generator emits the parser into this assembly at build time, so there is no runtime dependency on a parsing library.
This file describes how the port maps to the upstream grammar, and how to bring in an upstream change.

## Files

| File | Role |
|---|---|
| `JavascriptExpressionParser.parlot.cs` | The grammar. Build-only: the Parlot source generator runs `Build()` and `BuildToken()` at compile time and emits `TryParse` and `TryParseToken`. The file is not compiled into the assembly. |
| `JavascriptExpressionParser.cs` | The declarations of the generated methods, `Parse`, which turns a failure into a `ParseException` with the messages of the ANTLR 3 parser, and the factories of the syntax tree. |
| `JavascriptAst.cs` | The syntax tree. Its shape follows the grammar rules: an operator chain such as `a + b - c` is one node, a parenthesized expression keeps its own node. |
| `JavascriptCompiler.cs` | `JavascriptNodeCompiler` walks the syntax tree and emits IL. It plays the part of upstream's `JavascriptCompiler.recursiveCompile`. |

## How the upstream rules map to the port

The rules are those of Lucene 4.8.0's
[`Javascript.g`](https://github.com/apache/lucene/blob/dc47488a2b8bbf37f610fd40eb7ec599e3dcc32b/lucene/expressions/src/java/org/apache/lucene/expressions/js/Javascript.g).

| Upstream rule | Port |
|---|---|
| `expression: conditional EOF` | `conditional.AndSkip(SkipWhiteSpace(Eof()))`. `Eof()` doesn't skip white space by itself. |
| `conditional: logical_or (AT_COND_QUE conditional AT_COLON conditional)?` | The `Ternary(...)` level, last in the `Operators()` table. |
| `logical_or` down to `multiplicative` | One `LeftAssociative(...)` level of the `Operators()` table each, from the tightest (`multiplicative`) to the loosest (`logical_or`). Each operator calls `CreateChain` with its `ChainLevel`. |
| `unary: postfix \| AT_ADD unary \| unary_operator unary` | The `Unary(...)` level, first in the table. |
| `postfix`, `call`, `primary`, `arguments` | `primary` and `nameOrCall`: a name followed by arguments is a call, a name alone is a variable. |
| `numeric: HEX \| OCTAL \| DECIMAL` | `NumberToken()`, with `CreateNumber` recording which one matched. |
| `NAMESPACE_ID`, `ID` | `NameToken()`. |
| The `AT_*` operator tokens | `Terms.Char` or `Terms.Text` in the level of the operator. |
| `WS` | `WithWhiteSpaceSet(WhiteSpaceSet.SpaceTabCrLf)`, and `Terms.*` parsers skip it before each token. |

Some differences from ANTLR matter when editing the grammar:

- **ANTLR's lexer takes the longest token; Parlot tries alternatives in order.** In `NumberToken()`, the hex and octal
  alternatives come before the decimal one, as each is longer than the decimal it starts with. In a level of the
  table, `>>>` comes before `>>`, and `<=` before `<`.
- **An operator not followed by an operand is tried again in the looser levels.** So `&` doesn't need a lookahead to
  leave `&&` to its own level.
- **`ParseException.ErrorOffset` is the furthest position the parser reached.** Usually that's the first token that
  can't be parsed, but a continuation of a name or number that was tried and abandoned counts too: `x.y+.z` reports
  position 5, the `z`, rather than the `.` at 4.
- **The generator runs the grammar code at compile time.** It reports what it can't generate as a `PARLOT` build
  diagnostic, and a callback can't capture the parameters of another method (`PARLOT015`).

## Porting an upstream change

1. Diff the upstream grammar between the version this port follows and the target version. The last version is
   [`Javascript.g4`](https://github.com/apache/lucene/blob/main/lucene/expressions/src/java/org/apache/lucene/expressions/js/Javascript.g4).
2. Map each changed rule with the table above:
   - **A new operator** is a row in an existing level, or a new `LeftAssociative(...)` level between its neighbors.
   - **A new token** is a `Terms.*` parser in the grammar, or a method like `NameToken()` built from `Literals.*`
     parsers. For error messages, an operator's characters go in `OperatorChars` in `JavascriptExpressionParser.cs`,
     and a token of several characters that isn't an operator goes in `BuildToken()`. Otherwise an error at that token
     reports an unexpected character.
   - **A new kind of node** is a type in `JavascriptAst.cs` and a case in `JavascriptNodeCompiler`. Port its IL from
     the matching case of upstream's `JavascriptCompiler`.
3. Port the upstream tests that come with the change, under `src/Lucene.Net.Tests.Expressions/JS`.
4. Build and run the tests:

   ```bash
   dotnet test src/Lucene.Net.Tests.Expressions/Lucene.Net.Tests.Expressions.csproj
   ```

   `TestParseErrorOffsets`, `TestDeepNesting` and `TestLongChains` cover the error positions and the limits on nested
   and chained expressions.
5. To read the generated parser, rebuild with `--no-incremental -p:EmitCompilerGeneratedFiles=true`. The files are
   under `obj/<Configuration>/<TargetFramework>/generated`.

## Updating Parlot

Change `ParlotSourceGeneratorPackageVersion` in `.build/dependencies.props` and run the tests. The package is a
build-only analyzer (`PrivateAssets="all"`), so the version doesn't reach consumers of `Lucene.Net.Expressions`.
`Lucene.Net.Expressions.csproj` works around the generator's floor of .NET Framework 4.7.2 for the `net462` target.
