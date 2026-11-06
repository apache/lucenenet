// LUCENENET specific - our MMapDirectory implementation deviates from the Java implementation,
// so the ByteBufferIndexInput class was unnecessary and not included in the Lucene.NET release.
// In Java, it was only used by MMapDirectory and was declared package-private, so it
// was clearly not intended for public use. Our implementation instead derives from UnsafeChunkIndexInput,
// which was specifically designed to take advantage of .NET's strengths in memory management and
// garbage collection.

/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
