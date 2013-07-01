using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene42
{
    internal sealed class Lucene42FieldInfosReader : FieldInfosReader
    {
        public Lucene42FieldInfosReader()
        {
        }

        public override FieldInfos Read(Directory directory, string segmentName, IOContext iocontext)
        {
            String fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene42FieldInfosFormat.EXTENSION);
            IndexInput input = directory.OpenInput(fileName, iocontext);

            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene42FieldInfosFormat.CODEC_NAME,
                                             Lucene42FieldInfosFormat.FORMAT_START,
                                             Lucene42FieldInfosFormat.FORMAT_CURRENT);

                int size = input.ReadVInt(); //read in the size
                FieldInfo[] infos = new FieldInfo[size];

                for (int i = 0; i < size; i++)
                {
                    String name = input.ReadString();
                    int fieldNumber = input.ReadVInt();
                    byte bits = input.ReadByte();
                    bool isIndexed = (bits & Lucene42FieldInfosFormat.IS_INDEXED) != 0;
                    bool storeTermVector = (bits & Lucene42FieldInfosFormat.STORE_TERMVECTOR) != 0;
                    bool omitNorms = (bits & Lucene42FieldInfosFormat.OMIT_NORMS) != 0;
                    bool storePayloads = (bits & Lucene42FieldInfosFormat.STORE_PAYLOADS) != 0;
                    FieldInfo.IndexOptions? indexOptions;
                    if (!isIndexed)
                    {
                        indexOptions = null;
                    }
                    else if ((bits & Lucene42FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
                    }
                    else if ((bits & Lucene42FieldInfosFormat.OMIT_POSITIONS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;
                    }
                    else if ((bits & Lucene42FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    }
                    else
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    }

                    // DV Types are packed in one byte
                    byte val = input.ReadByte();
                    FieldInfo.DocValuesType? docValuesType = GetDocValuesType(input, (sbyte)(val & 0x0F));
                    FieldInfo.DocValuesType? normsType = GetDocValuesType(input, (sbyte)(Number.URShift(val, 4) & 0x0F));
                    IDictionary<String, String> attributes = input.ReadStringStringMap();
                    infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector,
                      omitNorms, storePayloads, indexOptions, docValuesType, normsType, attributes);
                }

                if (input.FilePointer != input.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + fileName + "\": read " + input.FilePointer + " vs size " + input.Length + " (resource: " + input + ")");
                }
                FieldInfos fieldInfos = new FieldInfos(infos);
                success = true;
                return fieldInfos;
            }
            finally
            {
                if (success)
                {
                    input.Dispose();
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
            }
        }

        private static FieldInfo.DocValuesType? GetDocValuesType(IndexInput input, sbyte b)
        {
            if (b == 0)
            {
                return null;
            }
            else if (b == 1)
            {
                return FieldInfo.DocValuesType.NUMERIC;
            }
            else if (b == 2)
            {
                return FieldInfo.DocValuesType.BINARY;
            }
            else if (b == 3)
            {
                return FieldInfo.DocValuesType.SORTED;
            }
            else if (b == 4)
            {
                return FieldInfo.DocValuesType.SORTED_SET;
            }
            else
            {
                throw new CorruptIndexException("invalid docvalues byte: " + b + " (resource=" + input + ")");
            }
        }
    }
}
