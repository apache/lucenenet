---
uid: Lucene.Net.Util.Packed
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

## Packed integer arrays and streams.

 The packed package provides
 
 * sequential and random access capable arrays of positive longs,
 * routines for efficient serialization and deserialization of streams of packed integers.
 
 The implementations provide different trade-offs between memory usage and access speed. The standard usage scenario is replacing large int or long arrays in order to reduce the memory footprint. 

 The main access point is the <xref:Lucene.Net.Util.Packed.PackedInt32s> factory. 

### In-memory structures

*   __<xref:Lucene.Net.Util.Packed.PackedInt32s.Mutable>__

    *   Only supports positive longs.

    *   Requires the number of bits per value to be known in advance.

    *   Random-access for both writing and reading.

*   __<xref:Lucene.Net.Util.Packed.GrowableWriter>__

    *   Same as PackedInts.Mutable but grows the number of bits per values when needed.

    *   Useful to build a PackedInts.Mutable from a read-once stream of longs.

*   __<xref:Lucene.Net.Util.Packed.PagedGrowableWriter>__

    *   Slices data into fixed-size blocks stored in GrowableWriters.

    *   Supports more than 2B values.

    *   You should use Appending(Delta)PackedLongBuffer instead if you don't need random write access.

*   __<xref:Lucene.Net.Util.Packed.AppendingDeltaPackedInt64Buffer>__

    *   Can store any sequence of longs.

    *   Compression is good when values are close to each other.

    *   Supports random reads, but only sequential writes.

    *   Can address up to 2^42 values.

*   __<xref:Lucene.Net.Util.Packed.AppendingPackedInt64Buffer>__

    *   Same as AppendingDeltaPackedInt64Buffer but assumes values are 0-based.

*   __<xref:Lucene.Net.Util.Packed.MonotonicAppendingInt64Buffer>__

    *   Same as AppendingDeltaPackedInt64Buffer except that compression is good when the stream is a succession of affine functions.

### Disk-based structures

*   __<xref:Lucene.Net.Util.Packed.PackedInt32s.Writer>, <xref:Lucene.Net.Util.Packed.PackedInt32s.Reader>, <xref:Lucene.Net.Util.Packed.PackedInt32s.IReaderIterator>__

    *   Only supports positive longs.

    *   Requires the number of bits per value to be known in advance.

    *   Supports both fast sequential access with low memory footprint with ReaderIterator and random-access by either loading values in memory or leaving them on disk with Reader.

*   __<xref:Lucene.Net.Util.Packed.BlockPackedWriter>, <xref:Lucene.Net.Util.Packed.BlockPackedReader>, <xref:Lucene.Net.Util.Packed.BlockPackedReaderIterator>__

    *   Splits the stream into fixed-size blocks.

    *   Compression is good when values are close to each other.

    *   Can address up to 2B * blockSize values.

*   __<xref:Lucene.Net.Util.Packed.MonotonicBlockPackedWriter>, <xref:Lucene.Net.Util.Packed.MonotonicBlockPackedReader>__

    *   Same as the non-monotonic variants except that compression is good when the stream is a succession of affine functions.

    *   The reason why there is no sequential access is that if you need sequential access, you should rather delta-encode and use BlockPackedWriter.

*   __<xref:Lucene.Net.Util.Packed.PackedDataOutput>, <xref:Lucene.Net.Util.Packed.PackedDataInput>__

    *   Writes sequences of longs where each long can use any number of bits.