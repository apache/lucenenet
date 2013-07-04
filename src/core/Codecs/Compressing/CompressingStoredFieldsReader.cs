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
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Compressing
{
/**
 * {@link StoredFieldsReader} impl for {@link CompressingStoredFieldsFormat}.
 * @lucene.experimental
 */
public sealed class CompressingStoredFieldsReader: StoredFieldsReader {

  private FieldInfos fieldInfos;
  private CompressingStoredFieldsIndexReader indexReader;
  private IndexInput fieldsStream;
  private int packedIntsVersion;
  private CompressionMode compressionMode;
  private Decompressor decompressor;
  private BytesRef bytes;
  private int numDocs;
  private bool closed;

  // used by clone
  private CompressingStoredFieldsReader(CompressingStoredFieldsReader reader) {
    this.fieldInfos = reader.fieldInfos;
    this.fieldsStream = (IndexInput)reader.fieldsStream.Clone();
    this.indexReader = reader.indexReader.clone();
    this.packedIntsVersion = reader.packedIntsVersion;
    this.compressionMode = reader.compressionMode;
    this.decompressor = (Decompressor)reader.decompressor.Clone();
    this.numDocs = reader.numDocs;
    this.bytes = new BytesRef(reader.bytes.bytes.Length);
    this.closed = false;
  }

  /** Sole constructor. */
  public CompressingStoredFieldsReader(Directory d, SegmentInfo si, string segmentSuffix, FieldInfos fn,
      IOContext context, string formatName, CompressionMode compressionMode) 
  {
    this.compressionMode = compressionMode;
    string segment = si.name;
    bool success = false;
    fieldInfos = fn;
    numDocs = si.DocCount;
    IndexInput indexStream = null;
    try {
      fieldsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, FIELDS_EXTENSION), context);
      string indexStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, FIELDS_INDEX_EXTENSION);
      indexStream = d.OpenInput(indexStreamFN, context);

      string codecNameIdx = formatName + CODEC_SFX_IDX;
      string codecNameDat = formatName + CODEC_SFX_DAT;
      CodecUtil.CheckHeader(indexStream, codecNameIdx, VERSION_START, VERSION_CURRENT);
      CodecUtil.CheckHeader(fieldsStream, codecNameDat, VERSION_START, VERSION_CURRENT);

      indexReader = new CompressingStoredFieldsIndexReader(indexStream, si);
      indexStream = null;

      packedIntsVersion = fieldsStream.ReadVInt();
      decompressor = compressionMode.newDecompressor();
      this.bytes = new BytesRef();

      success = true;
    } finally {
      if (!success) {
        IOUtils.closeWhileHandlingException(this, indexStream);
      }
    }
  }

  /**
   * @throws AlreadyClosedException if this FieldsReader is closed
   */
  private void ensureOpen() {
    if (closed) {
      throw new AlreadyClosedException("this FieldsReader is closed");
    }
  }

  /** 
   * Close the underlying {@link IndexInput}s.
   */
  public override void Close() {
    if (!closed) {
      IOUtils.Close(fieldsStream, indexReader);
      closed = true;
    }
  }

  private static void readField(ByteArrayDataInput input, StoredFieldVisitor visitor, FieldInfo info, int bits) {
    switch (bits & TYPE_MASK) {
      case BYTE_ARR:
        int length = input.readVInt();
        byte[] data = new byte[length];
        input.readBytes(data, 0, length);
        visitor.binaryField(info, data);
        break;
      case STRING:
        length = input.readVInt();
        data = new byte[length];
        input.readBytes(data, 0, length);
        visitor.stringField(info, new string(data, IOUtils.CHARSET_UTF_8));
        break;
      case NUMERIC_INT:
        visitor.intField(info, input.readInt());
        break;
      case NUMERIC_FLOAT:
        visitor.floatField(info, Float.intBitsToFloat(input.readInt()));
        break;
      case NUMERIC_LONG:
        visitor.longField(info, input.readLong());
        break;
      case NUMERIC_DOUBLE:
        visitor.doubleField(info, Double.longBitsToDouble(input.readLong()));
        break;
      default:
        throw new AssertionError("Unknown type flag: " + Integer.toHexString(bits));
    }
  }

  private static void skipField(ByteArrayDataInput input, int bits) {
    switch (bits & TYPE_MASK) {
      case BYTE_ARR:
      case STRING:
        int length = input.readVInt();
        input.skipBytes(length);
        break;
      case NUMERIC_INT:
      case NUMERIC_FLOAT:
        input.readInt();
        break;
      case NUMERIC_LONG:
      case NUMERIC_DOUBLE:
        input.readLong();
        break;
      default:
        throw new AssertionError("Unknown type flag: " + Integer.toHexString(bits));
    }
  }

  public override void VisitDocument(int docID, StoredFieldVisitor visitor)
  {
    fieldsStream.Seek(indexReader.getStartPointer(docID));

    int docBase = fieldsStream.ReadVInt();
    int chunkDocs = fieldsStream.ReadVInt();
    if (docID < docBase
        || docID >= docBase + chunkDocs
        || docBase + chunkDocs > numDocs) {
      throw new CorruptIndexException("Corrupted: docID=" + docID
          + ", docBase=" + docBase + ", chunkDocs=" + chunkDocs
          + ", numDocs=" + numDocs);
    }

    int numStoredFields, length, offset, totalLength;
    if (chunkDocs == 1) {
      numStoredFields = fieldsStream.ReadVInt();
      offset = 0;
      length = fieldsStream.ReadVInt();
      totalLength = length;
    } else {
      int bitsPerStoredFields = fieldsStream.ReadVInt();
      if (bitsPerStoredFields == 0) {
        numStoredFields = fieldsStream.ReadVInt();
      } else if (bitsPerStoredFields > 31) {
        throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields);
      } else {
        long filePointer = fieldsStream.FilePointer();
        PackedInts.Reader reader = PackedInts.GetDirectReaderNoHeader(fieldsStream, PackedInts.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerStoredFields);
        numStoredFields = (int) (reader.Get(docID - docBase));
        fieldsStream.Seek(filePointer + PackedInts.Format.PACKED.ByteCount(packedIntsVersion, chunkDocs, bitsPerStoredFields));
      }

      int bitsPerLength = fieldsStream.ReadVInt();
      if (bitsPerLength == 0) {
        length = fieldsStream.ReadVInt();
        offset = (docID - docBase) * length;
        totalLength = chunkDocs * length;
      } else if (bitsPerStoredFields > 31) {
        throw new CorruptIndexException("bitsPerLength=" + bitsPerLength);
      } else {
        PackedInts.ReaderIterator it = (PackedInts.ReaderIterator)PackedInts.GetReaderIteratorNoHeader(fieldsStream, PackedInts.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerLength, 1);
        int off = 0;
        for (int i = 0; i < docID - docBase; ++i) {
          //TODO - HACKMP - Paul, this is a point of concern for me, in that everything from this file, and the 
          //decompressor.Decompress() contract is looking for int.  But, I don't want to simply cast from long to int here.
          off += it.Next();
        }
        offset = off;
        length = (int) it.Next();
        off += length;
        for (int i = docID - docBase + 1; i < chunkDocs; ++i) {
          off += it.Next();
        }
        totalLength = off;
      }
    }

    if ((length == 0) != (numStoredFields == 0)) {
      throw new CorruptIndexException("length=" + length + ", numStoredFields=" + numStoredFields);
    }
    if (numStoredFields == 0) {
      // nothing to do
      return;
    }

    decompressor.Decompress(fieldsStream, totalLength, offset, length, bytes);

    ByteArrayDataInput documentInput = new ByteArrayDataInput(bytes.bytes, bytes.offset, bytes.length);
    for (int fieldIDX = 0; fieldIDX < numStoredFields; fieldIDX++) {
      long infoAndBits = documentInput.ReadVLong();
      int fieldNumber = Number.URShift(infoAndBits, TYPE_BITS); // (infoAndBits >>> TYPE_BITS);
      FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);

      int bits = (int) (infoAndBits & TYPE_MASK);

      switch(visitor.NeedsField(fieldInfo)) {
        case YES:
          readField(documentInput, visitor, fieldInfo, bits);
          break;
        case NO:
          skipField(documentInput, bits);
          break;
        case STOP:
          return;
      }
    }
  }

  public override StoredFieldsReader Clone() {
    ensureOpen();
    return new CompressingStoredFieldsReader(this);
  }

  public CompressionMode getCompressionMode() {
    return compressionMode;
  }

  public ChunkIterator chunkIterator(int startDocID) {
    ensureOpen();
    fieldsStream.Seek(indexReader.getStartPointer(startDocID));
    return new ChunkIterator(fieldsStream, indexReader, numDocs, packedIntsVersion, decompressor);
  }

  internal readonly class ChunkIterator {
      private IndexInput _fieldsStream;
    private CompressingStoredFieldsReader _indexReader;
    private Decompressor _decompressor;
    private int _numOfDocs;
    private int _packedIntsVersion;
    BytesRef bytes;
    int docBase;
    int chunkDocs;
    int[] numStoredFields;
    int[] lengths;

    public ChunkIterator(IndexInput fieldsStream, CompressingStoredFieldsReader indexReader, 
                            int numOfDocs, int packedIntsVersion, Decompressor decompressor) {
        _indexReader = indexReader;
        _numOfDocs = numOfDocs;
        _packedIntsVersion = packedIntsVersion;
        _decompressor = decompressor;
        _fieldsStream = fieldsStream;
      this.docBase = -1;
      bytes = new BytesRef();
      numStoredFields = new int[1];
      lengths = new int[1];
    }

    /**
     * Return the decompressed size of the chunk
     */
    public int ChunkSize() {
      int sum = 0;
      for (int i = 0; i < chunkDocs; ++i) {
        sum += lengths[i];
      }
      return sum;
    }

    /**
     * Go to the chunk containing the provided doc ID.
     */
    public void Next(int doc) {
      _fieldsStream.Seek(_indexReader.getStartPointer(doc));

      int docBase = _fieldsStream.ReadVInt();
      int chunkDocs = _fieldsStream.ReadVInt();
      if (docBase < this.docBase + this.chunkDocs
          || docBase + chunkDocs > _numOfDocs) {
        throw new CorruptIndexException("Corrupted: current docBase=" + this.docBase
            + ", current numDocs=" + this.chunkDocs + ", new docBase=" + docBase
            + ", new numDocs=" + chunkDocs);
      }
      this.docBase = docBase;
      this.chunkDocs = chunkDocs;

      if (chunkDocs > numStoredFields.Length) {
        int newLength = ArrayUtil.Oversize(chunkDocs, 4);
        numStoredFields = new int[newLength];
        lengths = new int[newLength];
      }

      if (chunkDocs == 1) {
          numStoredFields[0] = _fieldsStream.ReadVInt();
          lengths[0] = _fieldsStream.ReadVInt();
      } else {
          int bitsPerStoredFields = _fieldsStream.ReadVInt();
        if (bitsPerStoredFields == 0) {
            Arrays.Fill(numStoredFields, 0, chunkDocs, _fieldsStream.ReadVInt());
        } else if (bitsPerStoredFields > 31) {
          throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields);
        } else {
            PackedInts.ReaderIterator it = (PackedInts.ReaderIterator)PackedInts.GetReaderIteratorNoHeader(_fieldsStream, PackedInts.Format.PACKED, _packedIntsVersion, chunkDocs, bitsPerStoredFields, 1);
          for (int i = 0; i < chunkDocs; ++i) {
            numStoredFields[i] = (int) it.Next();
          }
        }

        int bitsPerLength = _fieldsStream.ReadVInt();
        if (bitsPerLength == 0) {
            Arrays.Fill(lengths, 0, chunkDocs, _fieldsStream.ReadVInt());
        } else if (bitsPerLength > 31) {
          throw new CorruptIndexException("bitsPerLength=" + bitsPerLength);
        } else {
            PackedInts.ReaderIterator it = (PackedInts.ReaderIterator)PackedInts.GetReaderIteratorNoHeader(_fieldsStream, PackedInts.Format.PACKED, _packedIntsVersion, chunkDocs, bitsPerLength, 1);
          for (int i = 0; i < chunkDocs; ++i) {
            lengths[i] = (int) it.Next();
          }
        }
      }
    }

    /**
     * Decompress the chunk.
     */
    public void Decompress(){
      // decompress data
      int chunkSize = this.ChunkSize();
      _decompressor.Decompress(_fieldsStream, chunkSize, 0, chunkSize, bytes);
      if (bytes.length != chunkSize) {
        throw new CorruptIndexException("Corrupted: expected chunk size = " + this.ChunkSize() + ", got " + bytes.length);
      }
    }

    /**
     * Copy compressed data.
     */
    public void CopyCompressedData(DataOutput output){
      long chunkEnd = docBase + chunkDocs == _numOfDocs
          ? _fieldsStream.Length
          : _indexReader.getStartPointer(docBase + chunkDocs);
      output.CopyBytes(_fieldsStream, chunkEnd - _fieldsStream.FilePointer);
    }

  }

}
}