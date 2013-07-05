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

using Lucene.Net.Index;
/**
 * A compression mode. Tells how much effort should be spent on compression and
 * decompression of stored fields.
 * @lucene.experimental
 */
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System.IO;
namespace Lucene.Net.Codecs.Compressing
{
public abstract class CompressionMode {


  /** Sole constructor. */
  protected CompressionMode() {}

  /**
   * Create a new {@link Compressor} instance.
   */
  public abstract Compressor newCompressor();

  /**
   * Create a new {@link Decompressor} instance.
   */
  public abstract Decompressor newDecompressor();

    
  /**
   * A compression mode that trades compression ratio for speed. Although the
   * compression ratio might remain high, compression and decompression are
   * very fast. Use this mode with indices that have a high update rate but
   * should be able to load documents from disk quickly.
   */
  public class CompressionModeFast: CompressionMode{

      public override Compressor newCompressor()
      {
        return new LZ4FastCompressor();
      }

      public override Decompressor newDecompressor()
      {
        return new DecompressorLZ4();
      }

      public override string ToString()
      {
        return "FAST";
      }
  }

  public static readonly CompressionMode FAST = new CompressionModeFast();

  /**
   * A compression mode that trades speed for compression ratio. Although
   * compression and decompression might be slow, this compression mode should
   * provide a good compression ratio. This mode might be interesting if/when
   * your index size is much bigger than your OS cache.
   */
  public class CompressionModeHigh: CompressionMode{

    public override Compressor newCompressor()
    {
      return new DeflateCompressor(Deflater.BEST_COMPRESSION);
    }

    public override Decompressor newDecompressor()
    {
      return new DeflateDecompressor();
    }

    public override string ToString()
    {
      return "HIGH_COMPRESSION";
    }
  }

  /**
   * This compression mode is similar to {@link #FAST} but it spends more time
   * compressing in order to improve the compression ratio. This compression
   * mode is best used with indices that have a low update rate but should be
   * able to load documents from disk quickly.
   */
  public readonly class CompressionModeFastDecompression: CompressionMode{

    public override Compressor newCompressor()
    {
      return new LZ4HighCompressor();
    }

    public override Decompressor newDecompressor()
    {
      return new DecompressorLZ4();
    }

    public override string ToString()
    {
      return "FAST_DECOMPRESSION";
    }
  }

  public readonly class DecompressorLZ4: Decompressor
  {

    public override void decompress(DataInput input, int originalLength, int offset, int length, BytesRef bytes)
    {
      // add 7 padding bytes, this is not necessary but can help decompression run faster
      if (bytes.bytes.Length < originalLength + 7) {
        bytes.bytes = new sbyte[ArrayUtil.Oversize(originalLength + 7, 1)];
      }
      
      int decompressedLength = LZ4.Decompress(input, offset + length, bytes.bytes, 0);
      if (decompressedLength > originalLength) {
        throw new CorruptIndexException("Corrupted: lengths mismatch: " + decompressedLength + " > " + originalLength);
      }
      bytes.offset = offset;
      bytes.length = length;
    }

    public override Decompressor clone(){
      return this;
    }
  }

  public readonly class LZ4FastCompressor: Compressor {

    private LZ4.HashTable ht;

    public LZ4FastCompressor() {
      ht = new LZ4.HashTable();
    }

    public override void compress(byte[] bytes, int off, int len, DataOutput output)
    {
      LZ4.Compress(bytes, off, len, output, ht);
    }

  }

  public readonly class LZ4HighCompressor: Compressor {

    private LZ4.HCHashTable ht;

    public LZ4HighCompressor() {
      ht = new LZ4.HCHashTable();
    }

    public override void compress(byte[] bytes, int off, int len, DataOutput output)
    {
      LZ4.CompressHC(bytes, off, len, output, ht);
    }

  }

    public readonly class DeflateDecompressor: Decompressor 
    {

        private Inflater decompressor;
        byte[] compressed;

        public DeflateDecompressor() {
          decompressor = new Inflater();
          compressed = new byte[0];
        }

    public override void Decompress(DataInput input, int originalLength, int offset, int length, BytesRef bytes)
    {
          if (length == 0) 
          {
            bytes.length = 0;
            return;
          }


        int compressedLength = input.ReadVInt();

        if (compressedLength > compressed.Length) 
        {
            compressed = new byte[ArrayUtil.Oversize(compressedLength, 1)];
        }

        input.ReadBytes(compressed, 0, compressedLength);

        decompressor.Reset();
        decompressor.SetInput(compressed, 0, compressedLength);

        bytes.offset = bytes.length = 0;
        while (true) 
        {
            int count;
            try 
            {
                int remaining = bytes.bytes.Length - bytes.length;
                count = decompressor.Inflate(bytes.bytes, bytes.length, remaining);
            } 
            catch (DataFormatException e) {
                throw new IOException(e);
            }

            bytes.length += count;
            if (decompressor.finished()) {
              break;
            } 
            else {
                bytes.bytes = ArrayUtil.Grow(bytes.bytes);
            }
        }

        if (bytes.length != originalLength) {
            throw new CorruptIndexException("Lengths mismatch: " + bytes.length + " != " + originalLength);
        }

          bytes.offset = offset;
          bytes.length = length;
    }

    public override Decompressor clone() {
      return new DeflateDecompressor();
    }

  }

  public readonly class DeflateCompressor: Compressor {

    private Deflater compressor;
    sbyte[] compressed;

    public DeflateCompressor(int level) 
    {
      compressor = new Deflater(level);
      compressed = new sbyte[64];
    }

    public override void compress(byte[] bytes, int off, int len, DataOutput output)
    {
      compressor.reset();
      compressor.setInput(bytes, off, len);
      compressor.finish();

      if (compressor.needsInput()) {
        // no output
        output.WriteVInt(0);
        return;
      }

      int totalCount = 0;
      for (;;) {
        int count = compressor.deflate(compressed, totalCount, compressed.Length - totalCount);
        totalCount += count;
        if (compressor.finished()) {
          break;
        } else {
          compressed = ArrayUtil.Grow(compressed);
        }
      }

      output.WriteVInt(totalCount);
      output.WriteBytes(compressed, totalCount);
    }

  }

}
}