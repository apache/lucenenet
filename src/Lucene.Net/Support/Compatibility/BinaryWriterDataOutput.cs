using System;
using System.IO;
using Lucene.Net.Store;

namespace Lucene.Net.Support.Compatibility
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
    public class BinaryWriterDataOutput : DataOutput, IDisposable
    {
        private readonly BinaryWriter bw;

        public BinaryWriterDataOutput(BinaryWriter bw)
        {
            this.bw = bw;
        }

        public override void WriteByte(byte b)
        {
            bw.Write(b);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            bw.Write(b, offset, length);
        }

        public void Dispose()
        {
            bw.Dispose();
        }
    }
}
