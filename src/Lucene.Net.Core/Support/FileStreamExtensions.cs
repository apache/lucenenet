using System.IO;

namespace Lucene.Net.Support
{
    public static class FileStreamExtensions
    {
        private static object _fsReadLock = new object();

        //Reads bytes from the Filestream into the bytebuffer
        public static int Read(this FileStream file, ByteBuffer dst, long position)
        {
            lock (_fsReadLock)
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
                    dst.Put((byte) v);
                    count++;
                }

                file.Seek(original, SeekOrigin.Begin);

                return count;
            }
        }
    }
}