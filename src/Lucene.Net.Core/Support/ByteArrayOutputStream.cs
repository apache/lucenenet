using System.IO;
using System.Text;

namespace Lucene.Net.Support
{
    // Used to wrap Java's ByteArrayOutputStream's ToString() method, as MemoryStream uses default impl
    public class ByteArrayOutputStream : MemoryStream
    {
        public ByteArrayOutputStream()
        {
        }

        public ByteArrayOutputStream(int size)
            : base(size)
        {
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(this.ToArray());
        }
    }
}