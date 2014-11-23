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

/** Naive int block API that writes vInts.  This is
 *  expected to give poor performance; it's really only for
 *  testing the pluggability.  One should typically use pfor instead. */

using System;
using System.Diagnostics;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;


/// <summary>
/// Abstract base class that writes fixed-size blocks of ints to an 
/// IndexOutput. While this is a simple approach, a more 
/// performant approach would directly create an impl of 
/// IntIndexOutput inside Directory.  Wrapping a generic IndexInput 
/// will likely cost performance.
///
///  * @lucene.experimental
/// </summary>
public abstract class FixedIntBlockIndexOutput : IntIndexOutput {

  protected IndexOutput out;
  private int blockSize;
  protected int int[] buffer;
  private int upto;

  protected FixedIntBlockIndexOutput(IndexOutput out, int fixedBlockSize)  {
    blockSize = fixedBlockSize;
    this.out = out;
    out.WriteVInt(blockSize);
    buffer = new int[blockSize];
  }

  protected abstract void flushBlock() ;

  public override IntIndexOutputIndex index() {
    return new Index();
  }

    private class Index : IntIndexOutputIndex
    {
        private long fp;
        private int upto;
        private long lastFP;
        private int lastUpto;

        public override void Mark()
        {
            fp =out.
            FilePointer;
            upto = FixedIntBlockIndexOutput.
            this.upto;
        }

        public override void CopyFrom(IntIndexOutputIndex other, bool copyLast)
        {
            Index idx = (Index) other;
            fp = idx.fp;
            upto = idx.upto;
            if (copyLast)
            {
                lastFP = fp;
                lastUpto = upto;
            }
        }

        public override void Write(DataOutput indexOut, bool absolute)
        {
            if (absolute)
            {
                indexOut.WriteVInt(upto);
                indexOut.WriteVLong(fp);
            }
            else if (fp == lastFP)
            {
                // same block
                Debug.Assert(upto >= lastUpto);
                var uptoDelta = upto - lastUpto;
                indexOut.WriteVInt(uptoDelta << 1 | 1);
            }
            else
            {
                // new block
                indexOut.WriteVInt(upto << 1);
                indexOut.WriteVLong(fp - lastFP);
            }
            lastUpto = upto;
            lastFP = fp;
        }

        public override String ToString()
        {
            return String.Format("fp={0} upto={1}", fp, upto);
        }
    }

    public override void Write(int v)
    {
        buffer[upto++] = v;
        if (upto == blockSize)
        {
            flushBlock();
            upto = 0;
        }
    }

    public override void Dispose()
    {
        try
        {
            if (upto > 0)
            {
                // NOTE: entries in the block after current upto are
                // invalid
                flushBlock();
            }
        }
        finally
        {
        out.
            Dispose();
        }
    }
}
