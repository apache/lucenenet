using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    public class Lucene40SegmentInfoReader : SegmentInfoReader
    {
        public Lucene40SegmentInfoReader()
        {
        }

        public override SegmentInfo Read(Directory dir, string segment, IOContext context)
        {
            String fileName = IndexFileNames.SegmentFileName(segment, "", Lucene40SegmentInfoFormat.SI_EXTENSION);
            IndexInput input = dir.OpenInput(fileName, context);
            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene40SegmentInfoFormat.CODEC_NAME,
                                             Lucene40SegmentInfoFormat.VERSION_START,
                                             Lucene40SegmentInfoFormat.VERSION_CURRENT);
                String version = input.ReadString();
                int docCount = input.ReadInt();
                if (docCount < 0)
                {
                    throw new CorruptIndexException("invalid docCount: " + docCount + " (resource=" + input + ")");
                }
                bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;
                IDictionary<String, String> diagnostics = input.ReadStringStringMap();
                IDictionary<String, String> attributes = input.ReadStringStringMap();
                ISet<String> files = input.ReadStringSet();

                if (input.FilePointer != input.Length)
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + fileName + "\": read " + input.FilePointer + " vs size " + input.Length + " (resource: " + input + ")");
                }

                SegmentInfo si = new SegmentInfo(dir, version, segment, docCount, isCompoundFile,
                                                       null, diagnostics, attributes);
                si.Files = files;

                success = true;

                return si;

            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
                else
                {
                    input.Dispose();
                }
            }
        }
    }
}
