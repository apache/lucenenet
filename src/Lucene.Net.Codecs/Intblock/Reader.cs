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
    internal static class Reader : IntIndexInput.Reader
    {

    private final IndexInput in;
    private final BlockReader blockReader;
    private final int blockSize;
    private final int[] pending;

    private int upto;
    private bool seekPending;
    private long pendingFP;
    private long lastBlockFP = -1;

    public Reader(final IndexInput in, final int[] pending, final BlockReader blockReader) {
      this.in = in;
      this.pending = pending;
      this.blockSize = pending.length;
      this.blockReader = blockReader;
      upto = blockSize;
    }

    void Seek(final long fp, final int upto) {
      Debug.Assert( upto < blockSize;
      if (seekPending || fp != lastBlockFP) {
        pendingFP = fp;
        seekPending = true;
      }
      this.upto = upto;
    }

    public override int Next() {
      if (seekPending) {
        // Seek & load new block
        in.seek(pendingFP);
        lastBlockFP = pendingFP;
        blockReader.readBlock();
        seekPending = false;
      } else if (upto == blockSize) {
        // Load new block
        lastBlockFP = in.getFilePointer();
        blockReader.readBlock();
        upto = 0;
      }
      return pending[upto++];
    }
  }
    }
}
