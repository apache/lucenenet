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

*   Optional two-pass compression: [FST.pack](xref:Lucene.Net.Util.Fst.FST#methods)

*   [Lookup-by-output](xref:Lucene.Net.Util.Fst.Util#methods) when the 
       outputs are in sorted order (e.g., ordinals or file pointers)

*   Pluggable [Outputs](xref:Lucene.Net.Util.Fst.Outputs) representation

*   [N-shortest-paths](xref:Lucene.Net.Util.Fst.Util#methods) search by
       weight

*   Enumerators ([IntsRef](xref:Lucene.Net.Util.Fst.IntsRefFSTEnum) and [BytesRef](xref:Lucene.Net.Util.Fst.BytesRefFSTEnum)) that behave like {@link java.util.SortedMap SortedMap} iterators

FST Construction example:

        // Input values (keys). These must be provided to Builder in Unicode sorted order!
        String inputValues[] = {"cat", "dog", "dogs"};
        long outputValues[] = {5, 7, 12};

        PositiveIntOutputs outputs = PositiveIntOutputs.getSingleton();
        Builder<Long> builder = new Builder<Long>(INPUT_TYPE.BYTE1, outputs);
        BytesRef scratchBytes = new BytesRef();
        IntsRef scratchInts = new IntsRef();
        for (int i = 0; i < inputValues.length; i++) {
          scratchBytes.copyChars(inputValues[i]);
          builder.add(Util.toIntsRef(scratchBytes, scratchInts), outputValues[i]);
        }
        FST<Long> fst = builder.finish();

Retrieval by key:

        Long value = Util.get(fst, new BytesRef("dog"));
        System.out.println(value); // 7

Retrieval by value:

        // Only works because outputs are also in sorted order
        IntsRef key = Util.getByOutput(fst, 12);
        System.out.println(Util.toBytesRef(key, scratchBytes).utf8ToString()); // dogs

Iterate over key-value pairs in sorted order:

        // Like TermsEnum, this also supports seeking (advance)
        BytesRefFSTEnum<Long> iterator = new BytesRefFSTEnum<Long>(fst);
        while (iterator.next() is object) {
          InputOutput<Long> mapEntry = iterator.current();
          System.out.println(mapEntry.input.utf8ToString());
          System.out.println(mapEntry.output);
        }

N-shortest paths by weight:

        Comparator<Long> comparator = new Comparator<Long>() {
          public int compare(Long left, Long right) {
            return left.compareTo(right);
          }
        };
        Arc<Long> firstArc = fst.getFirstArc(new Arc<Long>());
        MinResult<Long> paths[] = Util.shortestPaths(fst, firstArc, comparator, 2);
        System.out.println(Util.toBytesRef(paths[0].input, scratchBytes).utf8ToString()); // cat
        System.out.println(paths[0].output); // 5
        System.out.println(Util.toBytesRef(paths[1].input, scratchBytes).utf8ToString()); // dog
        System.out.println(paths[1].output); // 7