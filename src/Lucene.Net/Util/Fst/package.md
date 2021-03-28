---
uid: Lucene.Net.Util.Fst
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

Finite state transducers

This package implements [
Finite State Transducers](http://en.wikipedia.org/wiki/Finite_state_transducer) with the following characteristics:

*   Fast and low memory overhead construction of the minimal FST 
       (but inputs must be provided in sorted order)

*   Low object overhead and quick deserialization (byte[] representation)

<!-- LUCENENET NOTE: This method is marked internal in Lucene and their link doesn't work -->
*   Optional two-pass compression: [FST.Pack()](xref:Lucene.Net.Util.Fst.FST#methods)

*   [Lookup-by-output](xref:Lucene.Net.Util.Fst.Util#Lucene_Net_Util_Fst_Util_GetByOutput_Lucene_Net_Util_Fst_FST_System_Nullable_System_Int64___System_Int64_) when the 
       outputs are in sorted order (e.g., ordinals or file pointers)

*   Pluggable [Outputs](xref:Lucene.Net.Util.Fst.Outputs) representation

*   [N-shortest-paths](xref:Lucene.Net.Util.Fst.Util#Lucene_Net_Util_Fst_Util_ShortestPaths__1_Lucene_Net_Util_Fst_FST___0__Lucene_Net_Util_Fst_FST_Arc___0____0_System_Collections_Generic_IComparer___0__System_Int32_System_Boolean_) search by
       weight

*   Enumerators ([Int32sRef](xref:Lucene.Net.Util.Fst.Int32sRefFSTEnum) and [BytesRef](xref:Lucene.Net.Util.Fst.BytesRefFSTEnum)) that behave like [SortedDictionary<TKey, TValue>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.sorteddictionary-2) enumerators

FST Construction example:

```cs
// Input values (keys). These must be provided to Builder in Unicode sorted order!
string[] inputValues = new string[] { "cat", "dog", "dogs" };
long[] outputValues = new long[] { 5, 7, 12 };

PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);
BytesRef scratchBytes = new BytesRef();
Int32sRef scratchInts = new Int32sRef();
for (int i = 0; i < inputValues.Length; i++)
{
	scratchBytes.CopyChars(inputValues[i]);
	builder.Add(Util.ToInt32sRef(scratchBytes, scratchInts), outputValues[i]);
}
FST<long?> fst = builder.Finish();
```

Retrieval by key:

```cs
long? value = Util.Get(fst, new BytesRef("dog"));
Console.WriteLine(value); // 7
```

Retrieval by value:

```cs
// Only works because outputs are also in sorted order
Int32sRef key = Util.GetByOutput(fst, 12);
Console.WriteLine(Util.ToBytesRef(key, scratchBytes).Utf8ToString()); // dogs
```

Iterate over key - value pairs in sorted order:

```cs
// Like TermsEnum, this also supports seeking (advance)
BytesRefFSTEnum<long?> enumerator = new BytesRefFSTEnum<long?>(fst);
while (enumerator.MoveNext())
{
	BytesRefFSTEnum.InputOutput<long?> mapEntry = enumerator.Current;
	Console.WriteLine(mapEntry.Input.Utf8ToString());
	Console.WriteLine(mapEntry.Output);
}
```

N-shortest paths by weight:

```cs
var comparer = Comparer<long?>.Create((left, right) =>
{
	return left.GetValueOrDefault().CompareTo(right);
});
FST.Arc<long?> firstArc = fst.GetFirstArc(new FST.Arc<long?>());
Util.TopResults<long?> paths = Util.ShortestPaths(
    fst, firstArc, startOutput: 0, comparer, topN: 2, allowEmptyString: false);

foreach (Util.Result<long?> path in paths)
{
	Console.WriteLine(Util.ToBytesRef(path.Input, scratchBytes).Utf8ToString());
	Console.WriteLine(path.Output);
}

// Results:
//
// cat
// 5
// dog
// 7
```