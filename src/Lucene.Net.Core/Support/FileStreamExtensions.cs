using System.IO;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    public static class FileStreamExtensions
    {
        //Reads bytes from the Filestream into the bytebuffer
        public static int Read(this FileStream file, ByteBuffer dst, long position)
        {
            // TODO: check this logic, could probably optimize
            if (position >= file.Length)
                return 0;

            var original = file.Position;

            var count = dst.Limit - dst.Position;
            // TODO: (.net port) consider move to async all the way.
            var bytes = ReadBytesAsync(file, (int) position, count).Result;
            dst.Put(bytes);

            file.Seek(original, SeekOrigin.Begin);
            return count;
        }

        private static async Task<byte[]> ReadBytesAsync(FileStream file, int offset, int count)
        {
            file.Position = offset;
            offset = 0;
            var buffer = new byte[count];
            int read;
            while (count > 0 && (read = await file.ReadAsync(buffer, offset, count).ConfigureAwait(false)) > 0)
            {
                offset += read;
                count -= read;
            }
            if (count < 0) throw new EndOfStreamException();
            return buffer;
        }

        private static byte[] ReadBytes(FileStream file, int offset, int count)
        {
            file.Position = offset;
            offset = 0;
            var buffer = new byte[count];
            int read;
            while (count > 0 && (read = file.Read(buffer, offset, count)) > 0)
            {
                offset += read;
                count -= read;
            }
            if (count < 0) throw new EndOfStreamException();
            return buffer;
        }
    }
}