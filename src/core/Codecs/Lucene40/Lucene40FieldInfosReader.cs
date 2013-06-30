using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    internal class Lucene40FieldInfosReader : FieldInfosReader
    {
        public Lucene40FieldInfosReader()
        {
        }

        public override FieldInfos Read(Directory directory, string segmentName, IOContext iocontext)
        {
            String fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene40FieldInfosFormat.FIELD_INFOS_EXTENSION);
            IndexInput input = directory.OpenInput(fileName, iocontext);

            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene40FieldInfosFormat.CODEC_NAME,
                                             Lucene40FieldInfosFormat.FORMAT_START,
                                             Lucene40FieldInfosFormat.FORMAT_CURRENT);

                int size = input.ReadVInt(); //read in the size
                FieldInfo[] infos = new FieldInfo[size];

                for (int i = 0; i < size; i++)
                {
                    String name = input.ReadString();
                    int fieldNumber = input.ReadVInt();
                    byte bits = input.ReadByte();
                    bool isIndexed = (bits & Lucene40FieldInfosFormat.IS_INDEXED) != 0;
                    bool storeTermVector = (bits & Lucene40FieldInfosFormat.STORE_TERMVECTOR) != 0;
                    bool omitNorms = (bits & Lucene40FieldInfosFormat.OMIT_NORMS) != 0;
                    bool storePayloads = (bits & Lucene40FieldInfosFormat.STORE_PAYLOADS) != 0;
                    FieldInfo.IndexOptions? indexOptions;
                    if (!isIndexed)
                    {
                        indexOptions = null;
                    }
                    else if ((bits & Lucene40FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
                    }
                    else if ((bits & Lucene40FieldInfosFormat.OMIT_POSITIONS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;
                    }
                    else if ((bits & Lucene40FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    }
                    else
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    }

                    // LUCENE-3027: past indices were able to write
                    // storePayloads=true when omitTFAP is also true,
                    // which is invalid.  We correct that, here:
                    if (isIndexed && indexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        storePayloads = false;
                    }
                    // DV Types are packed in one byte
                    byte val = input.ReadByte();
                    LegacyDocValuesType oldValuesType = GetDocValuesType((byte)(val & 0x0F));
                    LegacyDocValuesType oldNormsType = GetDocValuesType((byte)(Number.URShift(val, 4) & 0x0F));
                    IDictionary<String, String> attributes = input.ReadStringStringMap(); ;
                    if (oldValuesType.Mapping != null)
                    {
                        attributes[LEGACY_DV_TYPE_KEY] = oldValuesType.Name;
                    }
                    if (oldNormsType.Mapping != null)
                    {
                        if (oldNormsType.Mapping != FieldInfo.DocValuesType.NUMERIC)
                        {
                            throw new CorruptIndexException("invalid norm type: " + oldNormsType + " (resource=" + input + ")");
                        }
                        attributes[LEGACY_NORM_TYPE_KEY] = oldNormsType.Name;
                    }
                    infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector,
                      omitNorms, storePayloads, indexOptions, oldValuesType.Mapping, oldNormsType.Mapping, attributes);
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

        internal static readonly String LEGACY_DV_TYPE_KEY = typeof(Lucene40FieldInfosReader).Name + ".dvtype";
        internal static readonly String LEGACY_NORM_TYPE_KEY = typeof(Lucene40FieldInfosReader).Name + ".normtype";

        internal class LegacyDocValuesType
        {
            internal static readonly LegacyDocValuesType NONE = new LegacyDocValuesType("NONE", null);
            internal static readonly LegacyDocValuesType VAR_INTS = new LegacyDocValuesType("VAR_INTS", FieldInfo.DocValuesType.NUMERIC);
            internal static readonly LegacyDocValuesType FLOAT_32 = new LegacyDocValuesType("FLOAT_32", FieldInfo.DocValuesType.NUMERIC);
            internal static readonly LegacyDocValuesType FLOAT_64 = new LegacyDocValuesType("FLOAT_64", FieldInfo.DocValuesType.NUMERIC);
            internal static readonly LegacyDocValuesType BYTES_FIXED_STRAIGHT = new LegacyDocValuesType("BYTES_FIXED_STRAIGHT", FieldInfo.DocValuesType.BINARY);
            internal static readonly LegacyDocValuesType BYTES_FIXED_DEREF = new LegacyDocValuesType("BYTES_FIXED_DEREF", FieldInfo.DocValuesType.BINARY);
            internal static readonly LegacyDocValuesType BYTES_VAR_STRAIGHT = new LegacyDocValuesType("BYTES_VAR_STRAIGHT", FieldInfo.DocValuesType.BINARY);
            internal static readonly LegacyDocValuesType BYTES_VAR_DEREF = new LegacyDocValuesType("BYTES_VAR_DEREF", FieldInfo.DocValuesType.BINARY);
            internal static readonly LegacyDocValuesType FIXED_INTS_16 = new LegacyDocValuesType("FIXED_INTS_16", FieldInfo.DocValuesType.NUMERIC);
            internal static readonly LegacyDocValuesType FIXED_INTS_32 = new LegacyDocValuesType("FIXED_INTS_32", FieldInfo.DocValuesType.NUMERIC);
            internal static readonly LegacyDocValuesType FIXED_INTS_64 = new LegacyDocValuesType("FIXED_INTS_64", FieldInfo.DocValuesType.NUMERIC);
            internal static readonly LegacyDocValuesType FIXED_INTS_8 = new LegacyDocValuesType("FIXED_INTS_8", FieldInfo.DocValuesType.NUMERIC);
            internal static readonly LegacyDocValuesType BYTES_FIXED_SORTED = new LegacyDocValuesType("BYTES_FIXED_SORTED", FieldInfo.DocValuesType.SORTED);
            internal static readonly LegacyDocValuesType BYTES_VAR_SORTED = new LegacyDocValuesType("BYTES_VAR_SORTED", FieldInfo.DocValuesType.SORTED);

            private static readonly LegacyDocValuesType[] values = new[] {
                NONE,
                VAR_INTS,
                FLOAT_32,
                FLOAT_64,
                BYTES_FIXED_STRAIGHT,
                BYTES_FIXED_DEREF, 
                BYTES_VAR_STRAIGHT,
                BYTES_VAR_DEREF,
                FIXED_INTS_16,
                FIXED_INTS_32,
                FIXED_INTS_64,
                FIXED_INTS_8,
                BYTES_FIXED_SORTED,
                BYTES_VAR_SORTED
            };

            private static readonly IDictionary<string, LegacyDocValuesType> nameLookup = new HashMap<string, LegacyDocValuesType>(StringComparer.OrdinalIgnoreCase) {
                {"NONE", NONE},
                {"VAR_INTS", VAR_INTS},
                {"FLOAT_32", FLOAT_32},
                {"FLOAT_64", FLOAT_64},
                {"BYTES_FIXED_STRAIGHT", BYTES_FIXED_STRAIGHT},
                {"BYTES_FIXED_DEREF", BYTES_FIXED_DEREF},
                {"BYTES_VAR_STRAIGHT", BYTES_VAR_STRAIGHT},
                {"BYTES_VAR_DEREF", BYTES_VAR_DEREF},
                {"FIXED_INTS_16", FIXED_INTS_16},
                {"FIXED_INTS_32", FIXED_INTS_32},
                {"FIXED_INTS_64", FIXED_INTS_64},
                {"FIXED_INTS_8", FIXED_INTS_8},
                {"BYTES_FIXED_SORTED", BYTES_FIXED_SORTED},
                {"BYTES_VAR_SORTED", BYTES_VAR_SORTED}
            };

            private readonly FieldInfo.DocValuesType? mapping;
            private readonly string name;
            private LegacyDocValuesType(string name, FieldInfo.DocValuesType? mapping)
            {
                this.name = name;
                this.mapping = mapping;
            }

            public FieldInfo.DocValuesType? Mapping
            {
                get { return mapping; }
            }

            public string Name
            {
                get { return name; }
            }

            public static LegacyDocValuesType[] Values
            {
                get
                {
                    return values;
                }
            }

            public static LegacyDocValuesType ValueOf(string value)
            {
                return nameLookup[value];
            }
        }

        // decodes a 4.0 type
        private static LegacyDocValuesType GetDocValuesType(byte b)
        {
            return LegacyDocValuesType.Values[b];
        }
    }
}
