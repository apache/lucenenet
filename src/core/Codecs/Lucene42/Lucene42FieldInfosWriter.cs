using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene42
{
    internal sealed class Lucene42FieldInfosWriter : FieldInfosWriter
    {
        public Lucene42FieldInfosWriter()
        {
        }

        public override void Write(Directory directory, string segmentName, FieldInfos infos, IOContext context)
        {
            String fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene42FieldInfosFormat.EXTENSION);
            IndexOutput output = directory.CreateOutput(fileName, context);
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(output, Lucene42FieldInfosFormat.CODEC_NAME, Lucene42FieldInfosFormat.FORMAT_CURRENT);
                output.WriteVInt(infos.Size);
                foreach (FieldInfo fi in infos)
                {
                    FieldInfo.IndexOptions? indexOptions = fi.IndexOptionsValue;
                    sbyte bits = 0x0;
                    if (fi.HasVectors) bits |= Lucene42FieldInfosFormat.STORE_TERMVECTOR;
                    if (fi.OmitsNorms) bits |= Lucene42FieldInfosFormat.OMIT_NORMS;
                    if (fi.HasPayloads) bits |= Lucene42FieldInfosFormat.STORE_PAYLOADS;
                    if (fi.IsIndexed)
                    {
                        bits |= Lucene42FieldInfosFormat.IS_INDEXED;
                        //assert indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.hasPayloads();
                        if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
                        {
                            bits |= Lucene42FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
                        }
                        else if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                        {
                            bits |= Lucene42FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
                        }
                        else if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS)
                        {
                            bits |= Lucene42FieldInfosFormat.OMIT_POSITIONS;
                        }
                    }
                    output.WriteString(fi.name);
                    output.WriteVInt(fi.number);
                    output.WriteByte(bits);

                    // pack the DV types in one byte
                    sbyte dv = DocValuesByte(fi.DocValuesTypeValue);
                    sbyte nrm = DocValuesByte(fi.NormType);
                    //assert (dv & (~0xF)) == 0 && (nrm & (~0x0F)) == 0;
                    sbyte val = (sbyte)(0xff & ((nrm << 4) | dv));
                    output.WriteByte(val);
                    output.WriteStringStringMap(fi.Attributes);
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    output.Dispose();
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)output);
                }
            }
        }

        private static sbyte DocValuesByte(FieldInfo.DocValuesType? type)
        {
            if (type == null)
            {
                return 0;
            }
            else if (type == FieldInfo.DocValuesType.NUMERIC)
            {
                return 1;
            }
            else if (type == FieldInfo.DocValuesType.BINARY)
            {
                return 2;
            }
            else if (type == FieldInfo.DocValuesType.SORTED)
            {
                return 3;
            }
            else if (type == FieldInfo.DocValuesType.SORTED_SET)
            {
                return 4;
            }
            else
            {
                throw new InvalidOperationException();
            }
        } 
    }
}
