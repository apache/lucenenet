---
uid: Lucene.Net.Expressions.JS
summary: *content
---

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

# Javascript expressions

A Javascript expression is a numeric expression specified using an expression syntax that's based on JavaScript expressions. You can construct expressions using:

*   Integer, floating point, hex and octal literals

*   Arithmetic operators: `+ - * / %`

*   Bitwise operators: `| & ^ ~ << >> >>>`

*   Boolean operators (including the ternary operator): `&& || ! ?:`

*   Comparison operators: `&lt; <= == >= >`

*   Common mathematic functions: `abs ceil exp floor ln log2 log10 logn max min sqrt pow`

*   Trigonometric library functions: `acosh acos asinh asin atanh atan atan2 cosh cos sinh sin tanh tan`

*   Distance functions: `haversin`

*   Miscellaneous functions: `min, max`

*   Arbitrary external variables - see <xref:Lucene.Net.Expressions.Bindings>

 JavaScript order of precedence rules apply for operators. Shortcut evaluation is used for logical operators—the second argument is only evaluated if the value of the expression cannot be determined after evaluating the first argument. For example, in the expression `a || b`, `b` is only evaluated if a is not true. 

 To compile an expression, use <xref:Lucene.Net.Expressions.JS.JavascriptCompiler>. 