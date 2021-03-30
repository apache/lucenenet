---
uid: Lucene.Net.Expressions
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

# expressions

 <xref:Lucene.Net.Expressions.Expression> - result of compiling an expression, which can evaluate it for a given document. Each expression can have external variables are resolved by <xref:Lucene.Net.Expressions.Bindings>. 

 <xref:Lucene.Net.Expressions.Bindings> - abstraction for binding external variables to a way to get a value for those variables for a particular document (ValueSource). 

 <xref:Lucene.Net.Expressions.SimpleBindings> - default implementation of bindings which provide easy ways to bind sort fields and other expressions to external variables.