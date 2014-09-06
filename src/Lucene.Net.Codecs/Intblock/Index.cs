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

namespace Lucene.Net.Codecs.Intblock
{
    using System;
    using Lucene.Net.Codecs.Intblock;

    internal class Index : IntIndexInput.Index 
    {
    private long fp;
    private int upto;

        public override void Read(final DataInput indexIn, final bool absolute)
    {
        if (absolute)
        {
            upto = indexIn.readVInt();
            fp = indexIn.readVLong();
        }
        else
        {
            final
            int uptoDelta = indexIn.readVInt();
            if ((uptoDelta & 1) == 1)
            {
                // same block
                upto += uptoDelta >> > 1;
            }
            else
            {
                // new block
                upto = uptoDelta >> > 1;
                fp += indexIn.readVLong();
            }
        }
        Debug.Assert(
        upto < blockSize;
    }

        public override void Seek(final IntIndexInput .Reader other)
        {
            ((Reader) other).seek(fp, upto);
        }

        public override void CopyFrom(IntIndexInput.Index other)
        {
            Index idx = (Index) other;
            fp = idx.fp;
            upto = idx.upto;
        }

        public override Index Clone()
        {
            Index other = new Index();
            other.fp = fp;
            other.upto = upto;
            return other;
        }

        public override String ToString()
        {
            return "fp=" + fp + " upto=" + upto;
        }
    
    }
}
