using System;
using Force.Crc32;

namespace Lucene.Net.Store
{
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

    [CLSCompliant(false)]
    public class BufferedCrc32Algorithm : Crc32Algorithm
    {
        private bool isComputed = false;

        public BufferedCrc32Algorithm()
            : base(isBigEndian: false)
        {
            HashSizeValue = sizeof(uint);
        }

        public long Value
        {
            get
            {
                if (!isComputed)
                {
                    HashValue = HashFinal();
                }

                return BitConverter.ToUInt32(HashValue, 0);
            }
        }

        public void Update(byte value)
        {
            HashCore(new byte[] { value }, 0, 1);
        }

        public void Update(byte[] buffer, int offset, int length)
        {
            HashCore(buffer, offset, length);
        }

        public void Update(byte[] buffer)
        {
            Update(buffer, 0, buffer.Length);
        }

        public override void Initialize()
        {
            HashValue = new byte[]
            {
                0,0,0,0
            };

            base.Initialize();
        }

        protected override void HashCore(byte[] input, int offset, int length)
        {
            isComputed = false;

            base.HashCore(input, offset, length);
        }

        protected override byte[] HashFinal()
        {
            isComputed = true;

            return base.HashFinal();
        }
    }
}