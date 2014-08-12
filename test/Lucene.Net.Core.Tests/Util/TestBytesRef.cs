

namespace Lucene.Net.Util
{
    public class TestBytesRef : LuceneTestCase
    {
        [Test]
        public void TestEmpty()
        {
            var b = new BytesRef();
            Equal(BytesRef.EMPTY_BYTES, b.Bytes);
            Equal(0, b.Offset);
            Equal(0, b.Length);
        }

        [Test]
        public void TestFromBytes()
        {

            byte[] bytes = new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            var bytesRef = new BytesRef(bytes);
            Equal(bytes, bytesRef.Bytes);
            Equal(0, bytesRef.Offset);
            Equal(bytes.Length, bytesRef.Length);

            var bytesRef2 = new BytesRef(bytes, 1, 3);
            Equal("bcd", bytesRef2.Utf8ToString());

            Ok(!bytesRef.Equals(bytesRef2), "bytesRef should not equal bytesRef2");
        }


        [Test]
        public void TestFromChars()
        {
            100.Times((i) =>
            {
                var utf8str1 = this.Random.ToUnicodeString();
                var utf8str2 = new BytesRef(utf8str1).Utf8ToString();
                Equal(utf8str1, utf8str2);
            });
            var value = "\uFFFF";

            Equal(value, new BytesRef(value).Utf8ToString());
        }


    }
}
