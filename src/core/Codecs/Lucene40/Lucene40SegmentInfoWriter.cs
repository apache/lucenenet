using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    public class Lucene40SegmentInfoWriter : SegmentInfoWriter
    {
        public Lucene40SegmentInfoWriter()
        {
        }

        public override void Write(Directory dir, SegmentInfo si, FieldInfos fis, IOContext ioContext)
        {
            String fileName = IndexFileNames.SegmentFileName(si.name, "", Lucene40SegmentInfoFormat.SI_EXTENSION);
            si.AddFile(fileName);

            IndexOutput output = dir.CreateOutput(fileName, ioContext);

            bool success = false;
            try
            {
                CodecUtil.WriteHeader(output, Lucene40SegmentInfoFormat.CODEC_NAME, Lucene40SegmentInfoFormat.VERSION_CURRENT);
                // Write the Lucene version that created this segment, since 3.1
                output.WriteString(si.Version);
                output.WriteInt(si.DocCount);

                output.WriteByte((byte)(si.UseCompoundFile ? SegmentInfo.YES : SegmentInfo.NO));
                output.WriteStringStringMap(si.Diagnostics);
                output.WriteStringStringMap(si.Attributes);
                output.WriteStringSet(si.Files);

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)output);
                    si.dir.DeleteFile(fileName);
                }
                else
                {
                    output.Dispose();
                }
            }
        }
    }
}
