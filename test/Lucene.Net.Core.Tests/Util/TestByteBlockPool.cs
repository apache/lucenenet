/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements. See the NOTICE file distributed with this
 * work for additional information regarding copyright ownership. The ASF
 * licenses this file to You under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

namespace Lucene.Net.Util
{
    using Lucene.Net.Random;
    using System.Collections.Generic;


    public class TestByteBlockPool : LuceneTestCase
    {
        public class BytesRefProxy : BytesRef
        {
            public BytesRefProxy()
            {
            
            }

            public BytesRefProxy(string value) : base(value)
            {
                
            }

            internal void ForceGrow(int length)
            {
                this.Grow(length);
            }

            internal void SetLength(int length)
            {
                base.Length = length;
            }
        }

        [Test]
        public virtual void TestReadAndWrite()
        {
            var bytesUsed = Counter.NewCounter();
            var  pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(bytesUsed));
            
            var reuseFirst = this.Random.NextBoolean();
            for (var j = 0; j < 2; j++)
            {
                IList<BytesRefProxy> list = new List<BytesRefProxy>();
                int maxLength = this.AtLeast(500),
                    numValues = this.AtLeast(100);
                
               
                numValues.Times(i =>
                {
                    string value = this.Random.RandomRealisticUnicodeString(maxLength: maxLength);
                    list.Add(new BytesRefProxy(value));
                    var @ref = new BytesRefProxy();
                    @ref.CopyChars(value);
                    pool.Append(@ref);
                });
      
                // verify
                long position = 0;
                foreach (var expected in list)
                {
                    var @ref = new BytesRefProxy();
                    @ref.ForceGrow(expected.Length);
                    @ref.SetLength(expected.Length);
                    pool.ReadBytes(position, @ref.Bytes, @ref.Offset, @ref.Length);
                    Equal(expected, @ref);
                    position += @ref.Length;
                }
                pool.Reset(this.Random.NextBoolean(), reuseFirst);
                if (reuseFirst)
                {
                    Equal(ByteBlockPool.BYTE_BLOCK_SIZE, bytesUsed.Count);
                }
                else
                {
                    Equal(0, bytesUsed.Count);
                    pool.NextBuffer(); // prepare for next iter
                }
            }
        }
    }
}