using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public static class FileStreamExtensions
    {
        public static int Read(this FileStream file, ByteBuffer dst, long position)
        {
            // TODO: check this logic, could probably optimize
            if (position >= file.Length)
                return 0;

            var original = file.Position;

            file.Seek(position, SeekOrigin.Begin);

            int count = 0;

            for (int i = dst.Position; i < dst.Limit; i++)
            {
                int v = file.ReadByte();
                if (v == -1)
                    break;
                dst.Put((byte)v);
            }

            file.Seek(original, SeekOrigin.Begin);

            return count;
        }
    }
}
